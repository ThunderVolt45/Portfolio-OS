using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PortfolioOS.EditorTools
{
    /// <summary>
    /// 재현 가능한 WebGL 빌드 진입점. 배포에 결정적인 PlayerSettings를
    /// 코드로 고정한다(에디터 UI 상태에 의존하지 않음).
    ///
    /// GitHub Pages 제약 반영:
    ///  - compressionFormat = Brotli + decompressionFallback = Enabled
    ///    (Pages가 Content-Encoding 헤더를 못 넣으므로, Unity 내장 JS 디컴프레서가
    ///     클라이언트에서 .br을 해제하도록 fallback을 켠다. 배포 산출물 ~30MB.)
    ///  - 싱글스레드 (SharedArrayBuffer/COOP·COEP 불가)
    ///  - wasm2023 = false (구형 Safari 호환 — 아래 주석 참조)
    ///  - 파일당 100MB 하드 제한 → release + High 스트리핑으로 wasm 경량화
    ///
    /// 배치모드:
    ///   Unity -batchmode -quit -projectPath . \
    ///     -executeMethod PortfolioOS.EditorTools.PortfolioBuild.BuildWebGLRelease
    /// </summary>
    public static class PortfolioBuild
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/PortfolioOS.unity",
        };

        const string OutDir = "Build/WebGL";

        [MenuItem("Portfolio/Build WebGL (Release)")]
        public static void BuildWebGLRelease() => Build(development: false);

        [MenuItem("Portfolio/Build WebGL (Development)")]
        public static void BuildWebGLDevelopment() => Build(development: true);

        static void Build(bool development)
        {
            var nbt = NamedBuildTarget.WebGL;

            // --- 배포 결정적 설정 ---
            // Brotli 압축 + 디컴프레션 폴백: GH Pages는 Content-Encoding 헤더를 못 넣으므로
            // Unity 내장 JS 디컴프레서가 클라이언트에서 .br을 풀도록 fallback을 켠다(§4-C).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            // 전체 화면(뷰포트) 셸: Unity 캔버스가 페이지 전체를 채우도록 커스텀 템플릿 고정.
            PlayerSettings.WebGL.template = "PROJECT:PortfolioFull";
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            // WebAssembly 2023(SIMD 등 최신 wasm 기능) 요구를 끈다.
            // 켜져 있으면 로더가 "Your browser does not support WebAssembly 2023.
            // ... Safari >= 16.4"로 하드 실패한다. Safari 16.4는 macOS Ventura 이상 전용이라
            // Monterey/Big Sur Mac(Safari 15.6 고정) + iPadOS 15 이하는 전부 입장 불가였다.
            // 채용 담당자 단말을 고를 수 없으므로 호환성을 성능보다 우선한다.
            PlayerSettings.WebGL.wasm2023 = false;
            PlayerSettings.WebGL.threadsSupport = false; // 싱글스레드
            PlayerSettings.WebGL.dataCaching = false;

            // 크기·최적화: release는 High 스트리핑 + Master IL2CPP, dev는 디버그 편의 우선
            PlayerSettings.SetManagedStrippingLevel(
                nbt, development ? ManagedStrippingLevel.Minimal : ManagedStrippingLevel.High);
            // Release 설정: Master보다 빌드가 크게 빠르고, 최종 크기는 High 스트리핑이
            // 대부분 좌우하므로 반복 검증에 유리. 배포 직전 극한 경량화가 필요하면 Master로.
            PlayerSettings.SetIl2CppCompilerConfiguration(
                nbt, development ? Il2CppCompilerConfiguration.Debug : Il2CppCompilerConfiguration.Release);
            PlayerSettings.WebGL.exceptionSupport = development
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            Directory.CreateDirectory(OutDir);
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;

            if (s.result == BuildResult.Succeeded)
            {
                long wasm = WasmSizeBytes();
                Debug.Log(
                    $"[PortfolioBuild] SUCCESS ({(development ? "dev" : "release")}) " +
                    $"total={s.totalSize / (1024 * 1024f):0.0}MB " +
                    $"wasm={wasm / (1024 * 1024f):0.0}MB time={s.totalTime} out={OutDir}");
                if (!development && wasm >= 100L * 1024 * 1024)
                    Debug.LogWarning($"[PortfolioBuild] wasm이 100MB 이상 — GH Pages 배포 불가. 추가 경량화 필요.");
            }
            else
            {
                Debug.LogError($"[PortfolioBuild] FAILED result={s.result} errors={s.totalErrors}");
            }
        }

        static long WasmSizeBytes()
        {
            var f = new FileInfo(Path.Combine(OutDir, "Build", "WebGL.wasm"));
            return f.Exists ? f.Length : -1;
        }
    }
}
