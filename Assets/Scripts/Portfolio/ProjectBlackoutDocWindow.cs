namespace UGUIWindow
{
    /// <summary>
    /// ProjectBlackout 기술 문서 전용 PDF 뷰어 창.
    /// DocumentViewerWindow를 상속해 문서 경로/제목만 지정한다.
    /// </summary>
    public class ProjectBlackoutDocWindow : DocumentViewerWindow
    {
        protected override string DocumentPath { get { return "docs/projectblackout.pdf"; } }
        protected override string DocumentTitle { get { return "ProjectBlackout.pdf"; } }
    }
}
