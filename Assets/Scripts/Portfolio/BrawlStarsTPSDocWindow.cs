namespace UGUIWindow
{
    /// <summary>
    /// BrawlStarsTPS 기술 문서 전용 PDF 뷰어 창.
    /// DocumentViewerWindow를 상속해 문서 경로/제목만 지정한다.
    /// (매니저는 타입명으로 프리팹을 로드하므로 PDF별 구분에는 별도 타입이 필요하다.)
    /// </summary>
    public class BrawlStarsTPSDocWindow : DocumentViewerWindow
    {
        protected override string DocumentPath { get { return "docs/brawlstarstps.pdf"; } }
        protected override string DocumentTitle { get { return "BrawlStarsTPS.pdf"; } }
    }
}
