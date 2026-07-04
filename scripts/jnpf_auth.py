#!/usr/bin/env python3
"""
JNPF 无浏览器登录 + API 调用（与前端 MD5+AES 一致）

依赖: pip install requests pycryptodome

用法:
  python scripts/jnpf_auth.py login
  python scripts/jnpf_auth.py login --json
  python scripts/jnpf_auth.py GET /api/oauth/CurrentUser
  python scripts/jnpf_auth.py POST /api/studio/pipeline/execute/create '{"name":"t","userRequirement":"测试"}'

环境变量: JNPF_API_URL, JNPF_ACCOUNT, JNPF_PASSWORD, JNPF_CIPHER_KEY
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import sys
from pathlib import Path

try:
    import requests
    from Crypto.Cipher import AES
    from Crypto.Util.Padding import pad
except ImportError:
    print("请先安装: pip install requests pycryptodome", file=sys.stderr)
    sys.exit(1)

REPO_ROOT = Path(__file__).resolve().parent.parent
SESSION_FILE = REPO_ROOT / "scripts" / ".jnpf-session.json"

API_URL = os.environ.get("JNPF_API_URL", "http://localhost:5000").rstrip("/")
ACCOUNT = os.environ.get("JNPF_ACCOUNT", "admin")
PASSWORD = os.environ.get("JNPF_PASSWORD", "123456")
CIPHER_KEY = os.environ.get("JNPF_CIPHER_KEY", "EY8WePvjM5GGwQzn")
ORIGIN = os.environ.get("JNPF_ORIGIN", "pc")


def encrypt_password(plain: str, key: str = CIPHER_KEY) -> str:
    md5hex = hashlib.md5(plain.encode("utf-8")).hexdigest()
    cipher = AES.new(key.encode("utf-8"), AES.MODE_ECB)
    encrypted = cipher.encrypt(pad(md5hex.encode("utf-8"), AES.block_size))
    return encrypted.hex()


def jwt_exp_ms(token: str) -> int | None:
    try:
        payload = token.split(".")[1]
        padding = "=" * (-len(payload) % 4)
        data = json.loads(base64.urlsafe_b64decode(payload + padding))
        exp = data.get("exp")
        return int(exp) * 1000 if exp else None
    except Exception:
        return None


def load_session() -> dict | None:
    if not SESSION_FILE.exists():
        return None
    try:
        data = json.loads(SESSION_FILE.read_text(encoding="utf-8"))
        exp = data.get("expiresAt") or jwt_exp_ms(data.get("token", ""))
        if exp and __import__("time").time() * 1000 > exp - 60_000:
            return None
        return data
    except Exception:
        return None


def save_session(session: dict) -> None:
    SESSION_FILE.parent.mkdir(parents=True, exist_ok=True)
    SESSION_FILE.write_text(json.dumps(session, indent=2), encoding="utf-8")


def login(force: bool = False) -> dict:
    if not force:
        cached = load_session()
        if cached and cached.get("token") and cached.get("apiUrl") == API_URL:
            return cached

    password = encrypt_password(PASSWORD)
    resp = requests.post(
        f"{API_URL}/api/oauth/Login",
        data={
            "account": ACCOUNT,
            "password": password,
            "code": "",
            "timestamp": "",
            "origin": "password",
            "grant_type": "password",
        },
        headers={"Content-Type": "application/x-www-form-urlencoded", "jnpf-origin": ORIGIN},
        timeout=30,
    )
    body = resp.json()
    if body.get("code") != 200:
        raise RuntimeError(f"Login failed: HTTP {resp.status_code} {body}")

    token = body["data"]["token"]
    if token.startswith("Bearer "):
        token = token[7:].strip()
    session = {
        "apiUrl": API_URL,
        "account": ACCOUNT,
        "token": token,
        "expiresAt": jwt_exp_ms(token),
        "loginAt": __import__("datetime").datetime.utcnow().isoformat() + "Z",
    }
    save_session(session)
    return session


def api_request(method: str, path: str, body: dict | str | None = None, retry: bool = True) -> dict:
    session = login()
    url = path if path.startswith("http") else f"{API_URL}{path if path.startswith('/') else '/' + path}"
    headers = {"Authorization": f"Bearer {session['token']}", "jnpf-origin": ORIGIN}
    kwargs: dict = {"headers": headers, "timeout": 120}
    if body is not None:
        headers["Content-Type"] = "application/json"
        kwargs["json"] = body if isinstance(body, dict) else json.loads(body)

    resp = requests.request(method.upper(), url, **kwargs)
    if resp.status_code == 401 and retry:
        session = login(force=True)
        headers["Authorization"] = f"Bearer {session['token']}"
        resp = requests.request(method.upper(), url, **kwargs)

    try:
        data = resp.json()
    except Exception:
        data = resp.text
    return {"status": resp.status_code, "ok": resp.ok, "data": data}


def main() -> None:
    args = sys.argv[1:]
    if not args or args[0] in ("-h", "--help"):
        print(__doc__)
        sys.exit(0)

    if args[0] == "login":
        force = "--force" in args
        sess = login(force=force)
        if "--json" in args:
            print(json.dumps(sess, indent=2))
        else:
            print(sess["token"])
        return

    method = args[0].upper()
    path = args[1] if len(args) > 1 else ""
    if not path:
        print("缺少 path", file=sys.stderr)
        sys.exit(1)
    body = json.loads(args[2]) if len(args) > 2 else None
    result = api_request(method, path, body)
    print(json.dumps(result, indent=2, ensure_ascii=False))
    if not result["ok"]:
        sys.exit(1)


if __name__ == "__main__":
    main()
