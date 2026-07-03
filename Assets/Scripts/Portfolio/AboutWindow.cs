using TMPro;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// About 앱 창 (포트폴리오 개념 증명).
    /// 자기소개 요약을 표시한다. 콘텐츠 SSOT는 portfolio/index.md.
    /// </summary>
    public class AboutWindow : UGUIWindow
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
            Move(-80, 20);

            if (contentText == null)
            {
                return;
            }

            contentText.alignment = TextAlignmentOptions.TopLeft;
            contentText.fontSize = 16;
            contentText.margin = new Vector4(12, 10, 12, 10);
            contentText.text =
                "<b>김민영 (Kim Min-young)</b>\n" +
                "유니티 · 언리얼 게임 클라이언트 프로그래머 — 멀티플레이 통합 지향\n\n" +
                "유니티와 언리얼, C#과 C++, 클라이언트와 서버를 경계 없이 넘나들며, " +
                "기획된 기능을 실제 출시·배포까지 책임지고 완수하는 게임 개발자입니다.\n\n" +
                "<b>핵심 역량</b>\n" +
                "· 게임 엔진: 언리얼(클라이언트/Dedicated Server/GAS) · 유니티(클라이언트)\n" +
                "· 네트워크: Colyseus, Photon · 언리얼 Dedicated Server / Replication\n" +
                "· 언어: C++, C#\n\n" +
                "<b>Contact</b>\n" +
                "· zxc9876zxc@gmail.com\n" +
                "· github.com/ThunderVolt45";
        }

        // 매니저가 생성 시 제목을 클래스명으로 지정하므로, 이후 프레임(Start)에서
        // 사람이 읽기 좋은 제목으로 교체한다. (실패해도 콘텐츠 표시에는 영향 없음)
        private void Start()
        {
            SetWindowTitle("About");
        }
    }
}
