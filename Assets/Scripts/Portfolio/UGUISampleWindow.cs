using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// UGUI-Window-Sample 소개 앱 창.
    /// 이 포트폴리오 OS가 올라가 있는 창 프레임워크(UGUI-Window-Sample) 자체를 소개한다.
    /// (PDF 미사용 — 순수 UGUI 레이아웃. "이 창이 곧 그 프레임워크"라는 dogfooding 훅 포함)
    ///
    /// 콘텐츠는 프리팹의 Content/Viewport/ScrollContent 하위에 구조화된 UGUI 레이아웃으로
    /// 미리 구워져 있으며(에디터툴 Portfolio → Build UGUISample Content),
    /// 이 클래스는 창 크기·제목만 확정한다.
    /// 디자인 기반(HTML/CSS 시안)은 scratchpad/ugui-sample-mockup.html.
    /// 콘텐츠 SSOT: portfolio/projects/ugui-window-sample.md.
    /// </summary>
    public class UGUISampleWindow : UGUIWindow
    {
        // 크기는 Instantiate 중 반드시 호출되는 OnEnable에서 확정한다.
        // (Start는 인스턴스화 컨텍스트에 따라 호출이 누락될 수 있어 신뢰하지 않는다.)
        protected override void OnEnable()
        {
            base.OnEnable();

            // 콘텐츠 폭(시안 500px)에 맞춰 가로 스크롤 없이, 세로만 스크롤 허용.
            Resize(500, 620);
        }
    }
}
