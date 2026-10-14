using System;
using System.Collections.Generic;
using System.IO;
using DBP.Quiz;
using UnityEngine;

namespace DBP.Content
{
    /// Đọc dữ liệu JSON trong StreamingAssets/content (T11; BR-27). Lỗi trả về dạng chữ rõ ràng, không ném ngoại lệ.
    // ponytail: đọc file trực tiếp, đủ cho bản Windows. WebGL/Android cần UnityWebRequest nếu làm đa nền tảng.
    public static class ContentLoader
    {
        /// Thư mục gốc nội dung. Test đổi sang thư mục tạm.
        public static string Root = Path.Combine(Application.streamingAssetsPath, "content");

        public static bool TryLoad<T>(string relativePath, out T data, out string error)
        {
            data = default;
            error = null;
            var path = Path.Combine(Root, relativePath);
            if (!File.Exists(path))
            {
                error = $"Thiếu file content/{relativePath}";
                return false;
            }
            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                error = $"Hỏng JSON content/{relativePath}: {e.Message}";
                return false;
            }
            if (data == null)
            {
                error = $"File rỗng: content/{relativePath}";
                return false;
            }
            return true;
        }

        /// Tên các file .json trong một thư mục con (vd. "questions"), sắp theo tên.
        public static List<string> Files(string folder)
        {
            var dir = Path.Combine(Root, folder);
            var list = new List<string>();
            if (!Directory.Exists(dir)) return list;
            foreach (var f in Directory.GetFiles(dir, "*.json")) list.Add(Path.Combine(folder, Path.GetFileName(f)));
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }

    [Serializable]
    public class QuestionFile
    {
        public Question[] questions;
    }

    /// Toàn bộ ngân hàng câu hỏi: questions/campaign.json (dùng chung) + questions/m1…m5.json.
    public static class QuestionBank
    {
        public static List<Question> LoadAll(List<string> errors)
        {
            var all = new List<Question>();
            foreach (var file in ContentLoader.Files("questions"))
            {
                if (!ContentLoader.TryLoad<QuestionFile>(file, out var data, out var error))
                {
                    errors.Add(error);
                    continue;
                }
                if (data.questions != null) all.AddRange(data.questions);
            }
            return all;
        }
    }
}
