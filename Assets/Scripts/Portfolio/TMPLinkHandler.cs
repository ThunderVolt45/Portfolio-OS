using TMPro;
using UGUIWindow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PortfolioOS
{
    /// <summary>
    /// TextMeshPro의 &lt;link="url"&gt; 태그를 실제 하이퍼링크처럼 동작시킨다.
    ///
    /// - 클릭: 링크 글리프 위를 클릭하면 Application.OpenURL(linkID)로 연다.
    ///   (WebGL에서는 클릭 = 유저 제스처이므로 window.open이 팝업 차단 없이 새 탭을 연다.)
    /// - hover: 링크 글리프 위에 포인터가 오면 손가락(포인터) 커서로 전환한다.
    ///
    /// UGUICursorManager.OnPoint(InputSystem)가 마우스 이동마다 커서를 Default로
    /// 되돌리므로, LateUpdate에서 매 프레임 "링크 위"일 때 손가락 커서를 재지정해
    /// 이를 이긴다(입력 콜백은 LateUpdate보다 먼저 실행됨 → 같은 프레임 내에서 손가락이 최종 승리).
    ///
    /// 링크 라벨(TMP)에는 콘텐츠 베이커(PortfolioAboutContent / PortfolioUGUISampleContent)가
    /// 이 컴포넌트를 붙이고 커서 텍스처/핫스팟을 주입한다. 이 클래스는 순수 런타임 로직만 담당.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
    {
        [Tooltip("링크 hover 시 표시할 손가락 커서 텍스처(베이커가 주입).")]
        public Texture2D linkCursor;

        [Tooltip("손가락 커서의 핫스팟(픽셀). 손끝 위치.")]
        public Vector2 linkCursorHotspot;

        private TMP_Text _text;
        private Canvas _canvas;
        private bool _wasOverLink;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _canvas = GetComponentInParent<Canvas>();
        }

        private Camera EventCamera
        {
            get
            {
                if (_canvas == null)
                {
                    _canvas = GetComponentInParent<Canvas>();
                }
                if (_canvas == null)
                {
                    return null;
                }
                // Overlay 캔버스는 스크린 좌표를 그대로 쓰므로 카메라가 null이어야 한다.
                return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_text == null)
            {
                return;
            }

            int idx = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, eventData.pressEventCamera);
            if (idx == -1)
            {
                return;
            }

            string url = _text.textInfo.linkInfo[idx].GetLinkID();
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 텍스트 렉트를 완전히 벗어나면 즉시 기본 커서로 복원.
            if (_wasOverLink)
            {
                UGUICursorManager.SetCursor(UGUICursor.Default);
                _wasOverLink = false;
            }
        }

        private void LateUpdate()
        {
            if (_text == null || linkCursor == null)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 pos = mouse.position.ReadValue();
            bool overLink = TMP_TextUtilities.FindIntersectingLink(_text, pos, EventCamera) != -1;

            if (overLink)
            {
                // 링크 위에 있는 동안은 매 프레임 손가락 커서를 재지정한다
                // (OnPoint가 이동마다 Default로 덮어쓰므로 매 프레임 되잡아야 함).
                Cursor.SetCursor(linkCursor, linkCursorHotspot, CursorMode.Auto);
            }
            else if (_wasOverLink)
            {
                // 링크에서 벗어난 첫 프레임에만 기본 커서로 복원.
                UGUICursorManager.SetCursor(UGUICursor.Default);
            }

            _wasOverLink = overLink;
        }
    }
}
