using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// macOS 오버레이 스크롤바처럼 스크롤바를 유휴 시 페이드아웃하고, 스크롤/호버 시 페이드인한다.
    ///
    /// 동작:
    /// - 스크롤(휠·드래그)이 발생하면 나타나고, 마지막 활동 후 <see cref="hideDelay"/> 초가 지나면 사라진다.
    /// - <see cref="revealOnContentHover"/>가 켜져 있으면 창 콘텐츠 위에 포인터가 있는 동안에도 보인다
    ///   (마우스 사용자가 핸들을 잡을 수 있도록). 끄면 macOS 트랙패드처럼 "스크롤할 때만" 나타난다.
    /// - 숨김 상태에서는 CanvasGroup.blocksRaycasts를 꺼서 보이지 않는 게터가 콘텐츠 클릭을 가로채지 않는다.
    ///
    /// 각 Scrollbar GameObject에 CanvasGroup을 런타임에 부착해 alpha만 제어하며, ScrollRect의
    /// 가시성 모드(Permanent)나 레이아웃(<see cref="UGUIWindowContent"/>)은 건드리지 않는다.
    ///
    /// MPUIKit/프레임워크 base(UGUIWindowContent 등, upstream 동기화 대상)를 수정하지 않도록
    /// 포트폴리오 레이어에 additive 컴포넌트로 둔다([[UGUIWindowRounding]]와 동일한 이유).
    /// </summary>
    // 프레임워크 base(UGUIWindowContent)가 dirty 프레임에 스크롤바 오프셋을 flush(0)로 되돌리므로,
    // 기본 실행 순서(0)보다 뒤에 돌려 매 프레임 끝여백을 다시 적용한다(리사이즈 시 깜빡임 방지).
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(ScrollRect))]
    public class UGUIScrollbarAutoHide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("마지막 스크롤/호버 후 스크롤바를 숨기기까지의 대기 시간(초).")]
        [SerializeField] private float hideDelay = 1.2f;

        [Tooltip("페이드 인/아웃에 걸리는 시간(초).")]
        [SerializeField] private float fadeDuration = 0.28f;

        [Tooltip("켜면 콘텐츠 위에 포인터가 있는 동안 스크롤바가 보인다. 끄면 스크롤할 때만 나타난다.")]
        [SerializeField] private bool revealOnContentHover = true;

        [Tooltip("스크롤바 양 끝(세로바=위·아래, 가로바=좌·우)의 여백(px). macOS처럼 헤더/모서리에 닿지 않게 한다.")]
        [SerializeField] private float endMargin = 6f;

        private ScrollRect scrollRect;
        private CanvasGroup verticalGroup;
        private CanvasGroup horizontalGroup;

        private float lastActivityTime;
        private bool pointerInside;
        private bool hasFaded;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }

            // 창을 열었을 때 스크롤바를 잠깐 보여준 뒤 자연스럽게 숨긴다(macOS처럼 위치를 알려줌).
            MarkActive();
            hasFaded = false;
        }

        private void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            MarkActive();
        }

        /// <summary>스크롤바를 즉시 활성(보임) 상태로 만들고 유휴 타이머를 리셋한다.</summary>
        public void MarkActive()
        {
            lastActivityTime = Time.unscaledTime;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            MarkActive();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
        }

        private void LateUpdate()
        {
            EnsureGroups();
            ApplyEndMargins();

            bool hovering = revealOnContentHover && pointerInside;
            bool active = hovering || (Time.unscaledTime - lastActivityTime) < hideDelay;

            float target = active ? 1f : 0f;
            float step = fadeDuration > 0f ? Time.unscaledDeltaTime / fadeDuration : 1f;

            ApplyAlpha(verticalGroup, target, step);
            ApplyAlpha(horizontalGroup, target, step);
            hasFaded = true;
        }

        private void ApplyAlpha(CanvasGroup group, float target, float step)
        {
            if (group == null)
            {
                return;
            }

            // 첫 프레임은 즉시 목표값으로 스냅해 켜질 때 번쩍임을 방지한다.
            float alpha = hasFaded ? Mathf.MoveTowards(group.alpha, target, step) : target;
            group.alpha = alpha;

            // 숨김 상태에선 클릭이 아래 콘텐츠로 통과하도록 레이캐스트를 끈다.
            bool visible = alpha > 0.5f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }

        // macOS 스크롤바처럼 헤더/모서리에 닿지 않도록 스크롤바 rect의 스크롤 축 양 끝에 여백을 준다.
        // 두께 축 오프셋(.x for 세로, .y for 가로)은 프레임워크가 설정한 값을 보존한다.
        private void ApplyEndMargins()
        {
            if (scrollRect == null || endMargin <= 0f)
            {
                return;
            }

            ApplyEndMargin(scrollRect.verticalScrollbar, true);
            ApplyEndMargin(scrollRect.horizontalScrollbar, false);
        }

        private void ApplyEndMargin(Scrollbar scrollbar, bool vertical)
        {
            if (scrollbar == null)
            {
                return;
            }

            RectTransform rt = scrollbar.transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            Vector2 min = rt.offsetMin;
            Vector2 max = rt.offsetMax;

            if (vertical)
            {
                min.y = endMargin;   // 아래 여백
                max.y = -endMargin;  // 위 여백(헤더 밑단과 간격)
            }
            else
            {
                min.x = endMargin;   // 왼쪽 여백
                max.x = -endMargin;  // 오른쪽 여백
            }

            rt.offsetMin = min;
            rt.offsetMax = max;
        }

        private void EnsureGroups()
        {
            if (scrollRect == null)
            {
                return;
            }

            if (verticalGroup == null && scrollRect.verticalScrollbar != null)
            {
                verticalGroup = GetOrAddGroup(scrollRect.verticalScrollbar.gameObject);
            }

            if (horizontalGroup == null && scrollRect.horizontalScrollbar != null)
            {
                horizontalGroup = GetOrAddGroup(scrollRect.horizontalScrollbar.gameObject);
            }
        }

        private static CanvasGroup GetOrAddGroup(GameObject go)
        {
            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = go.AddComponent<CanvasGroup>();
            }

            return group;
        }
    }
}
