// PDF 오버레이 브리지 (WebGL 전용)
// Unity 캔버스 "위"에 실제 pdf.js viewer(iframe) DOM을 좌표동기 오버레이로 띄운다.
// 포커스 창일 때 라이브(선택·검색·폼·링크 native), 백그라운드일 때 텍스처 스냅샷으로 스왑.
// C#이 창 콘텐츠의 Unity 스크린 rect(좌하단 원점, px)를 넘기면 여기서 CSS 좌표로 변환·클리핑한다.
mergeInto(LibraryManager.library, {

  // 오버레이 1개 생성(PoC). viewerUrl = StreamingAssets 기준 절대 URL(?file=... 포함).
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
    window.__pdfOverlay = { wrap: wrap, iframe: ifr };
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

  // iframe viewer의 현재 page canvas를 캡처 → base64 PNG → goName.method(base64) 로 SendMessage.
  // 실패 시 빈 문자열을 넘긴다(C#이 스냅샷 없이 진행).
  PdfOverlaySnapshot: function (goNamePtr, methodPtr) {
    var goName = UTF8ToString(goNamePtr);
    var method = UTF8ToString(methodPtr);
    var o = window.__pdfOverlay;
    function reply(s) { try { SendMessage(goName, method, s); } catch (e) {} }
    if (!o) { reply(''); return; }
    try {
      var doc = o.iframe.contentDocument;
      var canvas = doc && doc.querySelector('#viewer canvas');
      if (!canvas) { reply(''); return; }
      var url = canvas.toDataURL('image/png'); // 동일 오리진이라 taint 없음
      reply(url.substring(url.indexOf(',') + 1));
    } catch (e) { reply(''); }
  }
});
