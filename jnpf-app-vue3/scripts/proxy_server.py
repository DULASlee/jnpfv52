#!/usr/bin/env python3
"""
UniApp H5 静态预览 + API 反代（演示联调）
- 静态根: ../unpackage/dist/build/web
- 监听: http://localhost:3800
- /api/* -> http://localhost:5000

<!-- 实测修正：原手册写 web 目录下 proxy_server.py 不存在；脚本置于 jnpf-app-vue3/scripts/ -->
"""
from __future__ import annotations

import os
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

PORT = int(os.environ.get("JNPF_H5_PORT", "3800"))
API_TARGET = os.environ.get("JNPF_API_TARGET", "http://localhost:5000").rstrip("/")
ROOT = os.path.normpath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "unpackage", "dist", "build", "web")
)


class ProxyHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def log_message(self, fmt, *args):
        sys.stderr.write("[%s] %s\n" % (self.log_date_time_string(), fmt % args))

    def do_GET(self):
        if self.path.startswith("/api/"):
            return self._proxy()
        return super().do_GET()

    def do_POST(self):
        if self.path.startswith("/api/"):
            return self._proxy()
        self.send_error(405)

    def do_PUT(self):
        if self.path.startswith("/api/"):
            return self._proxy()
        self.send_error(405)

    def do_DELETE(self):
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
    server.serve_forever()


if __name__ == "__main__":
    main()
