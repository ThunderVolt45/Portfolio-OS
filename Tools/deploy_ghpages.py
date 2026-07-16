#!/usr/bin/env python3
"""Portfolio-OS WebGL → GitHub Pages 배포.

로컬 `Build/WebGL` 산출물을 `gh-pages` 브랜치 '루트'로 배포한다.

왜 로컬 빌드를 푸시하나:
  MPUIKit(유료 에셋)이 저장소에 없어(gitignore) CI에서 빌드할 수 없다.
  따라서 Unity로 로컬에서 만든 `Build/WebGL`을 그대로 배포한다.
  (`Build/`는 .gitignore 대상이라 main 히스토리에는 들어가지 않는다.)

왜 매 배포마다 orphan(고아) 커밋인가:
  빌드 산출물은 ~37MB다. 배포마다 히스토리에 쌓으면 저장소가 무한정 커진다.
  이 스크립트는 배포마다 '커밋 1개짜리 새 고아 히스토리'를 만들어 force-push 하므로
  gh-pages는 항상 최신 빌드 1개분만 보관한다(gh-pages 히스토리는 보존 가치가 없음).
  작업 저장소는 건드리지 않고 임시 디렉터리에서 수행한다.

Brotli / .nojekyll (CLAUDE.md §4-C):
  빌드는 Brotli + Decompression Fallback = Enabled 로 만든다. GitHub Pages는
  `Content-Encoding` 헤더를 넣어줄 수 없지만, Unity 내장 JS 디컴프레서가 클라이언트에서
  `.br`을 풀어주므로 헤더 없이도 동작한다. `.nojekyll`을 넣어 Jekyll 전처리도 끈다.

사용:
  python Tools/deploy_ghpages.py             # 확인 프롬프트 후 배포
  python Tools/deploy_ghpages.py --yes       # 프롬프트 없이 배포
  python Tools/deploy_ghpages.py --verify    # 배포 후 공개 URL 응답까지 확인
"""
import argparse
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.request

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# 배포 산출물에 반드시 있어야 하는 것들(빌드 누락/경로 오지정 조기 차단)
REQUIRED = ["index.html", "Build"]

# GitHub 파일 1개 하드 리밋. 넘으면 push 자체가 거부된다.
HARD_LIMIT = 100 * 1024 * 1024
WARN_LIMIT = 50 * 1024 * 1024


def log(msg):
    print(f"[deploy] {msg}")


def die(msg):
    sys.exit(f"[deploy] 오류: {msg}")


def git(*args, cwd=None, check=True, capture=True):
    return subprocess.run(
        ["git", *args], cwd=cwd, check=check,
        capture_output=capture, text=True, encoding="utf-8", errors="replace",
    )


def parse_args():
    p = argparse.ArgumentParser(description="Portfolio-OS WebGL → GitHub Pages 배포")
    p.add_argument("--dir", default="Build/WebGL",
                   help="배포할 빌드 디렉터리(기본 Build/WebGL, 저장소 루트 기준)")
    p.add_argument("--branch", default="gh-pages", help="배포 브랜치(기본 gh-pages)")
    p.add_argument("--remote", default="origin", help="푸시할 remote(기본 origin)")
    p.add_argument("--yes", "-y", action="store_true", help="확인 프롬프트 생략")
    p.add_argument("--verify", action="store_true", help="배포 후 공개 URL 응답 확인")
    return p.parse_args()


def check_payload(src):
    """빌드 산출물 유효성 + GitHub 용량 제한 검사."""
    for name in REQUIRED:
        if not os.path.exists(os.path.join(src, name)):
            die(f"'{name}'이 없습니다: {src}\n"
                f"        먼저 Unity에서 'Portfolio → Build WebGL (Release)'로 빌드하세요.")

    total, biggest = 0, (0, "")
    for root, _, files in os.walk(src):
        for f in files:
            fp = os.path.join(root, f)
            size = os.path.getsize(fp)
            total += size
            if size > biggest[0]:
                biggest = (size, os.path.relpath(fp, src))

    log(f"산출물: {total / 1048576:.1f}MB, 최대 파일 {biggest[0] / 1048576:.1f}MB ({biggest[1]})")
    if biggest[0] >= HARD_LIMIT:
        die(f"'{biggest[1]}'이 100MB를 넘어 GitHub이 푸시를 거부합니다. 빌드 경량화 필요.")
    if biggest[0] >= WARN_LIMIT:
        log(f"경고: '{biggest[1]}'이 50MB를 넘습니다(100MB 하드 리밋에 근접).")

    # 빌드 시각을 보여줘 stale 빌드를 배포하는 실수를 알아채게 한다.
    idx = os.path.join(src, "index.html")
    built = time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(os.path.getmtime(idx)))
    log(f"index.html 빌드 시각: {built}")
    return total


