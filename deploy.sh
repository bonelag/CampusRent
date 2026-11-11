#!/bin/sh
# CampusRent installer for Ubuntu/Debian. Works with both `curl ... | sh` and Bash.
set -eu
umask 077

cr_configure_only=0
cr_noninteractive=${CAMPUSRENT_NONINTERACTIVE:-0}
for cr_option in "$@"; do
    case "$cr_option" in
        --configure-only) cr_configure_only=1 ;;
        --non-interactive) cr_noninteractive=1 ;;
        --help|-h)
            cat <<'HELP'
CampusRent Docker installer (Ubuntu 22.04+, Debian 12+)
  sh deploy.sh                       Interactive PostgreSQL/HTTPS setup
  sh deploy.sh --configure-only       Write configuration without Docker/startup
  sh deploy.sh --non-interactive      Read CAMPUSRENT_* environment variables

Optional: CAMPUSRENT_SOURCE_DIR=/path/to/checkout (otherwise download GitHub main)
          CAMPUSRENT_GITHUB_CURL_CONFIG=/path/to/private/github-curl.conf
          CAMPUSRENT_INSTALL_DIR=/opt/campusrent
          CAMPUSRENT_REPO=bonelag/CampusRent, CAMPUSRENT_REF=main
          CAMPUSRENT_PROXY=caddy|external (existing Nginx/Apache/host proxy)
Automation: CAMPUSRENT_DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD, DB_SSL
            (all prefixed CAMPUSRENT_), ADMIN_EMAIL, ADMIN_PASSWORD, DOMAIN, PORT
Existing configuration is reused. Set CAMPUSRENT_RECONFIGURE=1 to replace it.
Set CAMPUSRENT_PROXY=external to switch a saved domain to your existing proxy.
HELP
            exit 0 ;;
        *) printf 'Unknown option: %s\n' "$cr_option" >&2; exit 2 ;;
    esac
done

cr_work=''
cr_tty=0
cr_root=$(id -u)
cr_install=${CAMPUSRENT_INSTALL_DIR:-/opt/campusrent}
cr_project=${CAMPUSRENT_PROJECT:-campusrent}
cr_repo=${CAMPUSRENT_REPO:-bonelag/CampusRent}
cr_ref=${CAMPUSRENT_REF:-main}
cr_network=${cr_project}_network
cr_subnet=${CAMPUSRENT_NETWORK_CIDR:-172.30.250.0/24}
cr_proxy=${CAMPUSRENT_PROXY:-}

say() { printf '\n%s\n' "$*"; }
fail() { printf '\nERROR: %s\n' "$*" >&2; exit 1; }
cleanup() {
    if [ "$cr_tty" = 1 ]; then stty echo <&3 2>/dev/null || :; fi
    case "$cr_work" in /tmp/campusrent-install.*) rm -rf -- "$cr_work" ;; esac
}
trap cleanup 0
trap 'exit 130' INT
trap 'exit 143' HUP TERM

as_root() {
    if [ "$cr_root" = 0 ]; then "$@"; else sudo "$@"; fi
}

validate_proxy() {
    case "$cr_proxy" in caddy|external) ;; *) fail 'CAMPUSRENT_PROXY must be caddy or external.' ;; esac
    [ "$cr_proxy" != external ] || [ -n "$cr_domain" ] || fail 'External proxy mode requires a domain.'
}

prompt() {
    cr_label=$1 cr_default=$2 cr_secret=${3:-0}
    if [ "$cr_noninteractive" = 1 ]; then printf '%s' "$cr_default"; return; fi
    if [ "$cr_secret" = 1 ]; then
        stty -echo <&3
        printf '%s: ' "$cr_label" >&3
    else
        printf '%s [%s]: ' "$cr_label" "$cr_default" >&3
    fi
    cr_answer=''
    if ! IFS= read -r cr_answer <&3; then
        stty echo <&3 2>/dev/null || :
        fail 'Cannot read terminal input.'
    fi
    if [ "$cr_secret" = 1 ]; then stty echo <&3; printf '\n' >&3; fi
    printf '%s' "${cr_answer:-$cr_default}"
}

