namespace UGUIWindow
{
    /// <summary>
    /// BrawlStarsTPS 소개 창. 자세한 기술 문서는 브라우저 기본 PDF 뷰어로 연다.
    /// </summary>
    public class BrawlStarsTPSDocWindow : ProjectIntroductionWindow
    {
        protected override string DocumentPath
        {
            get { return "docs/brawlstarstps.pdf"; }
        }
    }
}
