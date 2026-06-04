#!/usr/bin/env python3
"""
UniApp H5 静态预览 + API/WebSocket 反代（演示联调）
- 静态根: ../unpackage/dist/build/web
- 监听: http://localhost:3800
- /api/* -> http://localhost:5000
- /websocket/* -> ws://localhost:5000
"""
from __future__ import annotations

import os
import sys
import struct
import hashlib
import base64
import socket
import select
import threading
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

PORT = int(os.environ.get("JNPF_H5_PORT", "3800"))
API_TARGET = os.environ.get("JNPF_API_TARGET", "http://localhost:5000").rstrip("/")
WS_TARGET = os.environ.get("JNPF_WS_TARGET", "localhost:5000")
ROOT = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "unpackage", "dist", "build", "web")
)


class ProxyHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def log_message(self, fmt, *args):
        sys.stderr.write("[%s] %s\n" % (self.log_date_time_string(), fmt % args))

    # Stub endpoints: return empty success instead of 404
    STUB_PATHS = {"/api/system/MenuData", "/api/system/MenuData/getAppDataList", "/api/system/MenuData/getDataList"}

    def _stub_response(self):
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self._cors_headers()
        self.end_headers()
        self.wfile.write(b'{"code":200,"msg":"","data":{"list":[],"pagination":{"total":0}},"timestamp":0}')

    def do_GET(self):
        if self.path in self.STUB_PATHS:
            return self._stub_response()
        if self.path.startswith("/api/"):
            return self._proxy()
        if self.path.startswith("/websocket"):
            return self._ws_proxy()
        # SPA fallback: if the path doesn't map to a real file, serve index.html
        path = self.translate_path(self.path)
        if not os.path.exists(path) or (os.path.isdir(path) and not os.path.exists(os.path.join(path, "index.html"))):
            self.path = "/"
        return super().do_GET()

    def do_POST(self):
        if self.path.startswith("/api/system/MenuData"):
            return self._stub_response()
        if self.path.startswith("/api/"):
            return self._proxy()
        self.send_error(405)

    def do_PUT(self):
        if self.path.startswith("/api/"):
            return self._proxy()
        self.send_error(405)

    def do_DELETE(self):
        if self.path.startswith("/api/system/MenuData"):
            return self._stub_response()
        if self.path.startswith("/api/"):
            return self._proxy()
        self.send_error(405)

    def do_OPTIONS(self):
        if self.path.startswith("/api/"):
            self.send_response(204)
            self._cors_headers()
            self.end_headers()
            return
        self.send_error(405)

    def _cors_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Authorization, Content-Type, jnpf-origin, X-Requested-With")

    def _proxy(self):
        url = API_TARGET + self.path
        length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(length) if length else None
        headers = {k: v for k, v in self.headers.items() if k.lower() not in ("host", "connection")}
        req = Request(url, data=body, headers=headers, method=self.command)
        try:
            with urlopen(req, timeout=120) as resp:
                data = resp.read()
                self.send_response(resp.status)
                for k, v in resp.headers.items():
                    if k.lower() not in ("transfer-encoding", "connection"):
                        self.send_header(k, v)
                self._cors_headers()
                self.end_headers()
                self.wfile.write(data)
        except HTTPError as e:
            data = e.read()
            self.send_response(e.code)
            self._cors_headers()
            self.end_headers()
            self.wfile.write(data)
        except URLError as e:
            self.send_error(502, str(e))

    def _ws_proxy(self):
        """Proxy WebSocket upgrade to backend.
        Rewrites /websocket/Bearer{token} -> /api/message/websocket/{token}
        """
        if self.headers.get("Upgrade", "").lower() != "websocket":
            self.send_error(400, "Not a WebSocket request")
            return

        # Rewrite path: /websocket/Bearer{token} -> /api/message/websocket/{token}
        backend_path = self.path.replace("/websocket/Bearer", "/api/message/websocket/", 1)
        if backend_path == self.path:
            backend_path = self.path.replace("/websocket/", "/api/message/websocket/", 1)

        # Perform WebSocket handshake with backend
        try:
            host, port_str = WS_TARGET.split(":")
            port = int(port_str)
            backend_sock = socket.create_connection((host, port), timeout=10)
        except Exception as e:
            self.send_error(502, f"Cannot connect to backend: {e}")
            return

        # Build the upgrade request for the backend
        ws_key = self.headers.get("Sec-WebSocket-Key", "")
        ws_version = self.headers.get("Sec-WebSocket-Version", "13")
        ws_protocols = self.headers.get("Sec-WebSocket-Protocol", "")
        ws_extensions = self.headers.get("Sec-WebSocket-Extensions", "")

        upgrade_req = (
            f"GET {backend_path} HTTP/1.1\r\n"
            f"Host: {WS_TARGET}\r\n"
            f"Upgrade: websocket\r\n"
            f"Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {ws_key}\r\n"
            f"Sec-WebSocket-Version: {ws_version}\r\n"
        )
        if ws_protocols:
            upgrade_req += f"Sec-WebSocket-Protocol: {ws_protocols}\r\n"
        if ws_extensions:
            upgrade_req += f"Sec-WebSocket-Extensions: {ws_extensions}\r\n"
        upgrade_req += "\r\n"

        backend_sock.sendall(upgrade_req.encode())

        # Read backend response
        resp_data = b""
        while b"\r\n\r\n" not in resp_data:
            chunk = backend_sock.recv(4096)
            if not chunk:
                backend_sock.close()
                self.send_error(502, "Backend closed connection")
                return
            resp_data += chunk

        # Check if backend accepted the upgrade
        status_line = resp_data.split(b"\r\n")[0]
        if b"101" not in status_line:
            backend_sock.close()
            self.send_error(502, f"Backend rejected WebSocket: {status_line.decode()}")
            return

        # Send the 101 Switching Protocols to the client
        self.send_response(101)
        self.send_header("Upgrade", "websocket")
        self.send_header("Connection", "Upgrade")
        # Echo back the Sec-WebSocket-Accept
        for line in resp_data.split(b"\r\n")[1:]:
            if b":" in line:
                k, v = line.split(b":", 1)
                k = k.strip().decode()
                v = v.strip().decode()
                if k.lower() in ("sec-websocket-accept", "sec-websocket-protocol"):
                    self.send_header(k, v)
        self.end_headers()

        # Bidirectional relay
        client_sock = self.connection
        self._relay(client_sock, backend_sock)

    def _relay(self, client, backend):
        """Bidirectional WebSocket frame relay."""
        client.setblocking(False)
        backend.setblocking(False)
        sockets = [client, backend]
        try:
            while True:
                readable, _, exceptional = select.select(sockets, [], sockets, 60)
                if exceptional:
                    break
                for sock in readable:
                    try:
                        data = sock.recv(65536)
                    except (ConnectionResetError, OSError):
                        data = b""
                    if not data:
                        return
                    target = backend if sock is client else client
                    try:
                        target.sendall(data)
                    except (BrokenPipeError, ConnectionResetError, OSError):
                        return
        except Exception:
            pass
        finally:
            try:
                client.close()
            except Exception:
                pass
            try:
                backend.close()
            except Exception:
                pass

    def end_headers(self):
        self._cors_headers()
        super().end_headers()


def main():
    if not os.path.isdir(ROOT):
        sys.stderr.write(f"ERROR: H5 build not found: {ROOT}\n")
        sys.stderr.write("Run HBuilderX: 发行 -> 网站-H5, then retry.\n")
        sys.exit(1)
    os.chdir(ROOT)
    server = ThreadingHTTPServer(("0.0.0.0", PORT), ProxyHandler)
    print(f"H5 proxy: http://localhost:{PORT}/")
    print(f"Static:   {ROOT}")
    print(f"API:      {API_TARGET}")
    print(f"WebSocket: ws://{WS_TARGET}/")
    server.serve_forever()


if __name__ == "__main__":
    main()
