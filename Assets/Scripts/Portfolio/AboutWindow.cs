using UnityEngine;
using UnityEngine.UI;

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
            // Resize(480, 580);
            // Move(-40, 0);

            // 버튼 배선은 반드시 호출되는 OnEnable에서 한다.
            // (Start는 인스턴스화 컨텍스트에 따라 누락될 수 있어 신뢰하지 않음 — 위 주석 참조.
            //  프리팹 자식은 Instantiate 시점에 이미 존재하므로 OnEnable에서 Find 가능.)
            WireResumeButton();
        }

        // 매니저가 생성 시 제목을 클래스명으로 지정하므로, 이후 프레임(Start)에서
        // 사람이 읽기 좋은 제목으로 교체한다. (실패해도 콘텐츠 표시에는 영향 없음)
        private void Start()
        {
            SetWindowTitle("About");
        }

        // 콘텐츠에 구워진 "이력서 전체 보기" 버튼(ResumeCta)을 이름으로 찾아
        // 클릭 시 이력서 PDF 뷰어(DocumentViewerWindow)를 열도록 배선한다.
        // (베이커는 버튼만 생성하고 배선은 하지 않는다 — 배선은 여기 코드로.)
        private void WireResumeButton()
        {
            var t = transform.Find("Content/Viewport/ScrollContent/ResumeCta");
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveListener(OpenResume);
            btn.onClick.AddListener(OpenResume);
        }

        // 이력서 전체 PDF 창을 연다. (기존 Icon_Resume 아이콘과 동일한 대상)
        public void OpenResume()
        {
            UGUIWindowManager.CreateWindow<DocumentViewerWindow>();
        }
    }
}
