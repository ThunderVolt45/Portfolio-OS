using TMPro;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// ProjectBlackout 앱 창 (포트폴리오 개념 증명).
    /// 대표 프로젝트 요약을 표시한다. 콘텐츠 SSOT는 portfolio/index.md.
    /// </summary>
    public class ProjectBlackoutWindow : UGUIWindow
    {
        [Header("Portfolio Content")]
        [Tooltip("본문을 표시할 Content 영역의 TMP_Text")]
        [SerializeField] private TMP_Text contentText;

        // 크기·본문은 Instantiate 중 반드시 호출되는 OnEnable에서 확정한다.
        // (Start는 인스턴스화 컨텍스트에 따라 호출이 누락될 수 있어 신뢰하지 않는다.)
        protected override void OnEnable()
        {
            base.OnEnable();

            Resize(400, 340);
            Move(80, -20);

            if (contentText == null)
            {
                return;
            }

            contentText.alignment = TextAlignmentOptions.TopLeft;
            contentText.fontSize = 16;
            contentText.margin = new Vector4(12, 10, 12, 10);
            contentText.text =
                "<b>ProjectBlackout</b>  <i>(대표 프로젝트)</i>\n" +
                "3인칭 슈팅(TPS) 소울라이크 PvE — 4인 협동 보스 레이드 · 팀장\n\n" +
                "언리얼 부트캠프 기업 협약(하이퍼센트) 프로젝트. Dedicated Server 기반 " +
                "4인 멀티플레이를 GAS로 구현했으며, 팀 내 최다 기여자로서 플레이어 측 " +
                "전투 전 영역(GAS·Combat·UI)을 담당했습니다.\n\n" +
                "<b>기술 스택</b>\n" +
                "Unreal Engine 5.7.4 · C++ · GAS · Dedicated Server · Replication\n\n" +
                "<b>GitHub</b>\n" +
                "· github.com/ThunderVolt45/ProjectBlackout";
        }

        // 매니저가 생성 시 제목을 클래스명으로 지정하므로, 이후 프레임(Start)에서
        // 사람이 읽기 좋은 제목으로 교체한다. (실패해도 콘텐츠 표시에는 영향 없음)
        private void Start()
        {
            SetWindowTitle("ProjectBlackout");
        }
    }
}
