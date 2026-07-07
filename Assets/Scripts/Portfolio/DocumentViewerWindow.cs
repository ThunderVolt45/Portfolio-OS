using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// PDF 문서 뷰어 — 포커스-스왑 오버레이 방식.
    /// 포커스 상태에서는 실제 pdf.js viewer(iframe) DOM을 창 위에 좌표동기 오버레이로 띄워
    /// 선택·검색·폼·하이퍼링크를 native로 지원한다. 백그라운드로 밀리면 현재 화면을
    /// 텍스처 스냅샷으로 굳혀 RawImage로 표시(다른 창이 정상적으로 위를 덮음).
    /// WebGL 전용. 에디터/비 WebGL에서는 안내 텍스트만 표시한다.
    /// </summary>
    public class DocumentViewerWindow : UGUIWindow
    {
        [Header("Document")]
        [SerializeField] private string documentRelativePath = "docs/resume.pdf";

        // PDF별 전용 창(서브클래스)이 문서 경로·제목을 지정할 수 있도록 오버라이드 지점을 연다.
        // 기본 구현은 직렬화 필드/고정값을 그대로 사용한다.
        protected virtual string DocumentPath { get { return documentRelativePath; } }
        protected virtual string DocumentTitle { get { return "Resume.pdf"; } }

        private RectTransform _contentRT;
        private Canvas _canvas;
        private RawImage _snapshotImage;
        private TMP_Text _statusText;

        private bool _overlayInited;
        private bool _live;          // 현재 라이브 오버레이 표시 중인가
        private bool _subscribed;
        private string _viewerUrl;   // 이 창의 문서를 가리키는 viewer URL

        // 단일 오버레이(iframe)를 z-order 때문에 한 번에 하나만 띄운다 → 현재 라이브 창을 전역 추적.
        // 새 창이 라이브가 되기 직전, 이전 라이브 창을 (오버레이가 아직 그 문서를 보이는 동안)
        // 동기 스냅샷으로 굳혀야 각 창이 자기 문서를 정확히 보존한다.
        private static DocumentViewerWindow s_live;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PdfOverlayInit(string viewerUrl);
        [DllImport("__Internal")] private static extern void PdfOverlaySetSrc(string viewerUrl);
        [DllImport("__Internal")] private static extern void PdfOverlaySetRect(float x, float y, float w, float h);
        [DllImport("__Internal")] private static extern void PdfOverlayShow();
        [DllImport("__Internal")] private static extern void PdfOverlayHide();
        [DllImport("__Internal")] private static extern void PdfOverlaySnapshot(string goName, string method);
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            Resize(560, 720);
            Move(0, 0);
            EnsureUi();
            Subscribe();
            InitOverlay();
            // 창이 방금 열렸으면 포커스 상태로 가정 → 라이브
            GoLive();
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
            HideOverlay();
        }

        private void Start()
        {
            SetWindowTitle(DocumentTitle);
        }

        private void EnsureUi()
        {
            var content = transform.Find("Content") as RectTransform;
            if (content == null) return;
            _contentRT = content;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

            if (_statusText == null)
            {
                _statusText = content.GetComponentInChildren<TMP_Text>(true);
                if (_statusText != null)
                {
                    _statusText.alignment = TextAlignmentOptions.Center;
                    _statusText.fontSize = 16;
                    _statusText.text = "";
                }
            }

            // 백그라운드 스냅샷 표시용 RawImage (콘텐츠 가득)
            if (_snapshotImage == null)
            {
                var go = new GameObject("SnapshotImage", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(content, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _snapshotImage = go.GetComponent<RawImage>();
                _snapshotImage.color = Color.white;
                _snapshotImage.enabled = false;
            }
        }

        private void InitOverlay()
        {
            if (_overlayInited) return;
            // viewer.html은 pdfjs/web/ 아래, 문서는 StreamingAssets/docs/ 아래
            // → viewer 기준 상대경로 ../../docs/... (동일 오리진, origin 검증 통과)
            _viewerUrl = Application.streamingAssetsPath +
                "/pdfjs/web/viewer.html?file=../../" + DocumentPath + "#zoom=page-width";
#if UNITY_WEBGL && !UNITY_EDITOR
            PdfOverlayInit(_viewerUrl);
            _overlayInited = true;
#else
            _overlayInited = true;
            if (_statusText != null)
                _statusText.text = "PDF 뷰어는 WebGL 빌드에서만 동작합니다 (에디터 미지원).";
#endif
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            var mgr = UGUIWindowManager.Instance;
            if (mgr == null) return;
            mgr.OnManagedWindowFocused.AddListener(OnAnyWindowFocused);
            mgr.OnManagedWindowMinimized.AddListener(OnThisWindowHidden);
            mgr.OnManagedWindowClosed.AddListener(OnThisWindowHidden);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            var mgr = UGUIWindowManager.Instance;
            if (mgr != null)
            {
                mgr.OnManagedWindowFocused.RemoveListener(OnAnyWindowFocused);
                mgr.OnManagedWindowMinimized.RemoveListener(OnThisWindowHidden);
                mgr.OnManagedWindowClosed.RemoveListener(OnThisWindowHidden);
            }
            _subscribed = false;
        }

        // 어떤 창이든 포커스되면: 그게 나면 라이브로 전환, 아니면 (내가 라이브였다면) 스냅샷으로 굳힘.
        private void OnAnyWindowFocused(UGUIWindow w)
        {
            if (w == (UGUIWindow)this) GoLive();
            else FreezeToSnapshot();
        }

        private void OnThisWindowHidden(UGUIWindow w)
        {
            if (w == (UGUIWindow)this) HideOverlay();
        }

        // 이 창을 라이브로 만든다. 단일 오버레이를 내 문서로 전환하되,
        // 전환 전에 이전 라이브 창을 (오버레이가 아직 그 문서를 보이는 동안) 동기 스냅샷으로 굳힌다.
        private void GoLive()
        {
            if (!_overlayInited) return;

            if (s_live != null && s_live != this)
                s_live.FreezeToSnapshot();   // 오버레이가 아직 이전 문서를 보임 → 정확한 스냅샷

            s_live = this;
            _live = true;
            if (_snapshotImage != null) _snapshotImage.enabled = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            PdfOverlaySetSrc(_viewerUrl);    // 단일 오버레이를 내 문서로 전환(같은 문서면 리로드 생략)
            SyncRect();
            PdfOverlayShow();
#endif
        }

        // 현재 라이브면: 지금 화면(=내 문서)을 스냅샷으로 굳히고 오버레이를 숨긴다.
        // 반드시 오버레이가 이 창의 문서를 보이는 동안 호출되어야 정확하다.
        private void FreezeToSnapshot()
        {
            if (!_overlayInited || !_live) return;
            _live = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            // 동기 콜백(OnSnapshotCaptured)에서 텍스처 적용 + 오버레이 숨김
            PdfOverlaySnapshot(gameObject.name, nameof(OnSnapshotCaptured));
#endif
        }

        private void HideOverlay()
        {
            _live = false;
            if (s_live == this) s_live = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_overlayInited) PdfOverlayHide();
#endif
        }

        // jslib SendMessage 콜백: 백그라운드 스냅샷(base64 PNG) 수신
        public void OnSnapshotCaptured(string base64Png)
        {
            try
            {
                if (!string.IsNullOrEmpty(base64Png) && _snapshotImage != null)
                {
                    byte[] png = Convert.FromBase64String(base64Png);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(png);
                    tex.Apply();
                    _snapshotImage.texture = tex;
                    _snapshotImage.enabled = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[DocumentViewerWindow] 스냅샷 텍스처 실패: " + e.Message);
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            // 스냅샷 적용 후 라이브 오버레이 숨김(순서: 굳힌 뒤 숨겨 깜빡임 최소화)
            PdfOverlayHide();
#endif
        }

        private void LateUpdate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_live && _overlayInited) SyncRect();
#endif
        }

        // 콘텐츠 RectTransform의 Unity 스크린 rect(좌하단 원점, px)를 계산해 오버레이에 전달
        private void SyncRect()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_contentRT == null) return;
            var corners = new Vector3[4];
            _contentRT.GetWorldCorners(corners); // 0:BL 1:TL 2:TR 3:BR
            Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera : null;
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            float x = bl.x, y = bl.y, w = tr.x - bl.x, h = tr.y - bl.y;
            if (w > 0 && h > 0) PdfOverlaySetRect(x, y, w, h);
#endif
        }
    }
}
