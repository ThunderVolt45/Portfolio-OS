#!/usr/bin/env python3
"""Portfolio-OS WebGL 로컬 개발 서버.

Unity WebGL 빌드는 file://로 못 열고, wasm은 정확한 MIME(application/wasm)가
있어야 WebAssembly.instantiateStreaming이 동작한다. 표준 http.server가
플랫폼별로 wasm MIME를 안 붙일 수 있어 명시적으로 매핑한다.

Brotli/gzip 압축 빌드 지원:
  Unity의 압축 포맷 = Brotli이고 Decompression Fallback = Disabled면
  빌드 산출물이 `WebGL.wasm.br`, `WebGL.data.br`, `WebGL.framework.js.br`처럼
  `.br`(또는 `.gz`)로 나온다. 이때 서버가 `Content-Encoding: br`(또는 gzip)과
  '원본' Content-Type(application/wasm 등)을 보내야 브라우저가 네이티브로
  풀어서 로드한다. 이 서버가 그 헤더를 붙인다.
  (GitHub Pages는 이 헤더를 못 넣어 이 방식이 불가 → 배포는 CLAUDE.md §4-C 참조.
   로컬에서는 우리가 헤더를 제어하므로 .br을 그대로 서빙해 용량을 줄일 수 있다.)

HTTPS:
  브라우저는 Brotli(`Content-Encoding: br`)를 보안 컨텍스트(HTTPS 또는 localhost)
  에서만 신뢰한다. localhost는 http로도 되지만, 모니터 셸/서브도메인 등 실제
  배포와 유사한 환경 검증을 위해 이 서버는 기본적으로 HTTPS로 뜬다.
  인증서가 없으면 openssl로 self-signed `localhost` 인증서를 Tools/.certs에
  1회 생성해 캐시한다(자체서명이라 브라우저 경고는 정상 — '고급 → 계속' 진행).

사용:
  python Tools/serve.py [port] [dir] [--http] [--cert PATH --key PATH]
기본:
  port=8000, dir=Build/WebGL, HTTPS 켜짐(자동 self-signed 인증서)
"""
import argparse
import http.server
import os
import socketserver
import ssl
import subprocess
import sys

# 상대경로 인자는 실행 위치(CWD)가 아니라 저장소 루트(= 이 스크립트의 부모의 부모)
# 기준으로 해석한다. 그래야 어느 디렉터리에서 실행해도 Build/WebGL을 정확히 찾는다.
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CERT_DIR = os.path.join(REPO_ROOT, "Tools", ".certs")
CERT_FILE = os.path.join(CERT_DIR, "localhost-cert.pem")
KEY_FILE = os.path.join(CERT_DIR, "localhost-key.pem")

# 사전 압축 파일 확장자 → HTTP Content-Encoding 매핑
ENCODINGS = {".br": "br", ".gz": "gzip"}


def parse_args():
    p = argparse.ArgumentParser(add_help=True, description="Portfolio-OS WebGL 로컬 서버")
    p.add_argument("port", nargs="?", type=int, default=8000, help="포트(기본 8000)")
    p.add_argument("dir", nargs="?", default="Build/WebGL",
                   help="서빙 디렉터리(기본 Build/WebGL, 저장소 루트 기준)")
    p.add_argument("--http", action="store_true", help="TLS 끄고 평문 HTTP로 서빙")
    p.add_argument("--cert", help="TLS 인증서(PEM) 경로(직접 지정)")
    p.add_argument("--key", help="TLS 개인키(PEM) 경로(직접 지정)")
    return p.parse_args()


