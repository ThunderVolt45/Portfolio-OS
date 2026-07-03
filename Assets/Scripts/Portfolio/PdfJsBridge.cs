using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UGUIWindow
{
    /// <summary>
    /// PDF.js(브라우저) 렌더 결과를 Unity 텍스처로 받아오는 브리지 (WebGL 전용, 개념 증명).
    /// jslib가 렌더 완료 시 SendMessage로 이 GameObject의 콜백 메서드를 호출한다.
    /// 에디터/비 WebGL 플랫폼에서는 즉시 오류 콜백을 반환한다.
    /// </summary>
    public class PdfJsBridge : MonoBehaviour
    {
        public static PdfJsBridge Instance { get; private set; }

        // 개념 증명 단계라 요청 슬롯은 1개만 유지한다(동시 렌더 미지원).
        private Action<Texture2D> _onSuccess;
        private Action<string> _onError;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PdfJsInit(string saPath);
        [DllImport("__Internal")] private static extern void PdfRenderPage(
            string url, int page, float scale, string goName, string okMethod, string errMethod);
#endif

        public static PdfJsBridge Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("PdfJsBridge");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<PdfJsBridge>();
                Instance.Init();
            }
            return Instance;
        }

        private void Init()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PdfJsInit(Application.streamingAssetsPath);
#endif
        }

        /// <param name="relativePath">StreamingAssets 기준 상대 경로 (예: "docs/resume.pdf")</param>
        public void RenderPage(string relativePath, int page, float scale,
            Action<Texture2D> onSuccess, Action<string> onError)
        {
            _onSuccess = onSuccess;
            _onError = onError;
#if UNITY_WEBGL && !UNITY_EDITOR
            string url = Application.streamingAssetsPath + "/" + relativePath;
            PdfRenderPage(url, page, scale, gameObject.name, nameof(OnPdfPageRendered), nameof(OnPdfError));
#else
            onError?.Invoke("PDF.js 렌더링은 WebGL 빌드에서만 동작합니다 (에디터 미지원).");
#endif
        }

        // === jslib에서 SendMessage로 호출되는 콜백 ===
        public void OnPdfPageRendered(string base64Png)
        {
            try
            {
                byte[] png = Convert.FromBase64String(base64Png);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(png); // 실제 크기로 리사이즈됨
                tex.Apply();
                _onSuccess?.Invoke(tex);
            }
            catch (Exception e)
            {
                _onError?.Invoke("텍스처 생성 실패: " + e.Message);
            }
        }

        public void OnPdfError(string message)
        {
            _onError?.Invoke(message);
        }
    }
}
