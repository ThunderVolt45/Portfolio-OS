using MPUIKIT;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// macOS 스타일 창 모서리 라운딩을 창 상태에 맞춰 동적으로 적용한다.
    /// - 헤더가 있으면 본체 상단은 각지게 두고(헤더가 상단 라운딩을 담당), 헤더가 없으면 본체가 상단도 라운딩한다.
    /// - 최대화(화면을 꽉 채운 상태)에서는 본체·헤더 라운딩을 모두 0으로 한다. 최대화 판정은 WindowMode와
    ///   실제 레이아웃(full-stretch 앵커) 중 하나라도 참이면 된다(IsMaximized 참고).
    ///
    /// MPUIKit(유료 에셋) 의존이므로 프레임워크 base(UGUIWindowView 등, upstream 동기화 대상)가 아닌
    /// 포트폴리오 레이어에 둔다. 최대화/복원 전용 이벤트가 프레임워크에 없어 상태를 폴링한다
    /// (변경 감지로 대부분의 프레임은 no-op).
    /// </summary>
    [RequireComponent(typeof(MPImage))]
    [RequireComponent(typeof(UGUIWindow))]
    public class UGUIWindowRounding : MonoBehaviour
    {
        [Tooltip("모서리 반경(px). macOS Big Sur+ 기준 약 10.")]
        [SerializeField] private float cornerRadius = 10f;

        public float CornerRadius => cornerRadius;

        private MPImage windowImage;
        private RectTransform rectTransform;
        // 포트폴리오 창 프리팹(Variant)은 루트에 base UGUIWindow와 서브클래스(AboutWindow 등)가
        // 함께 존재할 수 있다. 어느 컴포넌트가 최대화되든 감지하도록 전부 캐싱한다.
        private UGUIWindow[] windows;
        private MPImage headerImage;
        private GameObject headerObject;

        // 마지막으로 적용한 상태 캐시 — 매 프레임 머티리얼 갱신을 방지한다.
        private bool hasApplied;
        private float lastRadius;
        private bool lastMaximized;
        private bool lastHeaderActive;

        private void Awake()
        {
            windowImage = GetComponent<MPImage>();
            rectTransform = transform as RectTransform;
            windows = GetComponents<UGUIWindow>();

            // 헤더는 비활성 상태로도 존재할 수 있으므로 include-inactive로 탐색한다.
            UGUIWindowHeader header = GetComponentInChildren<UGUIWindowHeader>(true);
            if (header != null)
            {
                headerObject = header.gameObject;
                headerImage = header.GetComponent<MPImage>();
            }
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            bool maximized = IsMaximized();
            bool headerActive = headerObject != null && headerObject.activeInHierarchy;

            if (hasApplied
                && Mathf.Approximately(lastRadius, cornerRadius)
                && lastMaximized == maximized
                && lastHeaderActive == headerActive)
            {
                return;
            }

            float r = maximized ? 0f : cornerRadius;

            // Vector4 코너 순서: (x=하단좌, y=하단우, z=상단우, w=상단좌).
            // 헤더가 있으면 본체 상단(z,w)=0 → 헤더가 상단 라운딩을 담당한다.
            float bodyTop = (maximized || headerActive) ? 0f : r;
            ApplyCornerRadius(windowImage, new Vector4(r, r, bodyTop, bodyTop));

            if (headerImage != null)
            {
                float headerTop = maximized ? 0f : cornerRadius;
                ApplyCornerRadius(headerImage, new Vector4(0f, 0f, headerTop, headerTop));
            }

            hasApplied = true;
            lastRadius = cornerRadius;
            lastMaximized = maximized;
            lastHeaderActive = headerActive;
        }

        // 라운딩은 "창이 화면을 꽉 채우는가"에 따라 꺼져야 한다. WindowMode enum만으로는 부족한데,
        // (1) 루트에 UGUIWindow 컨트롤러가 둘이면 mode가 서로 어긋날 수 있고,
        // (2) 최대화 창을 최소화 후 재활성화하면 RestoreFromMinimized가 mode를 Windowed로 되돌리지만
        //     레이아웃은 전체 화면(full-stretch)으로 남아 mode와 실제 모습이 불일치한다.
        // 그래서 mode(어느 컨트롤러든 Maximized)와 실제 레이아웃(full-stretch 앵커) 중 하나라도
        // 참이면 최대화로 간주한다. 레이아웃은 루트가 공유하는 단일 RectTransform이라 신뢰할 수 있다.
        private bool IsMaximized()
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

            return IsFullStretchLayout();
        }

        // 앵커가 부모 전체를 덮으면(min≈0, max≈1) 화면을 꽉 채운 상태로 본다.
        // 포트폴리오 창은 창 모드에서 중앙 고정 앵커(0.5, 0.5)를 쓰므로 오탐 위험이 없다.
        private bool IsFullStretchLayout()
        {
            if (rectTransform == null)
            {
                return false;
            }

            Vector2 min = rectTransform.anchorMin;
            Vector2 max = rectTransform.anchorMax;
            const float eps = 0.001f;
            return min.x <= eps && min.y <= eps && max.x >= 1f - eps && max.y >= 1f - eps;
        }

        // 구조체(값 타입)라 get→수정→set로 되돌려야 Init된 내부 참조(RectTransform 등)가 보존된다.
        private static void ApplyCornerRadius(MPImage image, Vector4 radius)
        {
            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = radius;
            image.Rectangle = rectangle;
        }
    }
}