def ensure_cert():
    """self-signed localhost 인증서를 준비해 (cert, key) 경로를 반환.

    이미 있으면 재사용, 없으면 openssl로 생성. openssl이 없으면 None을 돌려
    호출부가 HTTP로 폴백하도록 한다.
    """
    if os.path.isfile(CERT_FILE) and os.path.isfile(KEY_FILE):
        return CERT_FILE, KEY_FILE

    os.makedirs(CERT_DIR, exist_ok=True)
    cmd = [
        "openssl", "req", "-x509", "-newkey", "rsa:2048",
        "-keyout", KEY_FILE, "-out", CERT_FILE,
        "-days", "825", "-nodes",
        "-subj", "/CN=localhost",
        "-addext", "subjectAltName=DNS:localhost,IP:127.0.0.1",
    ]
    try:
        subprocess.run(cmd, check=True, capture_output=True)
    except FileNotFoundError:
        print("[serve] openssl을 찾을 수 없어 self-signed 인증서를 만들 수 없습니다.",
              file=sys.stderr)
        return None
    except subprocess.CalledProcessError as e:
        print("[serve] 인증서 생성 실패:\n" + e.stderr.decode(errors="replace"),
              file=sys.stderr)
        return None
    print(f"[serve] self-signed 인증서 생성: {CERT_FILE}")
    return CERT_FILE, KEY_FILE


def make_handler(directory):
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
            super().__init__(*args, directory=directory, **kwargs)

        def guess_type(self, path):
            # `WebGL.wasm.br` → `.br`을 벗겨 '원본' 타입(application/wasm)을 돌려준다.
            base, ext = os.path.splitext(path)
            if ext.lower() in ENCODINGS:
                path = base
            return super().guess_type(path)

        def send_head(self):
            # 실제로 서빙하는 파일이 사전 압축본이면 Content-Encoding을 예약해 둔다.
            # (send_head 안에서 end_headers가 호출되므로 여기서 미리 판별)
            self._encoding = None
            fs_path = self.translate_path(self.path)
            if os.path.isfile(fs_path):
                ext = os.path.splitext(fs_path)[1].lower()
                self._encoding = ENCODINGS.get(ext)
            return super().send_head()

        def end_headers(self):
            # 반복 검증 중 캐시로 인한 stale 로드 방지
            self.send_header("Cache-Control", "no-store")
            if getattr(self, "_encoding", None):
                self.send_header("Content-Encoding", self._encoding)
                self._encoding = None
            super().end_headers()

        def log_message(self, fmt, *args):
            sys.stderr.write("[serve] " + (fmt % args) + "\n")

    return Handler


# 멀티스레드: pdf.js 뷰어/WebGL은 아이콘·워커·PDF 등 수십 개를 동시 요청한다.
# 단일 스레드 서버는 keep-alive 연결에 물려 backlog가 넘치고 연결이 거부된다.
class Server(socketserver.ThreadingMixIn, http.server.HTTPServer):
    allow_reuse_address = True  # TIME_WAIT 재바인딩 시 WinError 10048 회피
    daemon_threads = True       # 종료 시 요청 스레드가 프로세스를 붙잡지 않도록


def main():
    args = parse_args()
    directory = args.dir if os.path.isabs(args.dir) else os.path.join(REPO_ROOT, args.dir)
    if not os.path.isdir(directory):
        sys.exit(f"[serve] 오류: 서빙할 디렉터리가 없습니다: {directory}\n"
                 f"        먼저 Unity에서 'Portfolio → Build WebGL (Release)'로 빌드하세요.")

    # TLS 인증서 결정
    cert = key = None
    if not args.http:
        if args.cert and args.key:
            cert, key = args.cert, args.key
        else:
            pair = ensure_cert()
            if pair:
                cert, key = pair
            else:
                print("[serve] HTTPS를 켤 수 없어 HTTP로 폴백합니다(--http).", file=sys.stderr)

    scheme = "https" if cert else "http"
    with Server(("", args.port), make_handler(directory)) as httpd:
        if cert:
            ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
            ctx.load_cert_chain(certfile=cert, keyfile=key)
            httpd.socket = ctx.wrap_socket(httpd.socket, server_side=True)
        print(f"[serve] Serving '{directory}' at {scheme}://localhost:{args.port}/")
        if scheme == "https":
            print("[serve] self-signed 인증서 → 브라우저 경고는 정상('고급 → 계속' 진행).")
        httpd.serve_forever()


if __name__ == "__main__":
    main()
