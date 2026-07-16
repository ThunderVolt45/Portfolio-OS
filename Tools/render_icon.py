#!/usr/bin/env python3
"""CSS 시안(HTML) → 앱 아이콘 PNG 렌더러.

Portfolio-OS 앱 아이콘은 CSS로 디자인하고(Tools/icons/*.html) 이 스크립트로 렌더링한다.
기존 아이콘 규격에 맞춘 256x256 RGBA PNG를 만든다(타일 240x240, 8px 인셋).

  python Tools/render_icon.py Tools/icons/DpiSetting.html Assets/Portfolio/Icons/DpiSetting.png

헤드리스 Chrome으로 4배(1024px) 렌더한 뒤 256으로 축소한다. 축소는 알파를
프리멀티플라이한 상태에서 4x4 area 평균으로 수행한다 — 그냥 축소하면 투명 픽셀의
검정 RGB가 가장자리에 섞여 어두운 테두리가 생기고, LANCZOS는 링잉으로 타일 밖까지
알파가 번진다(형제 아이콘과 bbox가 어긋남).

렌더 후 Unity에서 텍스처 임포트 설정을 Sprite로 맞출 것
(textureType=Sprite, spriteMode=Single, alphaIsTransparency=true).
"""

import os
import subprocess
import sys
import tempfile

import numpy as np
from PIL import Image

SCALE = 4      # 렌더 배율
SIZE = 256     # 최종 아이콘 한 변(px)

CHROME_CANDIDATES = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "/usr/bin/google-chrome",
    "/usr/bin/chromium",
]


def find_chrome():
    for path in CHROME_CANDIDATES:
        if os.path.exists(path):
            return path
    raise SystemExit("Chrome/Edge를 찾지 못했습니다. CHROME_CANDIDATES에 경로를 추가하세요.")


def render(html_path, out_path):
    html_path = os.path.abspath(html_path)
    if not os.path.exists(html_path):
        raise SystemExit(f"HTML 시안이 없습니다: {html_path}")

    with tempfile.TemporaryDirectory() as tmp:
        # Chrome은 경로 구분자로 슬래시를 기대한다(윈도우 역슬래시를 주면 실패).
        raw = os.path.join(tmp, "raw.png").replace(os.sep, "/")
        subprocess.run(
            [
                find_chrome(),
                "--headless=new",
                "--disable-gpu",
                "--hide-scrollbars",
                f"--user-data-dir={tmp}/profile",  # 기존 프로필 잠금과 충돌 방지
                # 실행 중인 Chrome이 있으면 GPU 프로세스 샌드박스 기동에 실패한다
                # ("GPU process isn't usable. Goodbye.") → 로컬 시안 렌더링이므로 샌드박스 해제.
                "--no-sandbox",
                f"--force-device-scale-factor={SCALE}",
                "--default-background-color=00000000",  # 투명 배경
                f"--window-size={SIZE},{SIZE}",
                f"--screenshot={raw}",
                f"file:///{html_path.replace(os.sep, '/')}",
            ],
            check=True,
            capture_output=True,
        )

        src = np.asarray(Image.open(raw).convert("RGBA")).astype(np.float64)

    rgb, alpha = src[..., :3], src[..., 3:4] / 255.0

    def box_downscale(x):
        h, w, c = x.shape
        return x.reshape(h // SCALE, SCALE, w // SCALE, SCALE, c).mean(axis=(1, 3))

    pm_small = box_downscale(rgb * alpha)          # 프리멀티플라이 후 축소
    a_small = box_downscale(alpha)

    out_rgb = np.divide(pm_small, a_small, out=np.zeros_like(pm_small), where=a_small > 1e-6)
    out = np.concatenate([np.clip(out_rgb, 0, 255), np.clip(a_small * 255, 0, 255)], axis=-1)

    img = Image.fromarray(out.round().astype(np.uint8))
    img.save(out_path)

    print(f"{out_path}  size={img.size}  alpha_bbox={img.split()[3].getbbox()}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(f"usage: python {sys.argv[0]} <input.html> <output.png>")
    render(sys.argv[1], sys.argv[2])
