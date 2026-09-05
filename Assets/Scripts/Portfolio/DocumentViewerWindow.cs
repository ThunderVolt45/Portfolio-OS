using UnityEngine;
using UnityEngine.UI;

namespace UGUIWindow
{
    /// <summary>
    /// 브라우저의 기본 PDF 뷰어로 문서를 여는 창의 공통 동작.
    ///
    /// PDF를 Unity 캔버스 안에 임베드하지 않는다. 프리팹에 구워진 소개 콘텐츠를 보여주고,
    /// 사용자가 DetailsButton을 눌렀을 때만 StreamingAssets의 PDF를 새 브라우저 탭으로 연다.
    /// Application.OpenURL 호출이 버튼의 직접 클릭 이벤트 안에서 실행되므로 WebGL 팝업 차단 정책과도 맞는다.
    /// </summary>
    public abstract class ExternalPdfWindow : UGUIWindow
    {
        protected abstract string DocumentPath { get; }

        protected virtual Vector2 PreferredWindowSize
        {
            get { return new Vector2(500f, 620f); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Vector2 size = PreferredWindowSize;
            Resize((int)size.x, (int)size.y);
            WireDetailsButton();
        }

        private void WireDetailsButton()
        {
            Transform target = transform.Find("Content/Viewport/ScrollContent/DetailsButton");
            if (target == null)
            {
                UGUIWindowLog.LogWarning("[ExternalPdfWindow] DetailsButton을 찾을 수 없습니다: " + GetType().Name, this);
                return;
            }

            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                UGUIWindowLog.LogWarning("[ExternalPdfWindow] DetailsButton에 Button 컴포넌트가 없습니다: " + GetType().Name, this);
                return;
            }

            button.onClick.RemoveListener(OpenDocument);
            button.onClick.AddListener(OpenDocument);
        }

        public void OpenDocument()
        {
            string baseUrl = Application.streamingAssetsPath.TrimEnd('/', '\\');
            string relativePath = DocumentPath.TrimStart('/', '\\').Replace('\\', '/');
            Application.OpenURL(baseUrl + "/" + relativePath);
        }
    }

    /// <summary>
    /// 프로젝트 소개 창에서 공유하는 기본 크기와 외부 PDF 열기 동작.
    /// 기존 *DocWindow 타입명은 데스크톱 아이콘과 공개 딥링크 호환을 위해 유지한다.
    /// </summary>
    public abstract class ProjectIntroductionWindow : ExternalPdfWindow
    {
    }

    /// <summary>
    /// 이력서 안내 창. 역사적인 타입명은 기존 Icon_Resume과 #open 링크 호환을 위해 유지한다.
    /// PDF 본문은 임베드하지 않고 브라우저 기본 PDF 뷰어로 연다.
    /// </summary>
    public class DocumentViewerWindow : ExternalPdfWindow
    {
        protected override string DocumentPath
        {
            get { return "docs/resume.pdf"; }
        }

        protected override Vector2 PreferredWindowSize
        {
            get { return new Vector2(460f, 480f); }
        }
    }
}
