using System;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// URL 해시 딥링크로 특정 앱 창을 자동으로 연다.
    /// 예) <c>index.html#open=DocumentViewerWindow</c> → 이력서 안내 창을 시작 시 오픈.
    /// 씬 오브젝트 없이 RuntimeInitializeOnLoadMethod로 부팅한다.
    /// (테스트 편의 + 향후 monitor.html 셸에서 특정 앱으로 진입하는 딥링크에 사용)
    /// </summary>
    public class PortfolioBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("PortfolioBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<PortfolioBootstrap>();
        }

        private void Start()
        {
            // 딥링크가 있으면 그 창만 연다. 없으면 기본 시작 창 세트를 연다.
            if (!TryOpenDeepLink())
            {
                OpenStartupWindows();
            }
        }

        /// <summary>
        /// 부팅 직후 기본으로 띄우는 창들. 생성 순서 = z-순서이므로
        /// DPI 설정 창을 마지막에 열어 최상단에 오게 한다.
        /// 각 창의 크기는 창 자신이 정하므로(CreateWindowEx는 그 크기를 덮어씀)
        /// CreateWindow + Move로 위치만 겹치지 않게 배치한다.
        /// UGUI-Window-Sample 창은 시작 세트에서 뺐다 — 도크에서 언제든 열 수 있다.
        /// </summary>
        private static void OpenStartupWindows()
        {
            // About(500x600) → -470~30, DPI(380x250) → 80~460. 두 창을 합친 폭이 캔버스
            // 중앙에 오도록 잡았다(한 창만 남기면 화면이 한쪽으로 쏠린다).
            PlaceCentered(UGUIWindowManager.CreateWindow<AboutWindow>(), -220);

            var dpi = UGUIWindowManager.CreateWindow<DpiSettingWindow>();
            if (dpi != null)
            {
                PlaceCentered(dpi, 270);
                dpi.Focus(); // 최상단 + 포커스 표시를 매니저에 알린다.
            }
        }

        /// <summary>지정한 x에 놓고 헤더까지 포함한 창 전체가 세로 중앙에 오게 한다.</summary>
        private static void PlaceCentered(UGUIWindow window, int x)
        {
            if (window == null)
            {
                return;
            }

            // 헤더는 창 rect 안이 아니라 그 위에 얹혀 있다. 그래서 rect를 y=0에 두면 눈에 보이는
            // 창(=rect + 헤더)의 중심은 헤더가 튀어나온 만큼의 절반이 위로 밀린다. 그만큼 내려준다.
            window.Move(x, Mathf.RoundToInt(-HeaderOverhang(window) * 0.5f));
        }

        /// <summary>
        /// 헤더가 창 rect 위로 튀어나온 높이. 헤더는 rect 상단에 앵커·피벗을 두고 그 위로
        /// anchoredPosition.y만큼 올라가 있으므로 그 값이 곧 튀어나온 양이다.
        /// (UGUIWindow.ApplyMaximizedLayout이 최대화 시 창 위를 깎는 데 쓰는 값과 동일하다.)
        /// </summary>
        private static float HeaderOverhang(UGUIWindow window)
        {
            var header = window.GetComponentInChildren<UGUIWindowHeader>(true);
            return header != null ? ((RectTransform)header.transform).anchoredPosition.y : 0f;
        }

        /// <summary>URL 해시에서 <c>open=</c> 딥링크를 읽어 해당 창을 연다.</summary>
        /// <returns>딥링크로 창을 열었으면 true.</returns>
        private static bool TryOpenDeepLink()
        {
            try
            {
                string url = Application.absoluteURL ?? string.Empty;
                const string key = "open=";
                int i = url.IndexOf(key, StringComparison.Ordinal);
                if (i < 0) return false;

                string cls = url.Substring(i + key.Length);
                int end = cls.IndexOfAny(new[] { '&', '#', '/', '?' });
                if (end >= 0) cls = cls.Substring(0, end);
                cls = cls.Trim();
                if (cls.Length == 0) return false;

                // 아이콘 오픈 경로와 동일한 방식(Type.GetType) — 스트리핑 안전 검증됨
                Type t = Type.GetType("UGUIWindow." + cls, false);
                if (t != null && typeof(UGUIWindow).IsAssignableFrom(t))
                {
                    UGUIWindowManager.CreateWindow(t);
                    Debug.Log("[PortfolioBootstrap] 딥링크 오픈: " + cls);
                    return true;
                }

                // 알 수 없는 창이면 빈 화면으로 두지 않고 기본 시작 창을 연다.
                Debug.LogWarning("[PortfolioBootstrap] 알 수 없는 창 클래스: " + cls);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("[PortfolioBootstrap] " + e.Message);
                return false;
            }
        }
    }
}
