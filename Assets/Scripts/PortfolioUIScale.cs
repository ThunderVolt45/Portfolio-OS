using UnityEngine;

namespace PortfolioOS
{
    /// <summary>
    /// 포트폴리오 기본 UI 배율.
    ///
    /// 프레임워크(UGUIWindow.UGUIWindowManager)는 시작 시
    /// <c>PlayerPrefs.GetFloat("DPI Settings", 1f)</c>를 읽어 각 CanvasScaler의
    /// referenceResolution 을 <c>screen / dpi</c> 로 설정한다(= dpi 가 곧 UI 배율).
    /// 값이 낮을수록 referenceResolution 이 커져 UI 가 작아진다.
    ///
    /// 전체화면(뷰포트) WebGL 캔버스에서는 기본 100%(dpi=1)가 과대하므로,
    /// 씬 로드 전에 더 작은 기본 배율을 주입한다. 앱 내 설정 창에서 바꾸면
    /// 해당 세션 동안은 그 값이 우선한다(프레임워크 설정은 재로드 시 유지되지 않음).
    /// </summary>
    public static class PortfolioUIScale
    {
        /// <summary>기본 UI 배율(1 = 100%). 낮출수록 UI 가 작아진다.</summary>
        public const float DefaultScale = 1.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyDefaultScale()
        {
            PlayerPrefs.SetFloat("DPI Settings", DefaultScale);
        }
    }
}