[ "$(uname -s)" = Linux ] || fail 'Run this installer on a Linux server.'
case "$cr_install" in /*) ;; *) fail 'CAMPUSRENT_INSTALL_DIR must be an absolute path.' ;; esac
case "$cr_install" in /|*/../*|*/..|*[!a-zA-Z0-9_./-]*) fail 'Use a dedicated installation path without spaces or parent traversals.' ;; esac
case "$cr_project" in ''|*[!a-z0-9_-]*) fail 'Use a lowercase Docker project name.' ;; esac
case "$cr_repo" in */*) ;; *) fail 'CAMPUSRENT_REPO must be owner/repository.' ;; esac
case "$cr_repo$cr_ref" in *[!a-zA-Z0-9_./-]*|*..*) fail 'Invalid GitHub repository or ref.' ;; esac

if [ "$cr_noninteractive" != 1 ]; then
    if ! { exec 3<>/dev/tty; } 2>/dev/null; then
        fail 'No terminal available. Run interactively or use --non-interactive with CAMPUSRENT_* variables.'
    fi
    cr_tty=1
fi
if [ "$cr_root" != 0 ]; then
    command -v sudo >/dev/null 2>&1 || fail 'Run as root or install sudo.'
    if [ "$cr_tty" = 1 ]; then sudo -v <&3; else sudo -n true || fail 'Non-interactive setup requires root or passwordless sudo.'; fi
fi
if ! command -v python3 >/dev/null 2>&1 || ! command -v curl >/dev/null 2>&1; then
    command -v apt-get >/dev/null 2>&1 || fail 'Install python3, curl, tar and ca-certificates first.'
    as_root apt-get update -qq
    as_root apt-get install -y python3 curl tar ca-certificates
fi
cr_install=$(readlink -m "$cr_install")
[ "$cr_install" != / ] || fail 'Use a dedicated installation directory.'
if [ -n "${CAMPUSRENT_SOURCE_DIR:-}" ]; then
    cr_source=$(cd "$CAMPUSRENT_SOURCE_DIR" && pwd -P)
    case "$cr_install" in "$cr_source"|"$cr_source"/*) fail 'Keep the installation directory outside the source checkout.' ;; esac
fi

cr_work=$(mktemp -d /tmp/campusrent-install.XXXXXX)
cr_runtime=$cr_install/runtime
cr_reuse=0
if as_root test -f "$cr_runtime/appsettings.Production.json" && [ "${CAMPUSRENT_RECONFIGURE:-0}" != 1 ]; then
    cr_keep=$(prompt 'Da co cau hinh. Dung lai? (Y/n)' Y)
    case "$cr_keep" in Y|y|yes|YES) cr_reuse=1 ;; esac
fi

if [ "$cr_reuse" = 1 ]; then
    as_root cp "$cr_runtime/appsettings.Production.json" "$cr_work/appsettings.Production.json"
    as_root cp "$cr_runtime/pgpass" "$cr_work/pgpass"
    as_root cp "$cr_runtime/deploy.env" "$cr_work/deploy.env"
    as_root chown "$cr_root:$(id -g)" "$cr_work/appsettings.Production.json" "$cr_work/pgpass" "$cr_work/deploy.env"
    cr_domain=$(sed -n 's/^CAMPUSRENT_DOMAIN=//p' "$cr_work/deploy.env")
    cr_port=$(sed -n 's/^APP_PORT=//p' "$cr_work/deploy.env")
    cr_db_host=$(sed -n 's/^CAMPUSRENT_DB_HOST=//p' "$cr_work/deploy.env")
    cr_db_port=$(sed -n 's/^CAMPUSRENT_DB_PORT=//p' "$cr_work/deploy.env")
    cr_db_name=$(sed -n 's/^CAMPUSRENT_DB_NAME=//p' "$cr_work/deploy.env")
    cr_db_user=$(sed -n 's/^CAMPUSRENT_DB_USER=//p' "$cr_work/deploy.env")
    cr_db_ssl=$(sed -n 's/^CAMPUSRENT_DB_SSL=//p' "$cr_work/deploy.env")
    cr_network=$(sed -n 's/^CAMPUSRENT_NETWORK_NAME=//p' "$cr_work/deploy.env")
    cr_subnet=$(sed -n 's/^CAMPUSRENT_NETWORK_CIDR=//p' "$cr_work/deploy.env")
    cr_saved_project=$(sed -n 's/^COMPOSE_PROJECT_NAME=//p' "$cr_work/deploy.env")
    cr_project=${cr_saved_project:-$cr_project}
    cr_saved_proxy=$(sed -n 's/^CAMPUSRENT_PROXY=//p' "$cr_work/deploy.env")
    cr_proxy=${cr_proxy:-${cr_saved_proxy:-caddy}}
    validate_proxy
else
    say 'Cau hinh PostgreSQL co san (host phai truy cap duoc tu Docker).'
    cr_db_host=$(prompt 'PostgreSQL host / Supabase pooler host' "${CAMPUSRENT_DB_HOST:-}")
    cr_db_port=$(prompt 'PostgreSQL port' "${CAMPUSRENT_DB_PORT:-5432}")
    cr_db_name=$(prompt 'Database name' "${CAMPUSRENT_DB_NAME:-campusrent}")
    cr_db_user=$(prompt 'Database username' "${CAMPUSRENT_DB_USER:-campusrent}")
    cr_db_password=$(prompt 'Database password' "${CAMPUSRENT_DB_PASSWORD:-}" 1)
    cr_db_ssl=$(prompt 'SSL: VerifyFull / Require / Disable' "${CAMPUSRENT_DB_SSL:-VerifyFull}")
    cr_domain=$(prompt 'Ten mien (de trong chi dung HTTP thu nghiem)' "${CAMPUSRENT_DOMAIN:-}")
    if [ -n "$cr_domain" ] && [ -z "$cr_proxy" ]; then
        cr_proxy=$(prompt 'HTTPS proxy: caddy / external (Nginx/Apache co san)' caddy)
    fi
    cr_proxy=${cr_proxy:-caddy}
    validate_proxy
    cr_port=$(prompt 'Cong HTTP local / cong HTTP thu nghiem' "${CAMPUSRENT_PORT:-8080}")
    cr_admin_email=$(prompt 'Admin email' "${CAMPUSRENT_ADMIN_EMAIL:-}")
    cr_admin_password=$(prompt 'Admin password (at least 12 characters, max 72 UTF-8 bytes)' "${CAMPUSRENT_ADMIN_PASSWORD:-}" 1)
    case "$cr_db_host" in localhost|127.0.0.1|::1) cr_db_host=host.docker.internal ;; esac
    export cr_work cr_runtime cr_db_host cr_db_port cr_db_name cr_db_user cr_db_password cr_db_ssl
    export cr_domain cr_port cr_admin_email cr_admin_password cr_network cr_subnet cr_project
    python3 - <<'PY'
import ipaddress, json, os, re, secrets
from pathlib import Path

def value(name):
    result = os.environ.get('cr_' + name, '')
    if any(c in result for c in '\r\n\x00'):
        raise SystemExit('Invalid configuration: control characters are not allowed.')
    return result

host, name, user, password = (value('db_' + key) for key in ('host', 'name', 'user', 'password'))
email, admin_password, domain = value('admin_email'), value('admin_password'), value('domain').lower()
if not all((host, name, user, password, email, admin_password)):
    raise SystemExit('Database host/name/user/password and administrator credentials are required.')
if not re.fullmatch(r'[A-Za-z0-9_.:-]+', host) or not re.fullmatch(r'[^\s@]+@[^\s@]+\.[^\s@]+', email):
    raise SystemExit('Invalid database hostname or administrator email.')
if len(admin_password) < 12 or len(admin_password.encode('utf-8')) > 72 or admin_password.startswith('CHANGE_ME'):
    raise SystemExit('Administrator password must be unique, at least 12 characters and at most 72 UTF-8 bytes.')
if domain and (len(domain) > 253 or not re.fullmatch(r'(?=.{1,253}$)(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}', domain)):
    raise SystemExit('Use a DNS domain without a scheme, port, path or wildcards.')
try:
    port, db_port = int(value('port')), int(value('db_port'))
    assert 1 <= port <= 65535 and 1 <= db_port <= 65535
    if domain: assert port not in (80, 443)
    network = ipaddress.ip_network(value('subnet'), strict=True)
    assert network.version == 4 and network.is_private
except (ValueError, AssertionError):
    raise SystemExit('Invalid TCP port or private Docker network CIDR.')
ssl = value('db_ssl')
if ssl not in ('VerifyFull', 'Require', 'Disable'):
    raise SystemExit('SSL must be VerifyFull, Require or Disable.')
quote = lambda text: '"' + text.replace('"', '""') + '"'
connection = ';'.join(f'{key}={quote(val)}' for key, val in (
    ('Host', host), ('Port', str(db_port)), ('Database', name), ('Username', user),
    ('Password', password), ('SSL Mode', ssl), ('Timeout', '15')))
config = {
    'Database': {'Provider': 'Postgres'}, 'ConnectionStrings': {'DefaultConnection': connection},
    'Jwt': {'Secret': secrets.token_urlsafe(48), 'Issuer': 'CampusRent', 'Audience': 'CampusRentApp'},
    'Admin': {'Email': email, 'Password': admin_password},
    'ClientUrl': 'https://' + domain if domain else f'http://localhost:{port}',
    'AllowedHosts': domain + ';localhost;127.0.0.1;web' if domain else '*',
    'ReverseProxy': {'KnownNetworks': [str(network)]}
}
work = Path(value('work'))
(work / 'appsettings.Production.json').write_text(json.dumps(config, indent=2, ensure_ascii=False) + '\n')
escape = lambda text: text.replace('\\', '\\\\').replace(':', '\\:')
(work / 'pgpass').write_text(':'.join(map(escape, (host, str(db_port), name, user, password))) + '\n')
env = {
    'COMPOSE_PROJECT_NAME': value('project'),
    'CAMPUSRENT_CONFIG_FILE': value('runtime') + '/appsettings.Production.json',
    'CAMPUSRENT_DOMAIN': domain, 'APP_PORT': str(port), 'WEB_BIND': '127.0.0.1' if domain else '0.0.0.0',
    'CAMPUSRENT_NETWORK_NAME': value('network'), 'CAMPUSRENT_NETWORK_CIDR': str(network),
    'CAMPUSRENT_IMAGE': value('project') + ':production',
    'CAMPUSRENT_DB_HOST': host, 'CAMPUSRENT_DB_PORT': str(db_port),
    'CAMPUSRENT_DB_NAME': name, 'CAMPUSRENT_DB_USER': user, 'CAMPUSRENT_DB_SSL': ssl
}
# Only non-secret, unambiguous values go through Compose interpolation.
for key, val in env.items():
    if any(c in val for c in '\r\n\x00$\'"#'):
        raise SystemExit('Use database names/usernames without quotes, dollar signs or comment characters.')
(work / 'deploy.env').write_text(''.join(f'{key}={val}\n' for key, val in env.items()))
PY
    unset cr_db_password cr_admin_password CAMPUSRENT_DB_PASSWORD CAMPUSRENT_ADMIN_PASSWORD
fi

# Persist the proxy choice without regenerating credentials on an existing installation.
sed '/^CAMPUSRENT_PROXY=/d' "$cr_work/deploy.env" > "$cr_work/proxy.env"
printf 'CAMPUSRENT_PROXY=%s\n' "$cr_proxy" >> "$cr_work/proxy.env"
mv "$cr_work/proxy.env" "$cr_work/deploy.env"
cr_https_profile=''
if [ -n "$cr_domain" ] && [ "$cr_proxy" = caddy ]; then cr_https_profile='--profile https'; fi

if [ "$cr_configure_only" != 1 ]; then
    if ! command -v docker >/dev/null 2>&1 || ! as_root docker compose version >/dev/null 2>&1; then
        [ -r /etc/os-release ] || fail 'Cannot identify the Linux distribution.'
        # /etc/os-release is the operating system's trusted distribution metadata.
        # shellcheck source=/dev/null
        cr_os=$( . /etc/os-release; printf '%s' "$ID" )
        # shellcheck source=/dev/null
        cr_codename=$( . /etc/os-release; printf '%s' "${VERSION_CODENAME:-}" )
        case "$cr_os" in ubuntu|debian) ;; *) fail 'Automatic Docker installation supports Ubuntu/Debian. Install Docker Engine + Compose v2 first on other distributions.' ;; esac
        [ -n "$cr_codename" ] || fail 'Unknown distribution codename.'
        say 'Installing Docker Engine and Compose from the official Docker repository...'
        as_root apt-get update -qq
        as_root apt-get install -y ca-certificates curl
        as_root install -d -m 0755 /etc/apt/keyrings
        curl -fsSL --retry 3 "https://download.docker.com/linux/$cr_os/gpg" -o "$cr_work/docker.asc"
        as_root install -m 0644 "$cr_work/docker.asc" /etc/apt/keyrings/docker.asc
        printf 'deb [arch=%s signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/%s %s stable\n' "$(dpkg --print-architecture)" "$cr_os" "$cr_codename" > "$cr_work/docker.list"
        as_root install -m 0644 "$cr_work/docker.list" /etc/apt/sources.list.d/docker.list
        as_root apt-get update -qq
        as_root apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
    fi
    if ! as_root docker info >/dev/null 2>&1; then
        command -v systemctl >/dev/null 2>&1 || fail 'Start the Docker daemon, then rerun this installer.'
        as_root systemctl enable --now docker
        as_root docker info >/dev/null 2>&1 || fail 'Docker daemon is unavailable.'
    fi
    if as_root docker network inspect "$cr_network" >/dev/null 2>&1; then
        cr_existing_subnet=$(as_root docker network inspect "$cr_network" --format '{{(index .IPAM.Config 0).Subnet}}')
        [ "$cr_existing_subnet" = "$cr_subnet" ] || fail 'Existing Docker network has a different subnet. Use another project name/network CIDR.'
    else
        as_root docker network create --subnet "$cr_subnet" "$cr_network" >/dev/null
    fi
    say 'Checking PostgreSQL connectivity and credentials from Docker...'
    cr_pg_ssl=$(printf '%s' "$cr_db_ssl" | tr '[:upper:]' '[:lower:]')
    cr_pg_root=''
    case "$cr_pg_ssl" in verifyfull) cr_pg_ssl=verify-full; cr_pg_root=system ;; esac
    as_root docker run --rm --network "$cr_network" --add-host host.docker.internal:host-gateway \
        --mount "type=bind,source=$cr_work/pgpass,target=/run/secrets/pgpass,readonly" \
        -e PGPASSFILE=/run/secrets/pgpass -e PGCONNECT_TIMEOUT=15 -e "PGSSLMODE=$cr_pg_ssl" -e "PGSSLROOTCERT=$cr_pg_root" \
        postgres:16-alpine psql --no-password -h "$cr_db_host" -p "$cr_db_port" -U "$cr_db_user" -d "$cr_db_name" \
        -v ON_ERROR_STOP=1 -Atc 'SELECT 1' >/dev/null || fail 'PostgreSQL check failed. Check host reachability, credentials, SSL and database permissions.'
fi

as_root install -d -m 0755 "$cr_install" "$cr_install/releases"
as_root install -d -m 0700 "$cr_runtime"
as_root install -m 0640 -o 1654 -g 1654 "$cr_work/appsettings.Production.json" "$cr_runtime/appsettings.Production.json"
as_root install -m 0600 "$cr_work/pgpass" "$cr_runtime/pgpass"
as_root install -m 0600 "$cr_work/deploy.env" "$cr_runtime/deploy.env"
if [ "$cr_configure_only" = 1 ]; then
    say "Configuration saved to $cr_runtime. Docker and application startup were not performed."
    exit 0
fi

cr_release=$cr_install/releases/$(date -u +%Y%m%dT%H%M%SZ)-$$
as_root install -d -m 0755 "$cr_release"
if [ -n "${CAMPUSRENT_SOURCE_DIR:-}" ]; then
    cr_source=$(cd "$CAMPUSRENT_SOURCE_DIR" && pwd -P)
    [ "$cr_source" != "$cr_install" ] || fail 'Use a source checkout outside the installation directory.'
    tar -C "$cr_source" --exclude=.git --exclude=.env --exclude='*/.env' --exclude='*/node_modules' \
        --exclude='*/bin' --exclude='*/obj' --exclude='*/dist' --exclude=server/uploads \
        --exclude=artifacts --exclude='*/test-results' --exclude='*/playwright-report' -czf "$cr_work/source.tar.gz" .
    as_root tar -xzf "$cr_work/source.tar.gz" -C "$cr_release" --no-same-owner
