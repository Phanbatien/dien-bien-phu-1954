using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Cảnh báo khi máy chưa tải file Git LFS (31/10): ảnh .png, font .ttf, model .fbx lưu bằng Git LFS. Máy chưa cài
    /// Git LFS hoặc tải "Download ZIP" sẽ chỉ có file con trỏ vài trăm byte → mặt đất / tay trắng, chữ giao diện mất.
    /// Mở project là kiểm tra vài file tiêu biểu; thiếu thì hiện hộp thoại kèm lệnh sửa.
    [InitializeOnLoad]
    static class LfsCheck
    {
        static readonly string[] Samples =
        {
            "Assets/_Project/Resources/Fonts/BeVietnamPro-Bold.ttf",
            "Assets/_Project/Art/Terrain/T_CoUa.png",
            "Assets/_Project/Art/Terrain/T_BunDat.png",
        };

        static LfsCheck() => EditorApplication.delayCall += () => Check();

        [MenuItem("DBP/Kiểm tra file Git LFS")]
        static void CheckMenu()
        {
            if (!Check()) EditorUtility.DisplayDialog("Git LFS", "Đủ file Git LFS (ảnh, font, model).", "OK");
        }

        /// Trả true nếu phát hiện file con trỏ LFS chưa tải.
        static bool Check()
        {
            var stubs = Samples.Where(IsPointer).ToArray();
            if (stubs.Length == 0) return false;
            string message = "Project thiếu file Git LFS (ảnh, font, model chỉ là file con trỏ) nên mặt đất và tay nhân vật bị trắng, chữ giao diện mất.\n\n" +
                             "Cách sửa (đóng Unity trước):\n" +
                             "1. Cài Git LFS: https://git-lfs.com (hoặc cài lại Git for Windows, chọn Git LFS)\n" +
                             "2. Trong thư mục project chạy:\n   git lfs install\n   git lfs pull\n" +
                             "3. Mở lại Unity.\n\n" +
                             "Nếu đã tải bằng 'Download ZIP' trên GitHub: phải clone bằng git thay vì ZIP (ZIP không chứa file LFS).\n\n" +
                             "File thiếu: " + string.Join(", ", stubs.Select(Path.GetFileName));
            Debug.LogError("[LFS] " + message);
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Thiếu file Git LFS", message, "Đã hiểu");
            return true;
        }

        static bool IsPointer(string path)
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length > 1024) return false;
            return File.ReadAllText(path).StartsWith("version https://git-lfs");
        }
    }
}
