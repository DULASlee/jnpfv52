const TOKEN_KEY = 'datascreen_token';

const ALLOWED_ORIGINS = [
  window.location.origin,
  ...(import.meta.env.VITE_ALLOWED_PARENT_ORIGINS || '').split(',').filter(Boolean),
];

function isValidOrigin(origin) {
  if (!origin) return false;
  return ALLOWED_ORIGINS.some(allowed => {
    if (allowed === '*') return true;
    return origin === allowed;
  });
}

export function getToken() {
  return sessionStorage.getItem(TOKEN_KEY) || '';
}

export function setToken(token) {
  if (token) {
    sessionStorage.setItem(TOKEN_KEY, token);
  }
}

export function clearToken() {
  sessionStorage.removeItem(TOKEN_KEY);
}

export function hasToken() {
  return !!getToken();
}

export function initTokenListener() {
  window.addEventListener('message', (event) => {
    if (!isValidOrigin(event.origin)) return;
    if (event.data && event.data.type === 'JNPF_TOKEN' && event.data.token) {
      setToken(event.data.token);
    }
  });

  const urlToken = new URLSearchParams(window.location.search).get('token');
  if (urlToken) {
    console.warn(
      '[JNPF Security] Token passed via URL parameter is deprecated and will be removed. ' +
      'Use postMessage with { type: "JNPF_TOKEN", token } instead.'
    );
    setToken(urlToken);
    if (window.history && window.history.replaceState) {
      const url = new URL(window.location);
      url.searchParams.delete('token');
      window.history.replaceState({}, '', url);
    }
  }
}
