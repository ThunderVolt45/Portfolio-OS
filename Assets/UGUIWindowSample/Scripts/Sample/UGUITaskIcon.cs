using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// macOS Dock 스타일 작업표시줄 아이콘.
    /// - 색 타일 하이라이트 대신 앱 아이콘을 그대로 노출한다.
    /// - 실행/포커스 상태는 아이콘 아래 점(indicator)으로 표시한다(macOS 관례).
    /// - 최소화된 창은 아이콘을 디밍한다.
    /// - 호버 확대(magnification)는 <see cref="UGUITaskDockMagnifier"/>가 LayoutElement 크기를
    ///   조절해 담당하며, 이 컴포넌트는 상태 시각화와 클릭 처리만 맡는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UGUITaskIcon : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage; // 호버 하이라이트(평소 투명). 라운드 스퀘어.
        [SerializeField] private Image iconImage;       // 앱 아이콘
        [SerializeField] private Image indicatorImage;  // 실행 표시 점

        [Header("Colors")]
        [SerializeField] private Color hoverHighlightColor = new Color(1f, 1f, 1f, 0.14f);
        [SerializeField] private Color indicatorOpenColor = new Color(0.22f, 0.22f, 0.24f, 0.55f);
        [SerializeField] private Color indicatorFocusedColor = new Color(0.13f, 0.13f, 0.15f, 0.95f);
        [Range(0f, 1f)]
        [SerializeField] private float minimizedIconAlpha = 0.55f;

        private UGUIWindow targetWindow;
        private bool isHovered;

        public UGUIWindow TargetWindow
        {
            get { return targetWindow; }
        }

        public RectTransform RectTransform
        {
            get { return (RectTransform)transform; }
        }

        private void Awake()
        {
            ResolveReferences();
            ApplyHoverHighlight();
        }

        // 하위호환: 코드 생성 fallback(UGUITaskBar.CreateDefaultIcon)에서 배경/아이콘만 넘긴다.
        public void SetReferences(Image background, Image icon)
        {
            backgroundImage = background;
            iconImage = icon;
            ResolveReferences();
        }

        public void Initialize(UGUIWindow window)
        {
            ResolveReferences();

            targetWindow = window;
            isHovered = false;
            Refresh(false);
        }

        public void Refresh(bool focused)
        {
            if (targetWindow == null)
            {
                return;
            }

            bool minimized = targetWindow.WindowMode == UGUIWindowMode.Minimized;

            if (iconImage != null)
            {
                iconImage.sprite = targetWindow.WindowIcon;
                iconImage.enabled = targetWindow.WindowIcon != null;
                iconImage.preserveAspect = true;

                Color c = iconImage.color;
                c.a = minimized ? minimizedIconAlpha : 1f;
                iconImage.color = c;
            }

            if (indicatorImage != null)
            {
                // 작업표시줄 아이콘은 곧 "열린 창" → 항상 점을 표시하고, 포커스면 진하게.
                indicatorImage.enabled = true;
                indicatorImage.color = focused ? indicatorFocusedColor : indicatorOpenColor;
            }

            ApplyHoverHighlight();
        }

        public void SetHovered(bool hovered)
        {
            if (isHovered == hovered)
            {
                return;
            }

            isHovered = hovered;
            ApplyHoverHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || targetWindow == null)
            {
                return;
            }

            if (targetWindow.WindowMode == UGUIWindowMode.Minimized)
            {
                targetWindow.RestoreFromMinimized();
            }
            else
            {
                targetWindow.Focus();
            }
        }

        private void ApplyHoverHighlight()
        {
            if (backgroundImage == null)
            {
                return;
            }

            Color c = hoverHighlightColor;
            if (!isHovered)
            {
                c.a = 0f;
            }

            backgroundImage.color = c;
        }

        private void ResolveReferences()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }
        }
    }
}
