#!/usr/bin/env python3
"""Portfolio-OS WebGL 로컬 개발 서버.

Unity WebGL 빌드는 file://로 못 열고, wasm은 정확한 MIME(application/wasm)가
있어야 WebAssembly.instantiateStreaming이 동작한다. 표준 http.server가
플랫폼별로 wasm MIME를 안 붙일 수 있어 명시적으로 매핑한다.

압축 포맷 = Disabled 빌드를 전제로 한다(Content-Encoding 헤더 불필요).

사용:  python Tools/serve.py [port] [dir]
기본:  port=8000, dir=Build/WebGL
"""
import http.server
import os
import socketserver
import sys

# 상대경로 인자는 실행 위치(CWD)가 아니라 저장소 루트(= 이 스크립트의 부모의 부모)
# 기준으로 해석한다. 그래야 어느 디렉터리에서 실행해도 Build/WebGL을 정확히 찾는다.
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
_dir_arg = sys.argv[2] if len(sys.argv) > 2 else "Build/WebGL"
DIRECTORY = _dir_arg if os.path.isabs(_dir_arg) else os.path.join(REPO_ROOT, _dir_arg)

if not os.path.isdir(DIRECTORY):
    sys.exit(f"[serve] 오류: 서빙할 디렉터리가 없습니다: {DIRECTORY}\n"
             f"        먼저 Unity에서 'Portfolio → Build WebGL (Release)'로 빌드하세요.")


class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".js": "application/javascript",
        ".mjs": "application/javascript",
        ".data": "application/octet-stream",
        ".json": "application/json",
        ".pdf": "application/pdf",
    }

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

    def end_headers(self):
        # 반복 검증 중 캐시로 인한 stale 로드 방지
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, fmt, *args):
        sys.stderr.write("[serve] " + (fmt % args) + "\n")


# 멀티스레드: pdf.js 뷰어/WebGL은 아이콘·워커·PDF 등 수십 개를 동시 요청한다.
# 단일 스레드 서버는 keep-alive 연결에 물려 backlog가 넘치고 연결이 거부된다.
class Server(socketserver.ThreadingMixIn, http.server.HTTPServer):
    allow_reuse_address = True  # TIME_WAIT 재바인딩 시 WinError 10048 회피
    daemon_threads = True       # 종료 시 요청 스레드가 프로세스를 붙잡지 않도록


def main():
    with Server(("", PORT), Handler) as httpd:
        print(f"[serve] Serving '{DIRECTORY}' at http://localhost:{PORT}/")
        httpd.serve_forever()


if __name__ == "__main__":
    main()
