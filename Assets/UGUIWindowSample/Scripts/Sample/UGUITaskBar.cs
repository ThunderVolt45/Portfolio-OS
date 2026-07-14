using System.Collections.Generic;
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

        [Header("Layout")]
        [SerializeField] private float taskBarHeight = 64f;
        [SerializeField] private float iconSize = 44f;
        [SerializeField] private float iconSpacing = 10f;
        [Tooltip("화면 하단과 도크 사이 간격(px). macOS 스타일 플로팅 도크.")]
        [SerializeField] private float bottomMargin = 12f;
        [SerializeField] private int sortingOrder = 10;

        [Header("Visibility")]
        [Tooltip("빈 도크(아이콘 0개) 페이드 속도. 클수록 빠름.")]
        [SerializeField] private float visibilityFadeSpeed = 14f;

        private readonly Dictionary<UGUIWindow, UGUITaskIcon> icons = new();

        private UGUIWindowManager subscribedManager;
        private UGUIWindowManager maximizedWindowAreaManager;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private bool isSubscribed;
        private bool registeredMaximizedWindowArea;
        // 아이콘이 하나라도 있으면 도크를 보인다(핀 고정 앱이 없으므로 빈 도크는 숨김).
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
                icons.Add(window, CreateIcon(window));
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
            Destroy(icon.gameObject);
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
                icons.Add(window, CreateIcon(window));
            }

            RefreshItems(null);
            UpdateDockVisibility(false);
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

            var canvas = GetComponent<Canvas>();
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
        }

        // 빈 도크(아이콘 0개)는 숨기고, 창이 하나라도 열리면 다시 보인다.
        // instant=true면 페이드 없이 즉시 적용(활성화 첫 프레임의 플래시 방지).
        private void UpdateDockVisibility(bool instant)
        {
            dockShouldShow = icons.Count > 0;

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
