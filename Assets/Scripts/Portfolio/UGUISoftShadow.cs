using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// 창의 둥근 외곽선을 따라 한 번의 UI 패스로 부드러운 그림자를 그린다.
    /// 부모 창의 CanvasGroup, transform, z-order를 그대로 따르며 최대화 시에는 숨는다.
    /// 같은 컴포넌트를 카드 같은 다른 UGUI 요소에도 재사용할 수 있다.
    /// </summary>
    [AddComponentMenu("UI/Effects/UGUI Soft Shadow")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UGUISoftShadow : MaskableGraphic
    {
        [Header("Elevation")]
        [SerializeField] private Color focusedColor = new Color32(15, 23, 42, 80);
        [SerializeField] private Color unfocusedColor = new Color32(15, 23, 42, 48);
        [SerializeField] private Vector2 offset = new Vector2(0f, -7f);
        [SerializeField, Min(0.5f)] private float blurRadius = 18f;
        [SerializeField, Min(0f)] private float spread = 1f;
        [SerializeField, Min(0f)] private float fallbackCornerRadius = 10f;

        [Header("Window Behavior")]
        [SerializeField] private bool inheritWindowCornerRadius = true;
        [SerializeField] private bool followWindowFocus = true;
        [SerializeField] private bool hideWhenMaximized = true;

        private static Material sharedShadowMaterial;

        private RectTransform windowRect;
        private UGUIWindow[] windows;
        private UGUIWindowRounding rounding;
        private bool isFocused = true;
        private bool isMaximized;
        private Vector2 lastSize;
        private float lastCornerRadius = -1f;

        public override Material defaultMaterial
        {
            get
            {
                if (sharedShadowMaterial == null)
                {
                    Shader shader = Resources.Load<Shader>("PortfolioSoftShadow");
                    if (shader != null)
                    {
                        sharedShadowMaterial = new Material(shader)
                        {
                            name = "Portfolio Soft Shadow (Runtime)",
                            hideFlags = HideFlags.HideAndDontSave
                        };
                    }
                }

                return sharedShadowMaterial != null ? sharedShadowMaterial : base.defaultMaterial;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            CacheWindowComponents();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CacheWindowComponents();
            RefreshState(true);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            RefreshState(false);
        }

        private void CacheWindowComponents()
        {
            windows = GetComponentsInParent<UGUIWindow>(true);
            rounding = GetComponentInParent<UGUIWindowRounding>(true);

            if (windows != null && windows.Length > 0)
            {
                windowRect = windows[0].transform as RectTransform;
            }
            else
            {
                windowRect = transform.parent as RectTransform;
            }
        }

        private void RefreshState(bool force)
        {
            bool nextMaximized = hideWhenMaximized && IsWindowMaximized();
            bool nextFocused = !followWindowFocus || IsWindowFocused();
            Vector2 nextSize = rectTransform.rect.size;
            float nextCornerRadius = GetCornerRadius();

            if (force || nextMaximized != isMaximized)
            {
                isMaximized = nextMaximized;
                canvasRenderer.SetAlpha(isMaximized ? 0f : 1f);
            }

            if (force
                || nextFocused != isFocused
                || nextSize != lastSize
                || !Mathf.Approximately(nextCornerRadius, lastCornerRadius))
            {
                isFocused = nextFocused;
                lastSize = nextSize;
                lastCornerRadius = nextCornerRadius;
                SetVerticesDirty();
            }
        }

        private bool IsWindowFocused()
        {
            if (windows == null || windows.Length == 0)
            {
                return true;
            }

            UGUIWindowManager manager = UGUIWindowManager.Instance;
            if (manager == null)
            {
                return true;
            }

            UGUIWindow focusedWindow = manager.GetFocusedWindow();
            if (focusedWindow == null)
            {
                return true;
            }

            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i] == focusedWindow)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWindowMaximized()
        {
            if (windows != null)
            {
                for (int i = 0; i < windows.Length; i++)
                {
                    if (windows[i] != null && windows[i].WindowMode == UGUIWindowMode.Maximized)
                    {
                        return true;
                    }
                }
            }

            if (windowRect == null)
            {
                return false;
            }

            Vector2 min = windowRect.anchorMin;
            Vector2 max = windowRect.anchorMax;
            const float epsilon = 0.001f;
            return min.x <= epsilon && min.y <= epsilon
                && max.x >= 1f - epsilon && max.y >= 1f - epsilon;
        }

        private float GetCornerRadius()
        {
            return inheritWindowCornerRadius && rounding != null
                ? rounding.CornerRadius
                : fallbackCornerRadius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float padding = blurRadius + spread + Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) + 2f;
            Vector2 halfSize = rect.size * 0.5f;
            Vector2 center = rect.center;
            Color32 vertexColor = isFocused ? focusedColor : unfocusedColor;
            float cornerRadius = GetCornerRadius();

            AddVertex(vh, new Vector2(rect.xMin - padding, rect.yMin - padding), center,
                halfSize, cornerRadius, vertexColor);
            AddVertex(vh, new Vector2(rect.xMin - padding, rect.yMax + padding), center,
                halfSize, cornerRadius, vertexColor);
            AddVertex(vh, new Vector2(rect.xMax + padding, rect.yMax + padding), center,
                halfSize, cornerRadius, vertexColor);
            AddVertex(vh, new Vector2(rect.xMax + padding, rect.yMin - padding), center,
                halfSize, cornerRadius, vertexColor);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void AddVertex(VertexHelper vh, Vector2 position, Vector2 center, Vector2 halfSize,
            float cornerRadius, Color32 vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vertex.uv1 = position - center;
            vertex.uv2 = halfSize;
            vertex.normal = new Vector3(offset.x, offset.y, cornerRadius);
            vertex.tangent = new Vector4(blurRadius, spread, 0f, 1f);
            vh.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            blurRadius = Mathf.Max(0.5f, blurRadius);
            spread = Mathf.Max(0f, spread);
            fallbackCornerRadius = Mathf.Max(0f, fallbackCornerRadius);
            SetVerticesDirty();
        }
#endif
    }
}
