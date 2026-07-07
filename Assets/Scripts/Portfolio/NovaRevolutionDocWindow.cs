namespace UGUIWindow
{
    /// <summary>
    /// NovaRevolution 기술 문서 전용 PDF 뷰어 창.
    /// DocumentViewerWindow를 상속해 문서 경로/제목만 지정한다.
    /// </summary>
    public class NovaRevolutionDocWindow : DocumentViewerWindow
    {
        protected override string DocumentPath { get { return "docs/novarevolution.pdf"; } }
        protected override string DocumentTitle { get { return "NovaRevolution.pdf"; } }
    }
}
