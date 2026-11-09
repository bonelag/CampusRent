import { useEffect, useState } from 'react';
import { fetchFile, downloadFile, fallbackImage } from '../lib/api';
import { useApp } from '../context/AppContext';

export function ProtectedImage({ src, alt, ...props }) {
  const local = src?.startsWith('/');
  const [preview, setPreview] = useState(local ? '' : src);
  useEffect(() => {
    if (!local) {
      setPreview(src);
      return;
    }
    const controller = new AbortController();
    let objectUrl;
    setPreview('');
    fetchFile(src, { signal: controller.signal })
      .then((blob) => {
        if (!controller.signal.aborted) {
          objectUrl = URL.createObjectURL(blob);
          setPreview(objectUrl);
        }
      })
      .catch((error) => {
        if (error.name !== 'AbortError') setPreview(fallbackImage);
      });
    return () => {
      controller.abort();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [src, local]);
  return <img {...props} src={preview || fallbackImage} alt={alt} loading="lazy" />;
}

export function DownloadLink({ url, fileName, children, ...props }) {
  const { notify } = useApp();
  const [busy, setBusy] = useState(false);
  if (/^https:\/\//.test(url))
    return (
      <a {...props} href={url} target="_blank" rel="noopener noreferrer">
        {children}
      </a>
    );
  return (
    <button
      {...props}
      type="button"
      disabled={busy}
      aria-busy={busy}
      onClick={async () => {
        setBusy(true);
        try {
          await downloadFile(url, fileName);
        } catch (error) {
          notify(error.message, 'error');
        } finally {
          setBusy(false);
        }
      }}
    >
      {children}
    </button>
  );
}
