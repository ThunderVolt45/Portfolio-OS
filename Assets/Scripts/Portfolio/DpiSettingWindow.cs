using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// UI 배율(DPI)만 다루는 간단한 설정 앱 창.
    ///
    /// 프레임워크의 <see cref="UGUIApplicationSetting"/>은 해상도·프레임레이트·윈도우 모드까지 다루지만,
    /// 전체화면 WebGL 캔버스에서는 그 셋이 의미가 없다(브라우저가 소유). 그래서 배율만 남긴 창을 따로 둔다.
    ///
    /// 적용은 <see cref="UGUIWindowManager.SetDPI"/>에 위임한다(= 각 CanvasScaler의
    /// referenceResolution 을 screen/dpi 로 설정 + PlayerPrefs 기록). 기본값은
    /// <see cref="PortfolioOS.PortfolioUIScale"/>가 씬 로드 전에 주입한다.
    ///
    /// 프리팹(배율 버튼·라벨 배선)은 에디터툴 Portfolio → Build DPI Setting Window 가 굽는다.
    /// </summary>
    public class DpiSettingWindow : UGUIWindow
    {
        [Header("DPI Setting")]
        [Tooltip("선택 가능한 UI 배율 (1f = 100%)")]
        [SerializeField] private float[] supportDPI = { 1f, 1.25f, 1.5f, 1.75f, 2f };

        [Tooltip("supportDPI와 같은 순서로 대응되는 배율 버튼")]
        [SerializeField] private Button[] presetButtons;

        [Tooltip("현재 적용된 배율을 표시하는 라벨")]
        [SerializeField] private TMP_Text currentLabel;

        // 베이커(PortfolioDpiSettingContent)와 공유하는 팔레트.
        static readonly Color Accent = new Color(10f / 255f, 132f / 255f, 1f, 1f);
        static readonly Color Panel = new Color(245f / 255f, 245f / 255f, 247f / 255f, 1f);
        static readonly Color Ink = new Color(29f / 255f, 29f / 255f, 31f / 255f, 1f);

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < presetButtons.Length; i++)
            {
                int index = i; // 클로저가 루프 변수를 캡처하지 않도록 복사
                if (presetButtons[i] != null)
                {
                    presetButtons[i].onClick.AddListener(() => Apply(index));
                }
            }
        }

        // 크기는 Instantiate 중 반드시 호출되는 OnEnable에서 확정한다(UGUISampleWindow와 동일 패턴).
        protected override void OnEnable()
        {
            base.OnEnable();

            // 콘텐츠(약 194px) + 헤더가 스크롤 없이 들어가는 높이.
            Resize(380, 250);
            Refresh();
        }

        // 매니저가 생성 시 제목을 클래스명으로 지정하므로 이후 프레임에서 교체한다.
        private void Start()
        {
            SetWindowTitle("화면 배율 설정");
        }

        private void Apply(int index)
        {
            if (index < 0 || index >= supportDPI.Length) return;

            // 매니저가 부팅 시(InitializeCanvas) 쓰는 것과 같은 기준 해상도를 사용해,
            // 배율만 바뀌고 referenceResolution의 기준이 흔들리지 않게 한다.
            var resolution = Screen.currentResolution;
            UGUIWindowManager.SetDPI(resolution.width, resolution.height, supportDPI[index]);

            Refresh();
        }

        /// <summary>현재 배율을 라벨과 버튼 강조 상태에 반영한다.</summary>
        private void Refresh()
        {
            float current = UGUIWindowManager.CurrentDPI;

            if (currentLabel != null)
            {
                currentLabel.text = $"현재 배율 {Mathf.RoundToInt(current * 100f)}%";
            }

            for (int i = 0; i < presetButtons.Length; i++)
            {
                if (presetButtons[i] == null) continue;

                bool selected = Mathf.Approximately(supportDPI[i], current);

                if (presetButtons[i].targetGraphic != null)
                {
                    presetButtons[i].targetGraphic.color = selected ? Accent : Panel;
                }

                var label = presetButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = selected ? Color.white : Ink;
                }
            }
        }
    }
}
