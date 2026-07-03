// PDF.js <-> Unity WebGL 브리지 (개념 증명)
// 브라우저에서 pdf.js로 PDF 페이지를 canvas에 렌더 -> PNG base64 -> Unity로 SendMessage
mergeInto(LibraryManager.library, {

  // pdf.js 라이브러리를 같은 오리진(StreamingAssets)에서 로드한다.
  PdfJsInit: function (saPathPtr) {
    var saPath = UTF8ToString(saPathPtr);
    if (window.__pdfjsReadyPromise) return;
    window.__pdfjsReadyPromise = new Promise(function (resolve, reject) {
      if (window.pdfjsLib) { resolve(); return; }
      var s = document.createElement('script');
      s.src = saPath + '/pdfjs/pdf.min.js';
      s.onload = function () {
        try {
          window.pdfjsLib.GlobalWorkerOptions.workerSrc = saPath + '/pdfjs/pdf.worker.min.js';
        } catch (e) { console.warn('[PdfJsBridge] workerSrc set failed', e); }
        console.log('[PdfJsBridge] pdf.js loaded from ' + saPath);
        resolve();
      };
      s.onerror = function (e) { console.error('[PdfJsBridge] pdf.js load failed', e); reject(e); };
      document.head.appendChild(s);
    });
  },

  // 지정한 URL의 PDF에서 pageNum(1-base) 페이지를 scale 배율로 렌더한다.
  // 완료 시 goName.okMethod(base64PNG), 실패 시 goName.errMethod(msg) 를 SendMessage로 호출.
  PdfRenderPage: function (urlPtr, pageNum, scale, goNamePtr, okPtr, errPtr) {
    var url = UTF8ToString(urlPtr);
    var goName = UTF8ToString(goNamePtr);
    var okMethod = UTF8ToString(okPtr);
    var errMethod = UTF8ToString(errPtr);

    function fail(msg) {
      console.error('[PdfJsBridge] ' + msg);
      try { SendMessage(goName, errMethod, '' + msg); } catch (e) { console.error(e); }
    }

    if (!window.__pdfjsReadyPromise) { fail('pdfjs not initialized'); return; }

    window.__pdfjsReadyPromise.then(function () {
      return window.pdfjsLib.getDocument(url).promise;
    }).then(function (pdf) {
      return pdf.getPage(pageNum);
    }).then(function (page) {
      var viewport = page.getViewport({ scale: scale });
      var canvas = document.createElement('canvas');
      canvas.width = Math.max(1, Math.floor(viewport.width));
      canvas.height = Math.max(1, Math.floor(viewport.height));
      var ctx = canvas.getContext('2d');
      return page.render({ canvasContext: ctx, viewport: viewport }).promise.then(function () {
        var dataUrl = canvas.toDataURL('image/png');
        var base64 = dataUrl.substring(dataUrl.indexOf(',') + 1);
        console.log('[PdfJsBridge] rendered page ' + pageNum + ' (' + canvas.width + 'x' + canvas.height + ')');
        SendMessage(goName, okMethod, base64);
      });
    }).catch(function (err) {
      fail((err && err.message) ? err.message : ('' + err));
    });
  }
});
