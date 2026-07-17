using System;
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
    ///
    /// 아이콘의 정체성은 창 인스턴스가 아니라 <see cref="AppClassName"/>(앱 클래스명)이다.
    /// 덕분에 창이 없는 상태(=핀 고정된 런처)로도 존재할 수 있고, 클릭하면 창을 새로 띄운다.
    /// 창이 열리면 <see cref="UGUITaskBar"/>가 <see cref="Bind"/>로 붙이고, 닫히면 <see cref="Unbind"/>한다.
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
        private string appClassName;
        private bool isPinned;
        private bool isHovered;

        // 프리팹에서 읽어온 런처용 아이콘·이름(Resources.Load 반복 방지용 캐시).
        private Sprite pinnedSprite;
        private string pinnedTitle;
        private bool prefabResolved;

        public UGUIWindow TargetWindow
        {
            get { return targetWindow; }
        }

        /// <summary>이 아이콘이 대표하는 앱의 클래스명. 창이 없어도 유지되는 정체성.</summary>
        public string AppClassName
        {
            get { return appClassName; }
        }

        /// <summary>핀 고정 아이콘이면 true. 창을 닫아도 도크에 남아 런처로 동작한다.</summary>
        public bool IsPinned
        {
            get { return isPinned; }
        }

        /// <summary>창이 열려 있으면 true(최소화 포함).</summary>
        public bool IsRunning
        {
            get { return targetWindow != null; }
        }

        /// <summary>포인터가 이 아이콘 위에 있으면 true. 도크가 이름 툴팁을 띄울 때 참조한다.</summary>
        public bool IsHovered
        {
            get { return isHovered; }
        }

        /// <summary>
        /// 툴팁에 표시할 앱 이름. 실행 중이면 창의 실제 제목을, 아니면 프리팹에 저장된 제목을 쓴다.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (targetWindow != null && !string.IsNullOrWhiteSpace(targetWindow.WindowTitle))
                {
                    return targetWindow.WindowTitle;
                }

                return ResolvePinnedTitle();
            }
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

        /// <summary>열린 창에 대응하는 아이콘으로 초기화한다(핀 고정 아님 → 창이 닫히면 사라진다).</summary>
        public void Initialize(UGUIWindow window)
        {
            ResolveReferences();

            targetWindow = window;
            appClassName = window != null ? window.GetType().Name : null;
            isPinned = false;
            isHovered = false;
            Refresh(false);
        }

        /// <summary>창 없이 앱만 대표하는 핀 고정 런처로 초기화한다.</summary>
        public void InitializePinned(string className)
        {
            ResolveReferences();

            targetWindow = null;
            appClassName = className;
            isPinned = true;
            isHovered = false;
            pinnedSprite = null;
            pinnedTitle = null;
            prefabResolved = false;
            Refresh(false);
        }

        /// <summary>핀 아이콘에 실행된 창을 연결한다.</summary>
        public void Bind(UGUIWindow window)
        {
            targetWindow = window;
            Refresh(false);
        }

        /// <summary>창이 닫혔을 때 연결을 끊고 런처 상태로 되돌린다.</summary>
        public void Unbind()
        {
            targetWindow = null;
            Refresh(false);
        }

        public void Refresh(bool focused)
        {
            bool running = targetWindow != null;
            bool minimized = running && targetWindow.WindowMode == UGUIWindowMode.Minimized;

            if (iconImage != null)
            {
                // 실행 중이면 창 인스턴스가, 아니면(핀 런처) 프리팹이 아이콘의 출처다.
                Sprite sprite = running ? targetWindow.WindowIcon : ResolvePinnedSprite();

                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
                iconImage.preserveAspect = true;

                Color c = iconImage.color;
                c.a = minimized ? minimizedIconAlpha : 1f;
                iconImage.color = c;
            }

            if (indicatorImage != null)
            {
                // macOS 관례: 점은 "실행 중"을 뜻한다. 핀만 된 앱은 점이 없다.
                indicatorImage.enabled = running;
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
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            // 데스크톱 아이콘과 달리 도크는 한 번 클릭으로 실행한다(macOS/Windows 관례).
            if (targetWindow == null)
            {
                Launch();
            }
            else if (targetWindow.WindowMode == UGUIWindowMode.Minimized)
            {
                targetWindow.RestoreFromMinimized();
            }
            else
            {
                targetWindow.Focus();
            }
        }

        private void Launch()
        {
            if (string.IsNullOrWhiteSpace(appClassName))
            {
                UGUIWindowLog.LogError("Task icon has no app class name to launch.");
                return;
            }

            Type windowType = ResolveWindowType();
            if (windowType == null)
            {
                UGUIWindowLog.LogError($"Cannot launch unknown window class: {appClassName}");
                return;
            }

            // 단일 인스턴스 창은 매니저가 풀에서 기존 창을 되살리므로 중복 생성되지 않는다.
            // 생성 결과는 여기서 붙이지 않는다 — 매니저의 OnManagedWindowOpened를 받은
            // UGUITaskBar가 이 아이콘에 Bind해, 실행 경로가 도크든 데스크톱이든 동일하게 흐른다.
            UGUIWindowManager.CreateWindow(windowType);
        }

        // UGUIIcon·PortfolioBootstrap과 동일한 해석 방식(WebGL 코드 스트리핑 안전 확인됨).
        private Type ResolveWindowType()
        {
            Type windowType = Type.GetType($"UGUIWindow.{appClassName}", false);
            return windowType != null && typeof(UGUIWindow).IsAssignableFrom(windowType)
                ? windowType
                : null;
        }

        // 핀 런처는 창 인스턴스가 없으므로 아이콘·이름을 창 프리팹에서 읽는다.
        private void ResolvePrefab()
        {
            if (prefabResolved)
            {
                return;
            }

            prefabResolved = true;

            if (string.IsNullOrWhiteSpace(appClassName))
            {
                return;
            }

            var windowPrefab = Resources.Load<GameObject>($"Windows/{appClassName}");
            if (windowPrefab != null && windowPrefab.TryGetComponent(out UGUIWindow prefabWindow))
            {
                pinnedSprite = prefabWindow.WindowIcon;
                pinnedTitle = prefabWindow.DefaultTitle;
            }
        }

        private Sprite ResolvePinnedSprite()
        {
            ResolvePrefab();
            return pinnedSprite;
        }

        private string ResolvePinnedTitle()
        {
            ResolvePrefab();
            return string.IsNullOrWhiteSpace(pinnedTitle) ? appClassName : pinnedTitle;
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
