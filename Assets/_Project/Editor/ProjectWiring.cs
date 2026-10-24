using System;
using System.Collections.Generic;
using System.Linq;
using DBP.Quiz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Nối các hệ thống của từng người vào scene đã có, không dựng lại scene (giữ dressing của Khôi).
    /// Chạy lại nhiều lần vẫn cho cùng kết quả. Menu DBP → Nối hệ thống vào scene.
    /// Dòng lệnh: -executeMethod DBP.EditorTools.ProjectWiring.WireAllCI
    public static partial class ProjectWiring
    {
        public static readonly string[] MissionScenes =
        {
            "Assets/_Project/Scenes/M1_HimLam.unity", "Assets/_Project/Scenes/M2_GiuDocLap.unity",
            "Assets/_Project/Scenes/M3_DoiED.unity", "Assets/_Project/Scenes/M4_LongHao.unity",
            "Assets/_Project/Scenes/M5_ChieuMungBay.unity", "Assets/_Project/Scenes/Sandbox.unity",
        };

        /// Các bước nối, theo thứ tự. Mỗi bước nhận scene đang mở và trả về mô tả thay đổi.
        static readonly List<(string name, Func<string, string> step)> Steps = new List<(string, Func<string, string>)>
        {
            ("Giao diện câu hỏi T18", WireQuizPanel),
        };

        [MenuItem("DBP/Nối hệ thống vào scene")]
        public static void WireAll()
        {
            foreach (var path in MissionScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var (name, step) in Steps)
                {
                    var note = step(scene.name);
                    if (!string.IsNullOrEmpty(note)) Debug.Log($"[Wiring] {scene.name} · {name}: {note}");
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        public static void WireAllCI()
        {
            try { WireAll(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static T[] All<T>() where T : Component =>
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        /// T18: thay giao diện câu hỏi tạm (DebugQuizView) bằng QuizPanel của Thiên Trí.
        static string WireQuizPanel(string sceneName)
        {
            var session = All<FieldQuizSession>().FirstOrDefault();
            if (!session) return "không có FieldQuizSession";
            var go = session.gameObject;
            int removed = 0;
            foreach (var debug in go.GetComponents<DebugQuizView>()) { UnityEngine.Object.DestroyImmediate(debug); removed++; }
            bool added = false;
            if (!go.GetComponent<QuizPanel>()) { go.AddComponent<QuizPanel>(); added = true; }
            return removed + (added ? 1 : 0) == 0 ? null : $"gỡ {removed} DebugQuizView, {(added ? "thêm" : "giữ")} QuizPanel";
        }
    }
}
