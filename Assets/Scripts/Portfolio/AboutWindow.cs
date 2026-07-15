using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// About 앱 창. 자기소개(이력서 1페이지)를 표시한다.
    ///
    /// 콘텐츠는 더 이상 단일 TMP 텍스트 블록으로 주입하지 않는다.
    /// 프리팹의 Content/Viewport/ScrollContent 하위에 구조화된 UGUI 레이아웃으로
    /// 미리 구워져 있으며(에디터툴 Portfolio → Build About Content),
    /// 이 클래스는 창 크기·제목만 확정한다.
    /// 디자인 기반(HTML/CSS 시안)은 scratchpad/about-mockup.html.
    /// </summary>
    public class AboutWindow : UGUIWindow
    {
        // 크기는 Instantiate 중 반드시 호출되는 OnEnable에서 확정한다.
        // (Start는 인스턴스화 컨텍스트에 따라 호출이 누락될 수 있어 신뢰하지 않는다.)
        protected override void OnEnable()
        {
            base.OnEnable();

            // 이력서 1페이지 전체를 담되 가로 스크롤이 없도록 넉넉한 폭, 세로는 스크롤 허용.
            Resize(480, 580);
            Move(-40, 0);
        }

        // 매니저가 생성 시 제목을 클래스명으로 지정하므로, 이후 프레임(Start)에서
        // 사람이 읽기 좋은 제목으로 교체한다. (실패해도 콘텐츠 표시에는 영향 없음)
        private void Start()
        {
            SetWindowTitle("About");
        }
    }
}
