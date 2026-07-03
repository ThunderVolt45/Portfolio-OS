using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// PDF 문서를 창 안에 렌더해 표시하는 뷰어 (개념 증명 — 1페이지만).
    /// PdfJsBridge를 통해 pdf.js가 렌더한 페이지 텍스처를 RawImage로 표시한다.
    /// </summary>
    public class DocumentViewerWindow : UGUIWindow
    {
        [Header("Document")]
        [SerializeField] private string documentPath = "docs/resume.pdf";
        [SerializeField] private int pageNumber = 1;
        [SerializeField] private float renderScale = 1.5f;

        private RawImage _pageImage;
        private AspectRatioFitter _fitter;
        private TMP_Text _statusText;
        private bool _requested;

        protected override void OnEnable()
        {
            base.OnEnable();
            Resize(460, 620);
            Move(0, 0);
            EnsureUi();
            RequestPage();
        }

        private void EnsureUi()
        {
            var content = transform.Find("Content");
            if (content == null) return;

            // 프리팹의 기존 TMP 텍스트를 상태 표시로 재사용
            if (_statusText == null)
            {
                _statusText = content.GetComponentInChildren<TMP_Text>(true);
                if (_statusText != null)
                {
                    _statusText.alignment = TextAlignmentOptions.Center;
                    _statusText.fontSize = 16;
                    _statusText.text = "PDF 렌더링 준비 중...";
                }
            }

            // 페이지 표시용 RawImage (종횡비 유지)
            if (_pageImage == null)
            {
                var go = new GameObject("PageImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                go.transform.SetParent(content, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                _pageImage = go.GetComponent<RawImage>();
                _pageImage.color = Color.white;
                _pageImage.enabled = false;

                _fitter = go.GetComponent<AspectRatioFitter>();
                _fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            }
        }

        private void RequestPage()
        {
            if (_requested) return;
            _requested = true;
            PdfJsBridge.Ensure().RenderPage(documentPath, pageNumber, renderScale, OnPageRendered, OnRenderError);
        }

        private void OnPageRendered(Texture2D tex)
        {
            if (_pageImage != null)
            {
                _pageImage.texture = tex;
                _pageImage.enabled = true;
                if (_fitter != null && tex.height > 0)
                {
                    _fitter.aspectRatio = (float)tex.width / tex.height;
                }
            }
            if (_statusText != null) _statusText.text = "";
        }

        private void OnRenderError(string message)
        {
            if (_statusText != null) _statusText.text = "PDF 렌더 실패:\n" + message;
            Debug.LogError("[DocumentViewerWindow] " + message);
        }

        private void Start()
        {
            SetWindowTitle("Resume.pdf");
        }
    }
}
