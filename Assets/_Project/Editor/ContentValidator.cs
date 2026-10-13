using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBP.Content;
using DBP.Interaction;
using DBP.Missions;
using DBP.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.EditorTools
{
    /// Nút Validate Content (T40; BR-27–BR-30): kiểm file JSON và cấu hình các màn trong Build Settings.
    /// Còn lỗi thì không build được bản nộp. Dòng lệnh: -executeMethod DBP.EditorTools.ContentValidator.ValidateCI
    public static class ContentValidator
    {
        public class Report
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();

            public override string ToString()
            {
                var sb = new StringBuilder($"Validate Content: {Errors.Count} lỗi, {Warnings.Count} cảnh báo\n");
                foreach (var e in Errors) sb.AppendLine($"  ✖ {e}");
                foreach (var w in Warnings) sb.AppendLine($"  ⚠ {w}");
                return sb.ToString();
            }
        }

        [MenuItem("DBP/Validate Content")]
        public static void RunFromMenu()
        {
            var report = Validate();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Validate Content",
                report.Errors.Count == 0 ? $"Không có lỗi. {report.Warnings.Count} cảnh báo (xem Console)."
                                         : $"{report.Errors.Count} lỗi, {report.Warnings.Count} cảnh báo. Xem Console.", "OK");
        }

        public static void ValidateCI()
        {
            var report = Validate();
            Debug.Log(report);
            EditorApplication.Exit(report.Errors.Count == 0 ? 0 : 1);
        }

        public static Report Validate()
        {
            var r = new Report();

            // Vũ khí
            if (ContentLoader.TryLoad<WeaponFileView>("weapons.json", out var wf, out var werr))
                ContentRules.CheckWeapons(wf.weapons ?? new WeaponDef[0], r.Errors);
            else r.Errors.Add(werr);

            // Câu hỏi
            var questions = QuestionBank.LoadAll(r.Errors);
            ContentRules.CheckQuestions(questions, r.Errors, r.Warnings);
            foreach (var budget in MissionBudget.Campaign) ContentRules.CheckBudget(questions, budget, r.Errors);

            // Cấu hình màn (BR-13, BR-14, BR-29, BR-30, BR-38)
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
                CheckScene(entry.path, r);
            return r;
        }

        [System.Serializable]
        class WeaponFileView { public WeaponDef[] weapons; }

        static void CheckScene(string path, Report r)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                string name = scene.name;
                T[] All<T>() where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

                var missions = All<MissionController>();
                if (missions.Length != 1) r.Errors.Add($"{name}: cần đúng 1 MissionController, đang có {missions.Length}");
                foreach (var m in missions)
                {
                    if (!m.Player) r.Errors.Add($"{name}: MissionController chưa gắn người chơi (BR-30)");
                    if (m.Objectives.Count == 0 || m.Objectives.Any(o => !o)) r.Errors.Add($"{name}: thiếu hoặc hỏng danh sách mục tiêu bắt buộc (BR-30)");
                    ContentRules.CheckChance(m.EnemyDropChance, $"{name}: tỉ lệ súng rơi", r.Errors);
                    if (m.MaxEnemyDrops < 0) r.Errors.Add($"{name}: giới hạn súng rơi âm (BR-14)");
                }

                foreach (var s in All<CrateSpawner>())
                {
                    string at = $"{name}/{s.name}";
                    ContentRules.CheckWeights(s.rarityWeights, at, r.Errors);
                    if (s.points.Count < 10 || s.points.Count > 15) r.Errors.Add($"{at}: cần 10–15 điểm đặt hòm, đang có {s.points.Count} (BR-13)");
                    if (s.points.Any(p => !p)) r.Errors.Add($"{at}: có điểm đặt hòm bị trống");
                    if (s.minCrates > s.maxCrates || s.maxCrates > s.points.Count) r.Errors.Add($"{at}: số hòm {s.minCrates}–{s.maxCrates} không hợp lệ");
                    for (int i = 0; i < 4 && s.rarityWeights != null && i < s.rarityWeights.Length; i++)
                        if (s.rarityWeights[i] > 0f && !s.rewards.Any(w => (int)w.rarity == i))
                            r.Errors.Add($"{at}: độ hiếm {(Rarity)i} có thể rút nhưng không có phần thưởng (BR-29)");
                    foreach (var w in s.rewards.Where(w => !string.IsNullOrEmpty(w.weaponId)))
                        if (WeaponCatalog.Get(w.weaponId) == null) r.Errors.Add($"{at}: phần thưởng tham chiếu vũ khí không có: {w.weaponId}");
                }

                foreach (var p in All<FieldPhone>())
                    if (!p.target) r.Errors.Add($"{name}/{p.name}: điện thoại chưa gắn mục tiêu (BR-38)");
                foreach (var c in All<QuizCrate>())
                    if (!string.IsNullOrEmpty(c.rewardWeaponId) && WeaponCatalog.Get(c.rewardWeaponId) == null)
                        r.Errors.Add($"{name}/{c.name}: tham chiếu vũ khí không có: {c.rewardWeaponId}");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
