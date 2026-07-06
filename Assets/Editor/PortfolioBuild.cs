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
    ///  - compressionFormat = Disabled (Pages가 Content-Encoding 헤더 못 넣음)
    ///  - 싱글스레드 (SharedArrayBuffer/COOP·COEP 불가)
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
            "Assets/UGUIWindowSample/Scenes/UGUIWindowSampleScene.unity",
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
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false; // Disabled면 무의미하나 명시
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
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
