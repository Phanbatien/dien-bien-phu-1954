using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Build Windows (T64) và đóng gói bản nộp (T74). Menu DBP → Build, hoặc dòng lệnh:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod DBP.EditorTools.BuildTools.BuildWindowsCI
    public static class BuildTools
    {
        public const string OutputPath = "Builds/Windows/DienBienPhu1954.exe";
        const string NotShipped = "_BurstDebugInformation_DoNotShip";

        [MenuItem("DBP/Build/Windows (bản nộp: Validate + .zip)")]
        public static void BuildRelease() => Build(release: true);

        [MenuItem("DBP/Build/Windows (Development: có FPS, phím F9)")]
        public static void BuildDevelopment() => Build(release: false);

        public static void BuildWindowsCI() => EditorApplication.Exit(Build(release: true) ? 0 : 1);
        public static void BuildDevelopmentCI() => EditorApplication.Exit(Build(release: false) ? 0 : 1);

        /// Chỉ đóng gói bản build đã có (dùng khi build tay rồi muốn nén lại).
        public static void PackageCI()
        {
            if (!File.Exists(OutputPath)) { Debug.LogError("[Build] Chưa có bản build để đóng gói."); EditorApplication.Exit(1); return; }
            Debug.Log($"[Build] Đã đóng gói: {Zip()}");
            EditorApplication.Exit(0);
        }

        static bool Build(bool release)
        {
            // Validate Content chặn bản nộp khi còn lỗi bắt buộc (BR-27–BR-30); bản Development chỉ cảnh báo.
            var report = ContentValidator.Validate();
            if (report.Errors.Count > 0)
            {
                if (release)
                {
                    Debug.LogError($"[Build] Dừng: {report}");
                    return false;
                }
                Debug.LogWarning($"[Build] Development build vẫn chạy dù còn lỗi nội dung. {report}");
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] Chưa có scene trong Build Settings. Chạy DBP → Greybox → Tạo scene còn thiếu.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = release ? BuildOptions.None : BuildOptions.Development,
            });
            var s = result.summary;
            Debug.Log($"[Build] {s.result}: {s.outputPath} · {s.totalSize / (1024 * 1024)} MB · {s.totalErrors} lỗi · {s.totalTime.TotalSeconds:0}s");
            if (s.result != BuildResult.Succeeded) return false;
            if (release) Debug.Log($"[Build] Đã đóng gói: {Zip()}");
            return true;
        }

        /// Nén thư mục build thành Builds/DienBienPhu1954_v&lt;version&gt;.zip, bỏ thư mục debug không phát hành.
        static string Zip()
        {
            var dir = Path.GetDirectoryName(OutputPath);
            var zipPath = $"Builds/DienBienPhu1954_v{PlayerSettings.bundleVersion}.zip";
            if (File.Exists(zipPath)) File.Delete(zipPath);
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    if (file.Contains(NotShipped)) continue;
                    var entry = Path.GetRelativePath(dir, file).Replace('\\', '/');
                    zip.CreateEntryFromFile(file, $"DienBienPhu1954/{entry}", System.IO.Compression.CompressionLevel.Optimal);
                }
            }
            return zipPath;
        }
    }
}