else
    say "Downloading $cr_repo ($cr_ref)..."
    if [ -n "${CAMPUSRENT_GITHUB_CURL_CONFIG:-}" ]; then
        [ -r "$CAMPUSRENT_GITHUB_CURL_CONFIG" ] || fail 'GitHub curl authentication configuration is not readable.'
        curl --config "$CAMPUSRENT_GITHUB_CURL_CONFIG" -fsSL --retry 3 --proto '=https' \
            "https://api.github.com/repos/$cr_repo/tarball/$cr_ref" -o "$cr_work/source.tar.gz"
    else
        curl -fsSL --retry 3 --proto '=https' "https://api.github.com/repos/$cr_repo/tarball/$cr_ref" -o "$cr_work/source.tar.gz"
    fi
    python3 - "$cr_work/source.tar.gz" <<'PY'
import sys, tarfile
from pathlib import PurePosixPath
with tarfile.open(sys.argv[1]) as archive:
    for member in archive.getmembers():
        path = PurePosixPath(member.name)
        if path.is_absolute() or '..' in path.parts or not (member.isfile() or member.isdir()):
            raise SystemExit('Unsafe source archive. Installation stopped.')
PY
    as_root tar -xzf "$cr_work/source.tar.gz" -C "$cr_release" --strip-components=1 --no-same-owner
