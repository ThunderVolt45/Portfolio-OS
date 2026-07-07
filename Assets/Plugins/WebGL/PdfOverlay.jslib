// PDF 오버레이 브리지 (WebGL 전용)
// Unity 캔버스 "위"에 실제 pdf.js viewer(iframe) DOM을 좌표동기 오버레이로 띄운다.
// 포커스 창일 때 라이브(선택·검색·폼·링크 native), 백그라운드일 때 텍스처 스냅샷으로 스왑.
// C#이 창 콘텐츠의 Unity 스크린 rect(좌하단 원점, px)를 넘기면 여기서 CSS 좌표로 변환·클리핑한다.
mergeInto(LibraryManager.library, {

  // 오버레이 1개 생성. viewerUrl = StreamingAssets 기준 절대 URL(?file=... 포함).
  // z-order 때문에 iframe은 1개만 두고, 포커스된 창의 문서로 src를 전환한다(PdfOverlaySetSrc).
  PdfOverlayInit: function (viewerUrlPtr) {
    var viewerUrl = UTF8ToString(viewerUrlPtr);
    if (window.__pdfOverlay) return;
    var wrap = document.createElement('div');
    wrap.id = 'pdf-overlay';
    wrap.style.cssText = 'position:fixed;left:0;top:0;width:0;height:0;overflow:hidden;z-index:10;display:none;background:#fff;';
    var ifr = document.createElement('iframe');
    ifr.style.cssText = 'border:0;display:block;';
    ifr.src = viewerUrl;
    wrap.appendChild(ifr);
    document.body.appendChild(wrap);
    window.__pdfOverlay = { wrap: wrap, iframe: ifr, url: viewerUrl };
  },

  // 단일 오버레이의 문서를 전환한다(포커스된 창의 문서). 같은 문서면 리로드하지 않는다.
  PdfOverlaySetSrc: function (viewerUrlPtr) {
    var viewerUrl = UTF8ToString(viewerUrlPtr);
    var o = window.__pdfOverlay; if (!o) return;
    if (o.url === viewerUrl) return;
    o.url = viewerUrl;
    o.iframe.src = viewerUrl;
  },

  // Unity 캔버스 엘리먼트를 찾는다(빌드 템플릿에 따라 id가 다를 수 있어 폴백 체인).
  // 내부 헬퍼가 아니라 각 함수에서 직접 찾음(jslib는 함수 간 공유가 까다로움).

  // 창 콘텐츠의 Unity 스크린 rect(x,y=좌하단, w,h; Unity px) → CSS 좌표 변환 + 캔버스 경계 클리핑.
  PdfOverlaySetRect: function (xPx, yPx, wPx, hPx) {
    var o = window.__pdfOverlay; if (!o) return;
    var canvas = (typeof Module !== 'undefined' && Module.canvas) ||
                 document.querySelector('#unity-canvas') ||
                 document.querySelector('canvas');
    if (!canvas) return;
    var r = canvas.getBoundingClientRect();
    var sx = r.width / canvas.width;     // Unity(드로잉버퍼) px → CSS px
    var sy = r.height / canvas.height;

    // CSS 좌표(좌상단 원점). Unity y는 아래가 0이므로 뒤집는다.
    var cssLeft = r.left + xPx * sx;
    var cssTop  = r.top + (canvas.height - (yPx + hPx)) * sy;
    var cssW = wPx * sx;
    var cssH = hPx * sy;

    // 캔버스 경계로 교집합 클리핑 (창이 캔버스 밖/OS 크롬 밑으로 나가도 안 새어나오게)
    var clipL = Math.max(cssLeft, r.left);
    var clipT = Math.max(cssTop, r.top);
    var clipR = Math.min(cssLeft + cssW, r.right);
    var clipB = Math.min(cssTop + cssH, r.bottom);
    var ow = Math.max(0, clipR - clipL);
    var oh = Math.max(0, clipB - clipT);

    o.wrap.style.left = clipL + 'px';
    o.wrap.style.top = clipT + 'px';
    o.wrap.style.width = ow + 'px';
    o.wrap.style.height = oh + 'px';
    // iframe은 원래 콘텐츠 크기 유지 + 잘린 만큼 음수 마진으로 밀어 정렬 유지
    o.iframe.style.width = cssW + 'px';
    o.iframe.style.height = cssH + 'px';
    o.iframe.style.marginLeft = (cssLeft - clipL) + 'px';
    o.iframe.style.marginTop = (cssTop - clipT) + 'px';
  },

  PdfOverlayShow: function () {
    var o = window.__pdfOverlay; if (o) o.wrap.style.display = 'block';
  },

  PdfOverlayHide: function () {
    var o = window.__pdfOverlay; if (o) o.wrap.style.display = 'none';
  },

  // 현재 보이는 뷰포트를 "실제 페이지 캔버스들"에서 직접 합성 → base64 PNG → SendMessage.
  // pdf.js가 각 페이지를 <canvas>로 렌더하므로, 현재 스크롤에서 보이는 부분만 drawImage로
  // 잘라 붙여 픽셀 퍼펙트 스냅샷을 만든다(무의존, 현재 페이지/스크롤 그대로 반영).
  // 실패 시 빈 문자열(C#이 스냅샷 없이 진행).
  PdfOverlaySnapshot: function (goNamePtr, methodPtr) {
    var goName = UTF8ToString(goNamePtr);
    var method = UTF8ToString(methodPtr);
    var o = window.__pdfOverlay;
    function reply(s) { try { SendMessage(goName, method, s); } catch (e) {} }
    if (!o) { reply(''); return; }
    try {
      var idoc = o.iframe.contentDocument;
      var de = idoc && idoc.documentElement;
      var vc = idoc && idoc.querySelector('#viewerContainer');
      var canvases = idoc ? idoc.querySelectorAll('#viewer .canvasWrapper canvas, #viewer .page canvas') : [];
      if (!de || !vc || !canvases.length) { reply(''); return; }

      // 출력 프레임 = iframe 전체 뷰포트(=콘텐츠 rect). 페이지-only가 아니라 콘텐츠 rect
      // 전체 크기로 만들어야 RawImage가 1:1로 표시되어 세로 늘어남이 없다.
      var fw = de.clientWidth, fh = de.clientHeight;
      if (fw === 0 || fh === 0) { reply(''); return; }
      var vcRect = vc.getBoundingClientRect(); // 툴바 아래 페이지 영역(iframe 좌표, top=실측 툴바 높이)

      var i, cv, r;
      var ratio = window.devicePixelRatio || 1;
      for (i = 0; i < canvases.length; i++) {
        r = canvases[i].getBoundingClientRect();
        if (r.width > 0) { ratio = canvases[i].width / r.width; break; }
      }

      var out = document.createElement('canvas');
      out.width = Math.max(1, Math.round(fw * ratio));
      out.height = Math.max(1, Math.round(fh * ratio));
      var ctx = out.getContext('2d');
      ctx.fillStyle = '#525659'; // 페이지 영역 배경
      ctx.fillRect(0, 0, out.width, out.height);

      // 툴바 영역(0 ~ vcRect.top)을 실제 툴바 배경색으로 채움. 높이는 하드코딩이 아니라
      // vcRect.top(=페이지 컨테이너 위치=실측 툴바 높이)이라 반응형/숨김에도 적응.
      var tb = idoc.querySelector('#toolbarContainer') || idoc.querySelector('.toolbar');
      var tbColor = (tb && tb.ownerDocument.defaultView) ? tb.ownerDocument.defaultView.getComputedStyle(tb).backgroundColor : '';
      if (!tbColor || tbColor === 'rgba(0, 0, 0, 0)' || tbColor === 'transparent') tbColor = '#38383d';
      ctx.fillStyle = tbColor;
      ctx.fillRect(0, 0, out.width, Math.max(0, Math.round(vcRect.top * ratio)));

      // 페이지 캔버스: viewerContainer 영역으로 클리핑, iframe 뷰포트 좌표 그대로 그림(툴바 침범 방지)
      for (i = 0; i < canvases.length; i++) {
        cv = canvases[i]; r = cv.getBoundingClientRect();
        var ix1 = Math.max(vcRect.left, r.left), iy1 = Math.max(vcRect.top, r.top);
        var ix2 = Math.min(vcRect.right, r.right), iy2 = Math.min(vcRect.bottom, r.bottom);
        if (ix2 <= ix1 || iy2 <= iy1 || r.width === 0 || r.height === 0) continue;
        var sx = cv.width / r.width, sy = cv.height / r.height;
        try {
          ctx.drawImage(cv,
            (ix1 - r.left) * sx, (iy1 - r.top) * sy, (ix2 - ix1) * sx, (iy2 - iy1) * sy,
            ix1 * ratio, iy1 * ratio, (ix2 - ix1) * ratio, (iy2 - iy1) * ratio);
        } catch (e) {}
      }
      var url = out.toDataURL('image/png'); // 동일 오리진이라 taint 없음
      reply(url.substring(url.indexOf(',') + 1));
    } catch (e) { reply(''); }
  }
});
