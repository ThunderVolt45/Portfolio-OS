using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// macOS Dock 스타일 아이콘 확대(magnification).
    /// 커서의 X 거리에 따라 각 <see cref="UGUITaskIcon"/>의 크기를 키워 HorizontalLayoutGroup이
    /// 자연스럽게 밀려나고(push-apart) 위로 자라도록 한다.
    ///
    /// 포인터 좌표는 raw <c>Pointer.current.position</c>를 폴링하지 않고, <b>EventSystem이 전달하는
    /// <c>eventData.position</c></b>만 사용한다. 이는 uGUI 레이캐스트(=클릭 판정)가 쓰는 좌표계와
    /// 완전히 동일하므로, High DPI·Canvas 스케일·게임뷰 스케일·WebGL 레티나 등에서 raw 포인터가 겪는
    /// 좌표 불일치(엉뚱한 아이콘이 확대되는 문제)가 원천적으로 없다. "확대되는 아이콘 = 커서 아래 아이콘".
    ///
    /// 전제(프리팹 구성):
    /// - 이 컴포넌트는 아이콘들의 부모(도크 루트: MPImage raycastTarget + HorizontalLayoutGroup + ContentSizeFitter)에 붙는다.
    /// - LayoutGroup은 childControlWidth = true, childControlHeight = false, childAlignment = LowerCenter.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UGUITaskDockMagnifier : MonoBehaviour,
        IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        [Header("Magnification")]
        [Tooltip("확대되지 않은 기본 아이콘 크기(px).")]
        [SerializeField] private float restingSize = 44f;
        [Tooltip("커서 바로 위 아이콘의 최대 배율.")]
        [SerializeField] private float maxScale = 1.5f;
        [Tooltip("커서 영향 반경(px). 클수록 넓은 범위가 완만하게 확대된다.")]
        [SerializeField] private float influence = 95f;
        [Tooltip("보간 속도(클수록 즉각적). 프레임률 독립.")]
        [SerializeField] private float responsiveness = 16f;

        private RectTransform rectTransform;
        private Canvas canvas;
        private Camera uiCamera;

        // EventSystem이 채워주는 포인터 좌표(레이캐스트와 동일 좌표계)만 사용한다.
        private bool pointerInside;
        private Vector2 pointerScreen;

        private readonly List<UGUITaskIcon> icons = new();
        private readonly List<LayoutElement> layouts = new();
        private readonly List<float> currentScales = new();
        private int lastChildCount = -1;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            RefreshChildren();
        }

        private void OnDisable()
        {
            pointerInside = false;
            ResetScales();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            pointerScreen = eventData.position;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            pointerInside = true;
            pointerScreen = eventData.position;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
        }

        private void Update()
        {
            if (transform.childCount != lastChildCount)
            {
                RefreshChildren();
            }

            if (icons.Count == 0)
            {
                return;
            }

            ResolveCamera();

            Vector2 local = Vector2.zero;
            bool inside = pointerInside && TryGetPointerLocal(out local);

            float dt = Time.unscaledDeltaTime;
            float t = 1f - Mathf.Exp(-responsiveness * dt); // 프레임률 독립 보간 계수

            for (int i = 0; i < icons.Count; i++)
            {
                UGUITaskIcon icon = icons[i];
                if (icon == null)
                {
                    continue;
                }

                float target = 1f;
                bool hovered = false;

                if (inside)
                {
                    // 아이콘 X 중심을 "컨테이너 로컬 좌표"로 계산한다. anchoredPosition은 쓰지 않는데,
                    // HorizontalLayoutGroup이 childControlWidth로 아이콘을 제어하면 앵커를 좌상단으로
                    // 바꿔 anchoredPosition이 왼쪽 가장자리 기준이 되기 때문이다. 그러면 pivot 중앙 기준인
                    // local.x와 프레임이 어긋나(약 절반폭 오프셋) 엉뚱한 아이콘이 확대된다.
                    // 월드→컨테이너 로컬 변환은 앵커/DPI/스케일에 무관하게 local.x와 같은 프레임을 보장한다.
                    float iconX = rectTransform.InverseTransformPoint(icon.RectTransform.position).x;
                    float dx = Mathf.Abs(local.x - iconX);
                    float f = Mathf.Clamp01(1f - dx / influence);
                    f = f * f * (3f - 2f * f); // smoothstep
                    target = Mathf.Lerp(1f, maxScale, f);
                    hovered = dx <= restingSize * 0.5f;
                }

                float s = Mathf.Lerp(currentScales[i], target, t);
                if (Mathf.Abs(s - 1f) < 0.001f)
                {
                    s = 1f;
                }
                currentScales[i] = s;

                ApplySize(icon, layouts[i], restingSize * s);
                icon.SetHovered(hovered);
            }
        }

        private bool TryGetPointerLocal(out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, pointerScreen, uiCamera, out local);
        }

        // 가로(주축): LayoutElement.preferredWidth → HorizontalLayoutGroup이 폭을 제어하며 이웃을 밀어낸다.
        // 세로(교차축): childControlHeight=false이므로 높이는 직접 준다(컨테이너 높이로 클램프되지 않도록).
        // LowerCenter 정렬이라 바닥은 고정되고 위로만 커진다 → macOS Dock 확대 효과.
        private void ApplySize(UGUITaskIcon icon, LayoutElement le, float size)
        {
            if (le != null)
            {
                le.preferredWidth = size;
                le.preferredHeight = size;
            }

            RectTransform irt = icon.RectTransform;
            Vector2 sd = irt.sizeDelta;
            if (!Mathf.Approximately(sd.y, size))
            {
                sd.y = size;
                irt.sizeDelta = sd;
            }
        }

        private void ResetScales()
        {
            for (int i = 0; i < icons.Count; i++)
            {
                if (icons[i] == null)
                {
                    continue;
                }

                currentScales[i] = 1f;
                ApplySize(icons[i], layouts[i], restingSize);
                icons[i].SetHovered(false);
            }
        }

        private void ResolveCamera()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            // nested canvas는 렌더 모드를 루트에서 물려받으므로 rootCanvas 기준으로 판정한다.
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            uiCamera = (root != null && root.renderMode != RenderMode.ScreenSpaceOverlay)
                ? root.worldCamera
                : null;
        }

        private void RefreshChildren()
        {
            icons.Clear();
            layouts.Clear();
            currentScales.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                UGUITaskIcon icon = child.GetComponent<UGUITaskIcon>();
                if (icon == null)
                {
                    continue;
                }

                icons.Add(icon);
                layouts.Add(child.GetComponent<LayoutElement>());
                currentScales.Add(1f);
            }

            lastChildCount = transform.childCount;
        }
    }
}