def stage(src, dst):
    """빌드 산출물을 임시 디렉터리로 복사하고 .nojekyll을 추가."""
    shutil.copytree(src, dst)
    open(os.path.join(dst, ".nojekyll"), "w").close()  # Jekyll 전처리 비활성화


def pages_url(remote_url):
    """origin URL → GitHub Pages 공개 URL 추정."""
    m = re.search(r"github\.com[:/]([^/]+)/([^/.]+)", remote_url)
    if not m:
        return None
    owner, repo = m.group(1), m.group(2)
    return f"https://{owner.lower()}.github.io/{repo}/"


def verify(url, attempts=20, delay=12):
    """Pages 반영에는 시간이 걸리므로 200이 뜰 때까지 폴링."""
    log(f"공개 URL 확인 중: {url}")
    for i in range(1, attempts + 1):
        try:
            with urllib.request.urlopen(url, timeout=15) as r:
                if r.status == 200:
                    log(f"HTTP 200 OK — 배포 반영 완료: {url}")
                    return True
                log(f"[{i}/{attempts}] HTTP {r.status} — 대기")
        except Exception as e:  # 빌드 중에는 404/연결오류가 정상
            log(f"[{i}/{attempts}] 아직 반영 전({e.__class__.__name__}) — 대기")
        time.sleep(delay)
    log("경고: 시간 내 200을 못 받았습니다. GitHub Pages 빌드가 늦어질 수 있습니다.")
    return False


def main():
    args = parse_args()
    src = args.dir if os.path.isabs(args.dir) else os.path.join(REPO_ROOT, args.dir)
    if not os.path.isdir(src):
        die(f"배포할 디렉터리가 없습니다: {src}\n"
            f"        먼저 Unity에서 'Portfolio → Build WebGL (Release)'로 빌드하세요.")

    check_payload(src)

    remote_url = git("remote", "get-url", args.remote, cwd=REPO_ROOT, check=False).stdout.strip()
    if not remote_url:
        die(f"remote '{args.remote}'를 찾을 수 없습니다.")
    source_sha = git("rev-parse", "--short", "HEAD", cwd=REPO_ROOT).stdout.strip()
    log(f"대상: {remote_url} → '{args.branch}' 브랜치 루트 (소스 커밋 {source_sha})")

    if not args.yes:
        log(f"'{args.branch}' 브랜치를 이 빌드로 '덮어씁니다'(force-push).")
        if input("[deploy] 계속할까요? [y/N] ").strip().lower() not in ("y", "yes"):
            sys.exit("[deploy] 취소했습니다.")

    tmp = tempfile.mkdtemp(prefix="ghpages-")
    work = os.path.join(tmp, "site")
    try:
        stage(src, work)

        git("init", "-q", "-b", args.branch, cwd=work)
        # 커밋 아이덴티티는 작업 저장소 설정을 그대로 따른다(전역 설정이 없어도 실패하지 않도록).
        for key in ("user.name", "user.email"):
            val = git("config", key, cwd=REPO_ROOT, check=False).stdout.strip()
            if val:
                git("config", key, val, cwd=work)

        git("add", "-A", cwd=work)
        msg = (f"deploy: Portfolio-OS WebGL 빌드 ({source_sha})\n\n"
               f"Build/WebGL 산출물을 {args.branch} 루트로 배포.\n"
               f"Brotli + Decompression Fallback → Content-Encoding 헤더 없이 동작.\n"
               f".nojekyll 로 Jekyll 처리 비활성화.")
        git("commit", "-q", "-m", msg, cwd=work)

        n = len(git("ls-files", cwd=work).stdout.splitlines())
        log(f"커밋 완료: {n}개 파일 — force-push 중…")
        git("push", "--force", remote_url, args.branch, cwd=work, capture=False)
        log(f"'{args.branch}' 푸시 완료.")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    url = pages_url(remote_url)
    if args.verify and url:
        verify(url)
    elif url:
        log(f"공개 URL: {url} (반영까지 1~2분 걸릴 수 있음)")


if __name__ == "__main__":
    main()