fi
[ -f "$cr_release/compose.production.yaml" ] || fail 'The downloaded ref does not contain the production deployment files. Publish this change or use CAMPUSRENT_SOURCE_DIR.'

# Each running container keeps its immutable configuration snapshot during an update.
cr_snapshot=$cr_runtime/appsettings.$(date -u +%Y%m%dT%H%M%SZ)-$$.json
as_root install -m 0640 -o 1654 -g 1654 "$cr_work/appsettings.Production.json" "$cr_snapshot"
sed "s|^CAMPUSRENT_CONFIG_FILE=.*|CAMPUSRENT_CONFIG_FILE=$cr_snapshot|" "$cr_work/deploy.env" > "$cr_work/active.env"
as_root install -m 0600 "$cr_work/active.env" "$cr_runtime/deploy.env"
as_root install -m 0600 "$cr_work/active.env" "$cr_release/deploy.env"

compose() {
    if [ -n "$cr_https_profile" ]; then
        as_root docker compose --project-name "$cr_project" --env-file "$cr_runtime/deploy.env" -f "$cr_release/compose.production.yaml" --profile https "$@"
    else
        as_root docker compose --project-name "$cr_project" --env-file "$cr_runtime/deploy.env" -f "$cr_release/compose.production.yaml" "$@"
    fi
}
compose config --quiet
if [ "$cr_proxy" = external ]; then
    cr_caddy_ids=$(as_root docker ps -aq --filter "label=com.docker.compose.project=$cr_project" --filter 'label=com.docker.compose.service=caddy')
    if [ -n "$cr_caddy_ids" ]; then
        say 'Stopping the Caddy service for this CampusRent project. Your existing proxy handles HTTPS.'
        compose --profile https stop caddy
    fi
