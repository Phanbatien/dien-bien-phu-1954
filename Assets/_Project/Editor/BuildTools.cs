using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Build Windows (T64, T74). Menu DBP → Build, hoặc dòng lệnh:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod DBP.EditorTools.BuildTools.BuildWindowsCI
    public static class BuildTools
    {
        public const string OutputPath = "Builds/Windows/DienBienPhu1954.exe";

        [MenuItem("DBP/Build/Windows (bản nộp)")]
        public static void BuildRelease() => Build(BuildOptions.None);

        [MenuItem("DBP/Build/Windows (Development: có FPS, phím F9)")]
        public static void BuildDevelopment() => Build(BuildOptions.Development);

        public static void BuildWindowsCI() => EditorApplication.Exit(Build(BuildOptions.None) ? 0 : 1);

        static bool Build(BuildOptions options)
        {
            // T40 (Long): gọi Validate Content ở đây và dừng build nếu còn lỗi bắt buộc (BR-27–BR-30).
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] Chưa có scene trong Build Settings. Chạy DBP → Greybox → Tạo scene còn thiếu.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = options,
            });
            var s = report.summary;
            Debug.Log($"[Build] {s.result}: {s.outputPath} · {s.totalSize / (1024 * 1024)} MB · {s.totalErrors} lỗi · {s.totalTime.TotalSeconds:0}s");
            return s.result == BuildResult.Succeeded;
        }
    }
}
