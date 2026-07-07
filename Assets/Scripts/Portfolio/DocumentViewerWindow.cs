using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// PDF 문서 뷰어 — 포커스-스왑 오버레이 방식.
    /// 각 창은 자기 전용 pdf.js viewer(iframe)를 DOM에 1개 만들어 살려 둔다(id로 키잉).
    /// 포커스(또는 최상단) 창일 때만 자기 iframe을 좌표동기로 보여(선택·검색·폼·링크 native),
    /// 백그라운드로 밀리면 자기 iframe을 텍스처 스냅샷으로 굳혀 RawImage로 표시하고 iframe을 숨긴다
    /// (z-order상 iframe은 항상 캔버스 위라, 한 번에 하나만 보여야 다른 창이 정상적으로 덮인다).
    /// iframe을 파괴하지 않고 숨겼다 다시 보이므로 문서 전환 시 리로드/상태소실이 없다.
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
        private bool _live;          // 현재 내 iframe이 보이는(라이브) 상태인가
        private bool _subscribed;
        private string _viewerUrl;   // 이 창의 문서를 가리키는 viewer URL
        private string _overlayId;   // 이 창 전용 iframe 키(타입명 = 인스턴스 단일이라 유일)

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PdfOverlayInit(string id, string viewerUrl);
        [DllImport("__Internal")] private static extern void PdfOverlaySetRect(string id, float x, float y, float w, float h);
        [DllImport("__Internal")] private static extern void PdfOverlayShow(string id);
        [DllImport("__Internal")] private static extern void PdfOverlayHide(string id);
        [DllImport("__Internal")] private static extern void PdfOverlaySnapshot(string id, string goName, string method);
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            _overlayId = GetType().Name;   // 타입별 단일 인스턴스 → iframe 키로 안정적
            Resize(560, 720);
            Move(0, 0);
            EnsureUi();
            Subscribe();
            InitOverlay();
            // 창이 방금 열렸으면 최상단(포커스)로 가정 → 라이브
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
            PdfOverlayInit(_overlayId, _viewerUrl);   // 이 창 전용 iframe 생성(존재하면 유지)
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
            // Open()은 포커스 이벤트를 발생시키지 않으므로 Opened도 구독해야
            // "다른 PDF 창이 새로 열릴 때" 내가 백그라운드로 내려간다.
            mgr.OnManagedWindowOpened.AddListener(OnAnyWindowActivated);
            mgr.OnManagedWindowFocused.AddListener(OnAnyWindowActivated);
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
                mgr.OnManagedWindowOpened.RemoveListener(OnAnyWindowActivated);
                mgr.OnManagedWindowFocused.RemoveListener(OnAnyWindowActivated);
                mgr.OnManagedWindowMinimized.RemoveListener(OnThisWindowHidden);
                mgr.OnManagedWindowClosed.RemoveListener(OnThisWindowHidden);
            }
            _subscribed = false;
        }

        // 어떤 창이 열리거나 포커스되면: 그게 나면 라이브(내 iframe 표시),
        // 아니면 (내가 라이브였다면) 내 iframe을 스냅샷으로 굳히고 숨긴다.
        private void OnAnyWindowActivated(UGUIWindow w)
        {
            if (w == (UGUIWindow)this) GoLive();
            else FreezeToSnapshot();
        }

        private void OnThisWindowHidden(UGUIWindow w)
        {
            if (w == (UGUIWindow)this) HideOverlay();
        }

        // 이 창을 라이브로: 내 전용 iframe을 (숨겨져 있었다면) 다시 보인다 → 리로드 없음, 상태 보존.
        private void GoLive()
        {
            if (!_overlayInited) return;
            _live = true;
            if (_snapshotImage != null) _snapshotImage.enabled = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            PdfOverlayShow(_overlayId);
            SyncRect();
#endif
        }

        // 현재 라이브면: 내 iframe의 현재 화면을 스냅샷으로 굳히고(백그라운드 텍스처) iframe을 숨긴다.
        // iframe은 파괴하지 않으므로 다음에 다시 라이브가 될 때 리로드가 없다.
        private void FreezeToSnapshot()
        {
            if (!_overlayInited || !_live) return;
            _live = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            // 동기 콜백(OnSnapshotCaptured)에서 텍스처 적용 + 내 iframe 숨김
            PdfOverlaySnapshot(_overlayId, gameObject.name, nameof(OnSnapshotCaptured));
#endif
        }

        private void HideOverlay()
        {
            _live = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_overlayInited) PdfOverlayHide(_overlayId);
#endif
        }

        // jslib SendMessage 콜백: 내 iframe의 백그라운드 스냅샷(base64 PNG) 수신
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
            // 스냅샷 적용 후 내 iframe 숨김(순서: 굳힌 뒤 숨겨 깜빡임 최소화)
            PdfOverlayHide(_overlayId);
#endif
        }

        private void LateUpdate()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (_live && _overlayInited) SyncRect();
#endif
        }

        // 콘텐츠 RectTransform의 Unity 스크린 rect(좌하단 원점, px)를 계산해 내 오버레이에 전달
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
            if (w > 0 && h > 0) PdfOverlaySetRect(_overlayId, x, y, w, h);
#endif
        }
    }
}