fi
start_app() {
    if [ "$cr_proxy" = external ]; then
        compose up -d --build --wait --wait-timeout 180 web
    else
        compose up -d --build --wait --wait-timeout 180
    fi
}
say 'Building and starting CampusRent. Database migrations run before the API becomes healthy...'
if ! start_app; then
    printf 'Diagnostics: sudo docker compose --project-name %s --env-file %s -f %s/compose.production.yaml %s logs --tail 100\n' \
        "$cr_project" "$cr_runtime/deploy.env" "$cr_release" "$cr_https_profile"
    fail 'Startup failed. Configuration, release files and persistent data were retained.'
fi
curl -fsS --max-time 15 "http://127.0.0.1:$cr_port/health" >/dev/null || fail 'Application health check failed.'
if [ -n "$cr_https_profile" ]; then
    curl -fsS --retry 6 --retry-delay 3 --retry-all-errors --max-time 15 "https://$cr_domain/health" >/dev/null || fail 'The app is healthy locally but HTTPS is not ready. Point DNS at this server and allow ports 80/443; inspect Caddy logs.'
    cr_url=https://$cr_domain
elif [ "$cr_proxy" = external ]; then
    cr_url=http://127.0.0.1:$cr_port
    say "Configure your existing reverse proxy for https://$cr_domain -> $cr_url."
    printf 'Verify public HTTPS after configuring the proxy: curl --fail https://%s/health\n' "$cr_domain"
else
    cr_url=http://SERVER_IP:$cr_port
    say 'HTTP mode is for verification only. Configure a domain and HTTPS before accepting production accounts.'
fi
if as_root test -e "$cr_install/current" && ! as_root test -L "$cr_install/current"; then
    fail 'The current path is not a symlink. Release is running; resolve the path manually.'
fi
as_root ln -sfnT "$cr_release" "$cr_install/current"
if [ "$cr_proxy" = external ]; then
    say "CampusRent backend is healthy: $cr_url (public HTTPS is managed by your proxy)."
else
    say "CampusRent is healthy: $cr_url"
fi
printf 'Manage: sudo docker compose --project-name %s --env-file %s -f %s/compose.production.yaml %s logs -f\n' \
    "$cr_project" "$cr_runtime/deploy.env" "$cr_install/current" "$cr_https_profile"
say 'Re-run the installer to update. Existing credentials and upload volumes are retained. Back up PostgreSQL before updates.'
