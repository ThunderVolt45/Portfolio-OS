using System;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// URL 해시 딥링크로 특정 앱 창을 자동으로 연다.
    /// 예) <c>index.html#open=DocumentViewerWindow</c> → 해당 창을 시작 시 오픈.
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
            try
            {
                string url = Application.absoluteURL ?? string.Empty;
                const string key = "open=";
                int i = url.IndexOf(key, StringComparison.Ordinal);
                if (i < 0) return;

                string cls = url.Substring(i + key.Length);
                int end = cls.IndexOfAny(new[] { '&', '#', '/', '?' });
                if (end >= 0) cls = cls.Substring(0, end);
                cls = cls.Trim();
                if (cls.Length == 0) return;

                // 아이콘 오픈 경로와 동일한 방식(Type.GetType) — 스트리핑 안전 검증됨
                Type t = Type.GetType("UGUIWindow." + cls, false);
                if (t != null && typeof(UGUIWindow).IsAssignableFrom(t))
                {
                    UGUIWindowManager.CreateWindow(t);
                    Debug.Log("[PortfolioBootstrap] 딥링크 오픈: " + cls);
                }
                else
                {
                    Debug.LogWarning("[PortfolioBootstrap] 알 수 없는 창 클래스: " + cls);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PortfolioBootstrap] " + e.Message);
            }
        }
    }
}
