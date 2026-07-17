using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class UGUITaskBar : MonoBehaviour
    {
        private static UGUITaskBar _instance;

        public static UGUITaskBar Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UGUITaskBar>(FindObjectsInactive.Include);

                    if (_instance != null)
                    {
                        _instance.gameObject.SetActive(true);
                    }
                    else
                    {
                        var taskBarObject = new GameObject(
                            "UGUITaskBar",
                            typeof(RectTransform),
                            typeof(Canvas),
                            typeof(CanvasRenderer),
                            typeof(Image),
                            typeof(GraphicRaycaster),
                            typeof(UGUITaskBar));

                        _instance = taskBarObject.GetComponent<UGUITaskBar>();
                    }
                }

                return _instance;
            }
        }

        [Header("UI Elements")]
        [SerializeField] private RectTransform iconContainer;
        [SerializeField] private UGUITaskIcon taskIconPrefab = null;

        [Header("Pinned Apps")]
        [Tooltip("도크에 항상 고정해 둘 앱의 창 클래스명(예: AboutWindow). 여기 적힌 순서대로 왼쪽부터 놓이며, " +
                 "창이 없어도 남아 클릭하면 실행된다. 실행 중인 창은 자기 앱의 핀 아이콘에 자동으로 붙는다.")]
        [SerializeField] private List<string> pinnedApps = new();

        [Header("Layout")]
        [SerializeField] private float taskBarHeight = 64f;
        [SerializeField] private float iconSize = 44f;
        [SerializeField] private float iconSpacing = 10f;
        [Tooltip("화면 하단과 도크 사이 간격(px). macOS 스타일 플로팅 도크.")]
        [SerializeField] private float bottomMargin = 12f;
        [SerializeField] private int sortingOrder = 10;

        [Header("Tooltip")]
        [Tooltip("호버한 아이콘 위에 앱 이름을 띄우는 툴팁. 비워두면 런타임에 기본 툴팁을 만든다.")]
        [SerializeField] private RectTransform tooltipRoot;
        [SerializeField] private TMP_Text tooltipLabel;
        [Tooltip("도크 위쪽 끝과 툴팁 사이 간격(px).")]
        [SerializeField] private float tooltipGap = 10f;
        [Tooltip("툴팁 배경이 글자 주위로 확보하는 여백(px, 가로/세로).")]
        [SerializeField] private Vector2 tooltipPadding = new Vector2(12f, 7f);
        [SerializeField] private float tooltipFadeSpeed = 20f;

        [Header("Visibility")]
        [Tooltip("빈 도크(아이콘 0개) 페이드 속도. 클수록 빠름.")]
        [SerializeField] private float visibilityFadeSpeed = 14f;

        [Tooltip("전체화면일 때 화면 아래쪽 이 범위 안으로 포인터가 들어오면 도크가 다시 나온다. " +
                 "캔버스 단위가 아니라 실제 화면 픽셀이라, 화면 배율이나 창 크기가 바뀌어도 손에 잡히는 폭은 같다.")]
        [SerializeField] private float fullScreenRevealZonePixels = 32f;

        // 창별 아이콘. 핀 아이콘에 붙은 창도 여기 등록되므로, 창 → 아이콘 조회는 항상 이 딕셔너리로 한다.
        private readonly Dictionary<UGUIWindow, UGUITaskIcon> icons = new();

        // 앱 클래스명 → 핀 아이콘. 창이 없어도 살아 있는 런처들.
        private readonly Dictionary<string, UGUITaskIcon> pinnedIcons = new();

        private UGUIWindowManager subscribedManager;
        private UGUIWindowManager maximizedWindowAreaManager;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas canvas;
        private CanvasGroup tooltipGroup;
        private string tooltipShownText;

        private bool isSubscribed;
        private bool registeredMaximizedWindowArea;
        // 아이콘이 하나라도 있으면 도크를 보인다. 핀 고정 앱이 있으면 사실상 항상 보이고,
        // 핀이 하나도 없을 때만 빈 도크(창 0개)가 숨는다.
        private bool dockShouldShow;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            rectTransform = transform as RectTransform;

            EnsureDefaultLayout();
            BuildPinnedIcons();
        }

        private void OnEnable()
        {
            ConfigureTaskBarRect();
            SubscribeToManager();
            RebuildFromManager();
            UpdateDockVisibility(true); // 활성화 시점의 상태로 즉시 스냅(빈 상태면 플래시 없이 숨김)
        }

        private void Update()
        {
            // 전체화면 여부와 포인터 위치는 매 프레임 바뀔 수 있으므로 여기서 다시 판단한다.
            UpdateDockVisibility(false);
            UpdateTooltip();

            if (canvasGroup == null)
            {
                return;
            }

            float target = dockShouldShow ? 1f : 0f;
            if (!Mathf.Approximately(canvasGroup.alpha, target))
            {
                float t = 1f - Mathf.Exp(-visibilityFadeSpeed * Time.unscaledDeltaTime);
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, t);
                if (Mathf.Abs(canvasGroup.alpha - target) < 0.004f)
                {
                    canvasGroup.alpha = target;
                }

                bool visible = canvasGroup.alpha > 0.01f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromManager();
            ClearMaximizedWindowArea();
        }

        private void OnDestroy()
        {
            UnsubscribeFromManager();

            if (_instance == this)
            {
                _instance = null;
            }

            ClearMaximizedWindowArea();
        }

        public void AttachToDesktop(UGUIDesktop desktop)
        {
            if (desktop == null)
            {
                return;
            }

            transform.SetParent(desktop.transform, false);
            ConfigureTaskBarRect();
            transform.SetAsLastSibling();
        }

        private void SubscribeToManager()
        {
            if (isSubscribed)
            {
                return;
            }

            subscribedManager = UGUIWindowManager.Instance;
            if (subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnManagedWindowOpened.AddListener(HandleWindowOpened);
            subscribedManager.OnManagedWindowClosed.AddListener(HandleWindowClosed);
            subscribedManager.OnManagedWindowFocused.AddListener(HandleWindowFocused);
            subscribedManager.OnManagedWindowMinimized.AddListener(HandleWindowMinimized);

            isSubscribed = true;
        }

        private void UnsubscribeFromManager()
        {
            if (!isSubscribed || subscribedManager == null)
            {
                return;
            }

            subscribedManager.OnManagedWindowOpened.RemoveListener(HandleWindowOpened);
            subscribedManager.OnManagedWindowClosed.RemoveListener(HandleWindowClosed);
            subscribedManager.OnManagedWindowFocused.RemoveListener(HandleWindowFocused);
            subscribedManager.OnManagedWindowMinimized.RemoveListener(HandleWindowMinimized);

            subscribedManager = null;
            isSubscribed = false;
        }

        private void RebuildFromManager()
        {
            if (subscribedManager == null)
            {
                return;
            }

            foreach (var window in subscribedManager.ManagedVisibleWindows)
            {
                HandleWindowOpened(window);
            }
        }

        private void HandleWindowOpened(UGUIWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (!icons.ContainsKey(window))
            {
                icons.Add(window, AcquireIcon(window));
            }

            RefreshItems(window);
            UpdateDockVisibility(false);
        }

        private void HandleWindowClosed(UGUIWindow window)
        {
            if (window == null || !icons.TryGetValue(window, out var icon))
            {
                return;
            }

            icons.Remove(window);

            // 핀 아이콘은 창이 닫혀도 런처로 남고, 창 전용 아이콘만 도크에서 사라진다.
            if (icon.IsPinned)
            {
                icon.Unbind();
            }
            else
            {
                Destroy(icon.gameObject);
            }

            UpdateDockVisibility(false);
        }

        private void HandleWindowFocused(UGUIWindow window)
        {
            RefreshItems(window);
        }

        private void HandleWindowMinimized(UGUIWindow window)
        {
            if (window == null)
            {
                return;
            }

            if (!icons.ContainsKey(window))
            {
                icons.Add(window, AcquireIcon(window));
            }

            RefreshItems(null);
            UpdateDockVisibility(false);
        }

        // 열린 창에 붙일 아이콘을 고른다. 자기 앱의 핀 아이콘이 비어 있으면 그것을 재사용하고,
        // 없으면 창 전용 아이콘을 새로 만들어 핀 아이콘들 뒤에 붙인다.
        private UGUITaskIcon AcquireIcon(UGUIWindow window)
        {
            // 다중 인스턴스 앱은 창마다 아이콘을 따로 둔다. 창 여러 개가 핀 아이콘 하나를 공유하면
            // 어느 창이 포커스인지 점 하나로 표현할 수 없기 때문이다. 핀 아이콘은 런처로 남는다.
            if (!window.allowMultipleInstance
                && pinnedIcons.TryGetValue(window.GetType().Name, out var pinnedIcon)
                && pinnedIcon != null
                && !pinnedIcon.IsRunning)
            {
                pinnedIcon.Bind(window);
                return pinnedIcon;
            }

            return CreateIcon(window);
        }

        private void BuildPinnedIcons()
        {
            foreach (var appClassName in pinnedApps)
            {
                if (string.IsNullOrWhiteSpace(appClassName) || pinnedIcons.ContainsKey(appClassName))
                {
                    continue;
                }

                UGUITaskIcon icon = taskIconPrefab != null
                    ? Instantiate(taskIconPrefab, iconContainer)
                    : CreateDefaultIcon();

                icon.name = $"{appClassName} Icon (Pinned)";
                icon.InitializePinned(appClassName);

                pinnedIcons.Add(appClassName, icon);
            }
        }

        private UGUITaskIcon CreateIcon(UGUIWindow window)
        {
            UGUITaskIcon icon = taskIconPrefab != null
                ? Instantiate(taskIconPrefab, iconContainer)
                : CreateDefaultIcon();

            icon.name = $"{window.name} Icon";
            icon.Initialize(window);

            return icon;
        }

        private UGUITaskIcon CreateDefaultIcon()
        {
            var iconObject = new GameObject(
                "TaskIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(UGUITaskIcon));

            iconObject.transform.SetParent(iconContainer, false);

            var iconRect = iconObject.transform as RectTransform;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            var background = iconObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f); // 평소 투명(호버 하이라이트용)

            var layoutElement = iconObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0f;

            var windowIconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            windowIconObject.transform.SetParent(iconObject.transform, false);

            var windowIconRect = windowIconObject.transform as RectTransform;
            windowIconRect.anchorMin = Vector2.zero;
            windowIconRect.anchorMax = Vector2.one;
            windowIconRect.offsetMin = new Vector2(5f, 5f);
            windowIconRect.offsetMax = new Vector2(-5f, -5f);

            var windowIconImage = windowIconObject.GetComponent<Image>();
            windowIconImage.raycastTarget = false;
            windowIconImage.enabled = false;
            windowIconImage.preserveAspect = true;

            var icon = iconObject.GetComponent<UGUITaskIcon>();
            icon.SetReferences(background, windowIconImage);

            return icon;
        }

        private void EnsureDefaultLayout()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            ConfigureTaskBarRect();

            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (iconContainer == null)
            {
                iconContainer = CreateDefaultContainer();
            }

            EnsureTooltip();
        }

        private void EnsureTooltip()
        {
            if (tooltipRoot == null)
            {
                tooltipRoot = CreateDefaultTooltip();
            }

            // 도크 루트가 곧 HorizontalLayoutGroup이므로, 제외하지 않으면 툴팁이 아이콘처럼 한 칸을 차지한다.
            var layoutElement = tooltipRoot.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = tooltipRoot.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;

            // 도크 하단 중앙 기준 → anchoredPosition.x에 아이콘의 로컬 x를 그대로 넣을 수 있다.
            tooltipRoot.anchorMin = new Vector2(0.5f, 0f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0f);
            tooltipRoot.pivot = new Vector2(0.5f, 0f);

            tooltipGroup = tooltipRoot.GetComponent<CanvasGroup>();
            if (tooltipGroup == null)
            {
                tooltipGroup = tooltipRoot.gameObject.AddComponent<CanvasGroup>();
            }

            tooltipGroup.alpha = 0f;
            // 툴팁이 포인터를 가로채면 아이콘 호버가 끊겨 툴팁이 깜빡인다.
            tooltipGroup.blocksRaycasts = false;
            tooltipGroup.interactable = false;

            if (tooltipLabel == null)
            {
                tooltipLabel = tooltipRoot.GetComponentInChildren<TMP_Text>(true);
            }
        }

        // 프리팹에 툴팁이 지정되지 않았을 때의 기본 툴팁. 외부 에셋에 의존하지 않도록 기본 Image로 만든다.
        private RectTransform CreateDefaultTooltip()
        {
            var tooltipObject = new GameObject(
                "TaskIconTooltip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LayoutElement));

            tooltipObject.transform.SetParent(iconContainer, false);

            var background = tooltipObject.GetComponent<Image>();
            background.color = new Color(0.17f, 0.17f, 0.19f, 0.92f);
            background.raycastTarget = false;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

            labelObject.transform.SetParent(tooltipObject.transform, false);

            var labelRect = labelObject.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.color = Color.white;
            label.raycastTarget = false;

            tooltipLabel = label;

            return tooltipObject.transform as RectTransform;
        }

        // 빈 도크(아이콘 0개)는 숨기고, 창이 하나라도 열리면 다시 보인다.
        // 전체화면 중에는 macOS처럼 도크를 숨기고, 화면 아래쪽에 포인터를 대면 다시 꺼낸다.
        // instant=true면 페이드 없이 즉시 적용(활성화 첫 프레임의 플래시 방지).
        private void UpdateDockVisibility(bool instant)
        {
            dockShouldShow = (pinnedIcons.Count > 0 || icons.Count > 0)
                && (!IsFullScreenActive() || IsPointerInRevealZone());

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (instant && canvasGroup != null)
            {
                canvasGroup.alpha = dockShouldShow ? 1f : 0f;
                canvasGroup.blocksRaycasts = dockShouldShow;
                canvasGroup.interactable = dockShouldShow;
            }
        }

        // 호버 판정은 UGUITaskDockMagnifier가 이미 매 프레임 내리고 있으므로(레이캐스트와 같은 좌표계),
        // 여기서는 그 결과(UGUITaskIcon.IsHovered)를 읽기만 한다. 포인터를 다시 계산하지 않는 것이 중요하다 —
        // 확대되는 아이콘과 이름이 표시되는 아이콘이 어긋나지 않는다.
        private void UpdateTooltip()
        {
            if (tooltipRoot == null || tooltipGroup == null)
            {
                return;
            }

            UGUITaskIcon hovered = FindHoveredIcon();

            // 도크 자체가 숨어 있으면(전체화면 등) 이름도 같이 감춘다.
            bool show = hovered != null && dockShouldShow;

            if (show)
            {
                string label = hovered.DisplayName;
                if (label != tooltipShownText)
                {
                    tooltipShownText = label;
                    tooltipLabel.text = label;
                    ResizeTooltipToText();

                    // 나중에 만들어진 창 아이콘보다 뒤에 그려지지 않도록 최상단으로 올린다.
                    tooltipRoot.SetAsLastSibling();
                }

                // 아이콘 X 중심을 도크 로컬 좌표로 구한다(매그니파이어와 동일한 방식).
                // anchoredPosition을 쓰지 않는 이유는 LayoutGroup이 아이콘 앵커를 바꾸기 때문이다.
                float iconX = iconContainer.InverseTransformPoint(hovered.RectTransform.position).x;
                tooltipRoot.anchoredPosition = new Vector2(iconX, taskBarHeight + tooltipGap);
            }

            float target = show ? 1f : 0f;
            if (!Mathf.Approximately(tooltipGroup.alpha, target))
            {
                float t = 1f - Mathf.Exp(-tooltipFadeSpeed * Time.unscaledDeltaTime);
                tooltipGroup.alpha = Mathf.Lerp(tooltipGroup.alpha, target, t);
                if (Mathf.Abs(tooltipGroup.alpha - target) < 0.004f)
                {
                    tooltipGroup.alpha = target;
                }
            }
        }

        private UGUITaskIcon FindHoveredIcon()
        {
            for (int i = 0; i < iconContainer.childCount; i++)
            {
                var icon = iconContainer.GetChild(i).GetComponent<UGUITaskIcon>();
                if (icon != null && icon.IsHovered)
                {
                    return icon;
                }
            }

            return null;
        }

        // LayoutGroup/ContentSizeFitter 대신 직접 크기를 준다. 툴팁은 레이아웃에서 제외된(ignoreLayout)
        // 자식이라, 자동 크기 조절 컴포넌트를 얹으면 도크 자신의 ContentSizeFitter와 얽히기 쉽다.
        private void ResizeTooltipToText()
        {
            if (tooltipLabel == null)
            {
                return;
            }

            Vector2 textSize = tooltipLabel.GetPreferredValues(tooltipShownText);
            tooltipRoot.sizeDelta = new Vector2(
                textSize.x + tooltipPadding.x * 2f,
                textSize.y + tooltipPadding.y * 2f);
        }

        private bool IsFullScreenActive()
        {
            var manager = subscribedManager != null ? subscribedManager : UGUIWindowManager.Instance;
            return manager != null && manager.HasFullScreenWindow;
        }

        // 도크가 숨어 있을 때는 raycast를 받지 못하므로, 포인터 이벤트가 아니라 좌표로 판정한다.
        private bool IsPointerInRevealZone()
        {
            var parentRect = rectTransform != null ? rectTransform.parent as RectTransform : null;
            if (parentRect == null)
            {
                return false;
            }

            if (!UGUIWindowManager.TryGetPointerScreenPosition(out Vector2 screenPosition))
            {
                return false;
            }

            // 캔버스가 ScreenSpaceOverlay이므로 카메라는 null.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    null,
                    out Vector2 localPointer))
            {
                return false;
            }

            // 도크가 이미 나와 있으면 감지 범위를 도크 높이까지 넓혀,
            // 포인터를 도크 위에 올려둔 채로 아이콘을 누를 수 있게 한다.
            Rect area = parentRect.rect;
            float zoneHeight = dockShouldShow
                ? taskBarHeight + bottomMargin
                : UGUIWindowManager.PixelsToCanvasUnits(fullScreenRevealZonePixels, canvas);

            return localPointer.y <= area.yMin + zoneHeight
                && localPointer.x >= area.xMin
                && localPointer.x <= area.xMax;
        }

        private RectTransform CreateDefaultContainer()
        {
            // macOS 도크: 루트 자체가 아이콘 컨테이너 역할을 맡아, 배경 패널이 아이콘 수에 맞춰
            // 폭을 hug 하도록 HorizontalLayoutGroup + ContentSizeFitter를 루트에 구성한다.
            var layout = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.padding = new RectOffset(14, 14, 8, 12);
            layout.spacing = iconSpacing;
            layout.childAlignment = TextAnchor.LowerCenter; // 아래 정렬 → 확대 시 위로 성장
            layout.childControlWidth = true;
            layout.childControlHeight = false; // 세로는 매그니파이어가 직접 제어(도크 위로 성장)
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            if (gameObject.GetComponent<UGUITaskDockMagnifier>() == null)
            {
                gameObject.AddComponent<UGUITaskDockMagnifier>();
            }

            return rectTransform;
        }

        private void ConfigureTaskBarRect()
        {
            if (rectTransform == null)
            {
                return;
            }

            // macOS 스타일: 하단 중앙에 콘텐츠 폭만큼만 차지하는 플로팅 도크.
            // 폭(sizeDelta.x)은 ContentSizeFitter가 아이콘 수에 맞춰 제어하므로 여기서 건드리지 않는다.
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottomMargin);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, taskBarHeight);

            var manager = UGUIWindowManager.Instance;
            if (manager == null)
            {
                return;
            }

            // 최대화 창이 도크 밑으로 숨지 않도록 하단에 도크 높이 + 간격만큼 예약.
            manager.SetMaximizedWindowOffsets(
                new Vector2(0f, taskBarHeight + bottomMargin),
                Vector2.zero);
            maximizedWindowAreaManager = manager;
            registeredMaximizedWindowArea = true;
        }

        private void ClearMaximizedWindowArea()
        {
            if (!registeredMaximizedWindowArea)
            {
                return;
            }

            if (maximizedWindowAreaManager != null)
            {
                maximizedWindowAreaManager.ClearMaximizedWindowOffsets();
            }

            maximizedWindowAreaManager = null;
            registeredMaximizedWindowArea = false;
        }

        private void RefreshItems(UGUIWindow focusedWindow)
        {
            foreach (var icon in icons.Values)
            {
                icon.Refresh(icon.TargetWindow == focusedWindow);
            }
        }
    }
}
