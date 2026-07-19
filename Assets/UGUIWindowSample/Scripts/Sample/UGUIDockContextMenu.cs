using System;
using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// macOS 도크 스타일의 우클릭 컨텍스트 메뉴.
    /// - 도크 아이콘(<see cref="UGUITaskIcon"/>)을 우클릭하면 그 위에 떠오른다.
    /// - 항목 구성은 호출부가 <see cref="MenuItem"/> 목록으로 넘긴다(예: 열기 / 닫기).
    /// - 항목 밖을 클릭하거나 항목을 고르면 닫힌다.
    ///
    /// 디자인은 MPUIKit(<see cref="MPImage"/>)으로 라운드 사각형 + 외곽선 + 소프트 그림자를 그려
    /// macOS 메뉴 느낌을 낸다. 창 라운딩(<c>UGUIWindowRounding</c>)과 같은 에셋을 쓴다.
    ///
    /// DPI(화면 배율)는 <see cref="UGUIWindowManager"/>가 mainCanvasScaler의 referenceResolution을
    /// screen/dpi 로 맞춰 캔버스 scaleFactor(≈dpi)로 반영한다. 이 메뉴는 전용 루트 캔버스를 따로
    /// 만들지 않고 <b>아이콘의 rootCanvas(=배율 캔버스) 아래에 중첩</b>되므로, 도크·창과 같은 단위로
    /// 그려지며 배율을 그대로 상속한다. 프리팹·씬 배선은 필요 없다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UGUIDockContextMenu : MonoBehaviour
    {
        /// <summary>메뉴 한 줄. 라벨과 눌렀을 때 동작, 그리고 활성 여부.</summary>
        public struct MenuItem
        {
            public string Label;
            public Action Action;
            public bool Enabled;

            public MenuItem(string label, Action action, bool enabled = true)
            {
                Label = label;
                Action = action;
                Enabled = enabled;
            }
        }

        // --- 디자인 상수(캔버스 단위 = 배율 캔버스 기준, 곧 화면 배율에 함께 스케일된다) ---
        private const int MenuSortingOrder = 32000;   // 어떤 창보다도 위에 그린다.
        private const float PanelCornerRadius = 10f;   // macOS Big Sur+ 메뉴 라운딩.
        private const float ItemCornerRadius = 5f;
        private const float ItemHeight = 26f;
        private const float ItemMinWidth = 176f;
        private const float LabelFontSize = 13f;
        private const float LabelLeftPadding = 12f;

        // macOS 라이트 외관 메뉴: 밝은 반투명 바탕 + 어두운 글자, 호버 시 액센트 블루 + 흰 글자.
        private static readonly Color PanelColor = new Color(0.96f, 0.96f, 0.97f, 0.98f);
        private static readonly Color PanelOutlineColor = new Color(0f, 0f, 0f, 0.12f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.22f);
        private static readonly Color HoverColor = new Color(0.14f, 0.47f, 0.94f, 1f); // macOS 액센트 블루
        private static readonly Color ItemNormalColor = new Color(1f, 1f, 1f, 0f);      // 평소 투명
        private static readonly Color LabelColor = new Color(0.12f, 0.12f, 0.13f, 1f);  // 거의 검정
        private static readonly Color LabelHoverColor = Color.white;
        private static readonly Color LabelDisabledColor = new Color(0f, 0f, 0f, 0.28f);

        private static UGUIDockContextMenu _instance;

        public static UGUIDockContextMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UGUIDockContextMenu>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("UGUIDockContextMenu", typeof(RectTransform));
                        _instance = go.AddComponent<UGUIDockContextMenu>();
                    }
                }

                return _instance;
            }
        }

        private RectTransform canvasRect;   // 배율 캔버스 아래에서 화면 전체를 덮는 중첩 캔버스(= this)
        private RectTransform panelRect;     // 메뉴 패널(항목들의 부모)
        private RectTransform shadowRect;    // 패널 뒤 소프트 그림자
        private Canvas hostRootCanvas;       // 현재 붙어 있는 배율 루트 캔버스

        private readonly List<GameObject> itemObjects = new();
        private bool built;
        private bool isOpen;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 주어진 아이콘 위에 컨텍스트 메뉴를 띄운다.
        /// </summary>
        public void Show(RectTransform anchor, IList<MenuItem> items)
        {
            if (anchor == null || items == null || items.Count == 0)
            {
                return;
            }

            var anchorCanvas = anchor.GetComponentInParent<Canvas>();
            Canvas rootCanvas = anchorCanvas != null ? anchorCanvas.rootCanvas : null;
            if (rootCanvas == null)
            {
                return;
            }

            EnsureBuilt(rootCanvas);
            RebuildItems(items);

            gameObject.SetActive(true);
            isOpen = true;

            // 항목 크기가 정해져야 화면 안으로 클램프하고 그림자를 맞출 수 있다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            PositionAbove(anchor, rootCanvas);
            SyncShadow();

            // 나중에 열린 창 등에 가려지지 않도록 최상단으로.
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            gameObject.SetActive(false);
        }

        // 아이콘의 위쪽 중앙에 메뉴 아래 끝을 맞추고, 화면 밖으로 넘치면 안으로 당긴다.
        private void PositionAbove(RectTransform anchor, Canvas rootCanvas)
        {
            Camera cam = ResolveCamera(rootCanvas);

            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            Vector3 worldTopCenter = Vector3.Lerp(corners[1], corners[2], 0.5f);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldTopCenter);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPoint, cam, out Vector2 local))
            {
                return;
            }

            // pivot (0.5, 0): 패널이 이 점에서 위로 자란다.
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = local + new Vector2(0f, 8f);

            ClampInsideCanvas();
        }

        private void ClampInsideCanvas()
        {
            Rect area = canvasRect.rect;
            Vector2 size = panelRect.rect.size;
            Vector2 pos = panelRect.anchoredPosition; // pivot=(0.5,0) 기준

            float halfW = size.x * 0.5f;
            float minX = area.xMin + halfW;
            float maxX = area.xMax - halfW;
            if (minX <= maxX)
            {
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
            }

            // 위로 넘치면 아래로 당긴다. 아래(도크 쪽)로도 방어적으로 클램프.
            float top = pos.y + size.y;
            if (top > area.yMax)
            {
                pos.y -= top - area.yMax;
            }
            if (pos.y < area.yMin)
            {
                pos.y = area.yMin;
            }

            panelRect.anchoredPosition = pos;
        }

        // 그림자를 패널 크기·위치에 맞춰 살짝 키우고 아래로 내린다.
        private void SyncShadow()
        {
            if (shadowRect == null)
            {
                return;
            }

            shadowRect.pivot = panelRect.pivot;
            shadowRect.sizeDelta = panelRect.rect.size + new Vector2(8f, 8f);
            shadowRect.anchoredPosition = panelRect.anchoredPosition + new Vector2(0f, -5f);
        }

        private void RebuildItems(IList<MenuItem> items)
        {
            foreach (var go in itemObjects)
            {
                Destroy(go);
            }
            itemObjects.Clear();

            foreach (var item in items)
            {
                itemObjects.Add(CreateItem(item));
            }
        }

        private GameObject CreateItem(MenuItem item)
        {
            var itemObject = new GameObject("MenuItem", typeof(RectTransform));
            itemObject.transform.SetParent(panelRect, false);

            var background = AddRoundedImage(itemObject, ItemCornerRadius);
            background.color = ItemNormalColor; // 평소 투명, 호버 시 하이라이트

            var layoutElement = itemObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = ItemMinWidth;
            layoutElement.minHeight = ItemHeight;
            layoutElement.preferredHeight = ItemHeight;
            layoutElement.flexibleWidth = 1f;

            var button = itemObject.AddComponent<Button>();
            button.targetGraphic = background;
            // 시각효과(배경+글자)는 아래 hover 컴포넌트가 함께 제어하므로 Button 자체 전환은 끈다.
            button.transition = Selectable.Transition.None;
            button.interactable = item.Enabled;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(itemObject.transform, false);

            var labelRect = labelObject.transform as RectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(LabelLeftPadding, 0f);
            labelRect.offsetMax = new Vector2(-LabelLeftPadding, 0f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = item.Label;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = LabelFontSize;
            label.color = item.Enabled ? LabelColor : LabelDisabledColor;
            label.raycastTarget = false;

            if (item.Enabled)
            {
                var hover = itemObject.AddComponent<UGUIDockContextMenuItem>();
                hover.Setup(background, label, ItemNormalColor, HoverColor, LabelColor, LabelHoverColor);

                if (item.Action != null)
                {
                    Action action = item.Action;
                    button.onClick.AddListener(() =>
                    {
                        Hide();
                        action();
                    });
                }
            }

            return itemObject;
        }

        // 오버레이 캔버스 + 배경 + 패널을 한 번만 짓고, 이후엔 rootCanvas가 바뀔 때만 재부착한다.
        private void EnsureBuilt(Canvas rootCanvas)
        {
            if (!built)
            {
                BuildOnce();
                built = true;
            }

            if (hostRootCanvas != rootCanvas)
            {
                transform.SetParent(rootCanvas.transform, false);
                StretchFull(canvasRect);
                hostRootCanvas = rootCanvas;
            }
        }

        private void BuildOnce()
        {
            canvasRect = transform as RectTransform;

            // 배율 루트 캔버스 아래 중첩되는 캔버스. overrideSorting으로 최상단에 그린다
            // (renderMode는 루트에서 상속되므로 지정하지 않는다).
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = MenuSortingOrder;
            gameObject.AddComponent<GraphicRaycaster>();

            // 화면 전체를 덮어 바깥 클릭을 받아 메뉴를 닫는 투명 배경.
            var backdropObject = new GameObject(
                "Backdrop",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            backdropObject.transform.SetParent(canvasRect, false);
            StretchFull(backdropObject.transform as RectTransform);

            var backdropImage = backdropObject.GetComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0f); // 완전 투명이지만 raycast는 받는다
            backdropImage.raycastTarget = true;

            var backdropButton = backdropObject.GetComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(Hide);

            // 패널 뒤 소프트 그림자(라운드 사각형, 넓은 falloff로 부드럽게).
            var shadowObject = new GameObject("Shadow", typeof(RectTransform));
            shadowObject.transform.SetParent(canvasRect, false);
            shadowRect = shadowObject.transform as RectTransform;
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            var shadowImage = AddRoundedImage(shadowObject, PanelCornerRadius + 2f);
            shadowImage.color = ShadowColor;
            shadowImage.FalloffDistance = 10f; // 가장자리를 흐리게 → 그림자처럼
            shadowImage.raycastTarget = false;

            // 메뉴 패널: 어두운 반투명 라운드 사각형 + 미세한 외곽선 + 세로 목록.
            var panelObject = new GameObject("Panel", typeof(RectTransform));
            panelObject.transform.SetParent(canvasRect, false);
            panelRect = panelObject.transform as RectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);

            var panelImage = AddRoundedImage(panelObject, PanelCornerRadius);
            panelImage.color = PanelColor;
            panelImage.OutlineWidth = 1f;
            panelImage.OutlineColor = PanelOutlineColor;

            var layout = panelObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            gameObject.SetActive(false);
        }

        // MPImage를 라운드 사각형으로 설정해 붙인다(창 라운딩과 같은 에셋).
        private static MPImage AddRoundedImage(GameObject target, float cornerRadius)
        {
            var image = target.AddComponent<MPImage>();
            image.DrawShape = DrawShape.Rectangle;

            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = Vector4.one * cornerRadius;
            image.Rectangle = rectangle;

            return image;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Camera ResolveCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }

    /// <summary>
    /// 컨텍스트 메뉴 한 항목의 호버 시각효과.
    /// macOS 라이트 메뉴처럼 호버 시 배경을 액센트 블루로, 글자를 흰색으로 <b>함께</b> 바꾼다.
    /// (Button의 ColorTint는 배경만 바꿔 어두운 글자가 블루 위에서 안 보이므로 직접 제어한다.)
    /// </summary>
    internal class UGUIDockContextMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Graphic background;
        private TMP_Text label;
        private Color normalBg;
        private Color hoverBg;
        private Color normalText;
        private Color hoverText;

        public void Setup(Graphic background, TMP_Text label,
            Color normalBg, Color hoverBg, Color normalText, Color hoverText)
        {
            this.background = background;
            this.label = label;
            this.normalBg = normalBg;
            this.hoverBg = hoverBg;
            this.normalText = normalText;
            this.hoverText = hoverText;
            ApplyNormal();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null)
            {
                background.color = hoverBg;
            }
            if (label != null)
            {
                label.color = hoverText;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ApplyNormal();
        }

        private void ApplyNormal()
        {
            if (background != null)
            {
                background.color = normalBg;
            }
            if (label != null)
            {
                label.color = normalText;
            }
        }
    }
}
