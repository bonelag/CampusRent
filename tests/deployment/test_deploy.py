#!/usr/bin/env python3
"""Linux installer integration checks. Run with sudo; --docker includes a real rollout."""
import http.server
import json
import os
import pty
import re
import select
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import threading
import time
import unittest
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
INSTALLER = ROOT / 'deploy.sh'
DOCKER = '--docker' in sys.argv
if DOCKER:
    sys.argv.remove('--docker')


@unittest.skipUnless(sys.platform.startswith('linux') and os.geteuid() == 0, 'Run on Linux with sudo')
class InstallerTests(unittest.TestCase):
    def setUp(self):
        self.workspace = Path(tempfile.mkdtemp(prefix='campusrent-installer-test-', dir='/tmp'))
        self.install = self.workspace / 'app'
        self.password = 'Test:postgres\\password;"with$hash#123'
        self.admin_password = 'Test:administrator$secret#123'
        self.env = {k: v for k, v in os.environ.items() if not k.startswith('CAMPUSRENT_')}
        self.env.update({
            'CAMPUSRENT_NONINTERACTIVE': '1', 'CAMPUSRENT_INSTALL_DIR': str(self.install),
            'CAMPUSRENT_SOURCE_DIR': str(ROOT), 'CAMPUSRENT_DB_HOST': 'database.example.com',
            'CAMPUSRENT_DB_PORT': '5432', 'CAMPUSRENT_DB_NAME': 'campusrent',
            'CAMPUSRENT_DB_USER': 'owner', 'CAMPUSRENT_DB_PASSWORD': self.password,
            'CAMPUSRENT_DB_SSL': 'VerifyFull', 'CAMPUSRENT_ADMIN_EMAIL': 'admin@example.com',
            'CAMPUSRENT_ADMIN_PASSWORD': self.admin_password,
            'CAMPUSRENT_DOMAIN': 'campusrent.example.com', 'CAMPUSRENT_PORT': '58080',
        })

    def tearDown(self):
        assert str(self.workspace).startswith('/tmp/campusrent-installer-test-')
        shutil.rmtree(self.workspace)

    def configure(self, **changes):
        env = dict(self.env, **changes)
        return subprocess.run(['sh', str(INSTALLER), '--configure-only'], env=env,
                              stdin=subprocess.DEVNULL, stdout=subprocess.PIPE,
                              stderr=subprocess.STDOUT, text=True, timeout=90)

    def config(self):
        return json.loads((self.install / 'runtime/appsettings.Production.json').read_text())

    def test_shell_syntax_and_shellcheck(self):
        for command in (['sh', '-n'], ['bash', '-n'], ['shellcheck']):
            result = subprocess.run(command + [str(INSTALLER)], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)

    def test_configuration_preserves_special_passwords_and_protects_secrets(self):
        result = self.configure()
        self.assertEqual(0, result.returncode, result.stdout)
        config = self.config()
        connection = config['ConnectionStrings']['DefaultConnection']
        password = re.search(r'Password="((?:[^"]|"")*)"', connection).group(1).replace('""', '"')
        self.assertEqual(self.password, password)
        self.assertGreaterEqual(len(config['Jwt']['Secret']), 64)
        self.assertEqual(self.admin_password, config['Admin']['Password'])
        self.assertEqual('https://campusrent.example.com', config['ClientUrl'])
        runtime = self.install / 'runtime'
        self.assertEqual(0o700, runtime.stat().st_mode & 0o777)
        self.assertEqual(0o640, (runtime / 'appsettings.Production.json').stat().st_mode & 0o777)
        self.assertEqual(1654, (runtime / 'appsettings.Production.json').stat().st_uid)
        self.assertEqual(0o600, (runtime / 'pgpass').stat().st_mode & 0o777)
        env_text = (runtime / 'deploy.env').read_text()
        for secret in (self.password, self.admin_password, config['Jwt']['Secret']):
            self.assertNotIn(secret, env_text)
            self.assertNotIn(secret, result.stdout)

    def test_rerun_preserves_existing_jwt_and_credentials(self):
        self.assertEqual(0, self.configure().returncode)
        original = self.config()
        result = self.configure(CAMPUSRENT_DB_PASSWORD='different-password', CAMPUSRENT_ADMIN_PASSWORD='different-admin-password')
        self.assertEqual(0, result.returncode, result.stdout)
        self.assertEqual(original, self.config())

    def test_invalid_input_stops_before_config_is_written(self):
        for changes in ({'CAMPUSRENT_DB_PASSWORD': ''}, {'CAMPUSRENT_DOMAIN': 'example.com { malicious }'},
                        {'CAMPUSRENT_PORT': '80'}, {'CAMPUSRENT_ADMIN_PASSWORD': 'short'}):
            result = self.configure(**changes)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse((self.install / 'runtime/appsettings.Production.json').exists())

    def test_installer_refuses_to_store_secrets_inside_checkout(self):
        result = self.configure(CAMPUSRENT_INSTALL_DIR=str(ROOT / 'artifacts/unsafe-install'))
        self.assertNotEqual(0, result.returncode)
        self.assertIn('outside the source checkout', result.stdout)

    def test_curl_pipe_prompts_from_terminal_under_sh_and_bash(self):
        content = INSTALLER.read_bytes()

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_GET(self):
                if self.path != '/deploy.sh':
                    self.send_error(404)
                    return
                self.send_response(200)
                self.send_header('Content-Type', 'text/plain')
                self.send_header('Content-Length', str(len(content)))
                self.end_headers()
                self.wfile.write(content)

            def log_message(self, *_args):
                pass

        server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        worker = threading.Thread(target=server.serve_forever, daemon=True)
        worker.start()
        try:
            for shell in ('sh', 'bash'):
                target = self.workspace / shell
                env = dict(self.env, CAMPUSRENT_NONINTERACTIVE='0', CAMPUSRENT_INSTALL_DIR=str(target))
                pid, fd = pty.fork()
                if pid == 0:
                    command = f'curl -fsSL http://127.0.0.1:{server.server_port}/deploy.sh | {shell} -s -- --configure-only'
                    os.execve('/bin/sh', ['sh', '-c', command], env)
                prompts = [(b'PostgreSQL host', 'database.example.com'), (b'PostgreSQL port', '5432'),
                           (b'Database name', 'campusrent'), (b'Database username', 'owner'),
                           (b'Database password:', self.password), (b'SSL:', 'VerifyFull'),
                           (b'Ten mien', 'campusrent.example.com'), (b'Cong HTTP', '58080'),
                           (b'Admin email', 'admin@example.com'), (b'Admin password', self.admin_password)]
                output = b''
                step = 0
                deadline = time.monotonic() + 40
                status = None
                try:
                    while time.monotonic() < deadline:
                        if select.select([fd], [], [], .1)[0]:
                            try:
                                chunk = os.read(fd, 65536)
                            except OSError:
                                break
                            if not chunk:
                                break
                            output += chunk
                            if step < len(prompts) and prompts[step][0] in output:
                                os.write(fd, (prompts[step][1] + '\n').encode())
                                step += 1
                        done, status = os.waitpid(pid, os.WNOHANG)
                        if done:
                            break
                    else:
                        os.kill(pid, signal.SIGTERM)
                    if status is None or not done:
                        _, status = os.waitpid(pid, 0)
                finally:
                    os.close(fd)
                text = output.decode(errors='replace')
                self.assertEqual(10, step, text)
                self.assertEqual(0, os.waitstatus_to_exitcode(status), text)
                self.assertNotIn(self.password, text)
                self.assertNotIn(self.admin_password, text)
                stored = json.loads((target / 'runtime/appsettings.Production.json').read_text())
                self.assertEqual(self.admin_password, stored['Admin']['Password'])
        finally:
            server.shutdown()
            server.server_close()

    @unittest.skipUnless(DOCKER, 'Use --docker for real PostgreSQL/container rollout')
    def test_full_docker_rollout_and_repeat_keep_data_and_sessions(self):
        project = 'campusrent-check-' + self.workspace.name.rsplit('-', 1)[-1].lower()
        network, pg = project + '_network', project + '-pg'
        subnet = '172.29.252.0/24'

        def docker(*args, **kwargs):
            return subprocess.run(['docker', *args], check=True, capture_output=True, text=True, **kwargs)

        docker('network', 'create', '--subnet', subnet, network)
        try:
            docker('run', '-d', '--name', pg, '--network', network,
                   '-e', 'POSTGRES_PASSWORD=installer-root-test-only', 'postgres:16-alpine')
            for _ in range(60):
                ready = subprocess.run(['docker', 'exec', pg, 'pg_isready', '-U', 'postgres'], capture_output=True)
                if ready.returncode == 0:
                    break
                time.sleep(.5)
            sql_password = self.password.replace("'", "''")
            sql = f"CREATE ROLE owner LOGIN PASSWORD '{sql_password}';\nCREATE DATABASE campusrent OWNER owner;\n"
            docker('exec', '-i', pg, 'psql', '-U', 'postgres', '-v', 'ON_ERROR_STOP=1', input=sql)
            with socket.socket() as sock:
                sock.bind(('127.0.0.1', 0))
                port = sock.getsockname()[1]
            env = dict(self.env, CAMPUSRENT_DB_HOST=pg, CAMPUSRENT_DB_SSL='Disable',
                       CAMPUSRENT_DOMAIN='', CAMPUSRENT_PORT=str(port), CAMPUSRENT_PROJECT=project,
                       CAMPUSRENT_NETWORK_CIDR=subnet)
            command = ['sh', str(INSTALLER), '--non-interactive']
            for attempt in range(2):
                result = subprocess.run(command, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                        text=True, timeout=900)
                self.assertEqual(0, result.returncode, result.stdout[-8000:])
                config = self.config()
                url = f'http://127.0.0.1:{port}'
                with urllib.request.urlopen(url + '/health') as response:
                    self.assertEqual(200, response.status)
                if attempt == 0:
                    jwt = config['Jwt']['Secret']
                    body = json.dumps({'email': 'persist@example.com', 'password': 'PersistPassword123!', 'fullName': 'Persisted student', 'role': 'STUDENT'}).encode()
                    req = urllib.request.Request(url + '/api/auth/register', body, {'Content-Type': 'application/json'})
                    with urllib.request.urlopen(req) as response:
                        token = json.load(response)['data']['token']
                else:
                    self.assertEqual(jwt, config['Jwt']['Secret'])
                    req = urllib.request.Request(url + '/api/auth/me', headers={'Authorization': 'Bearer ' + token})
                    with urllib.request.urlopen(req) as response:
                        self.assertEqual('persist@example.com', json.load(response)['data']['email'])
        finally:
            env_file = self.install / 'runtime/deploy.env'
            current = self.install / 'current/compose.production.yaml'
            if env_file.exists() and current.exists():
                subprocess.run(['docker', 'compose', '--project-name', project, '--env-file', str(env_file), '-f', str(current), 'down', '--volumes'], capture_output=True)
            subprocess.run(['docker', 'rm', '-fv', pg], capture_output=True)
            subprocess.run(['docker', 'network', 'rm', network], capture_output=True)


if __name__ == '__main__':
    unittest.main(verbosity=2)
