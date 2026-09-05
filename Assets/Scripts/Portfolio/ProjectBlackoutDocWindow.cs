namespace UGUIWindow
{
    /// <summary>
    /// ProjectBlackout 소개 창. 자세한 기술 문서는 브라우저 기본 PDF 뷰어로 연다.
    /// </summary>
    public class ProjectBlackoutDocWindow : ProjectIntroductionWindow
    {
        protected override string DocumentPath
        {
            get { return "docs/projectblackout.pdf"; }
        }
    }
}
