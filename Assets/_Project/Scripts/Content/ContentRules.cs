using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DBP.Interaction;
using DBP.Quiz;
using DBP.Weapons;

namespace DBP.Content
{
    /// Số tương tác tối đa của một màn, dùng tính ngân sách câu hỏi (BR-28).
    public struct MissionBudget
    {
        public string missionId;
        public int maxCrates, maxDrops, phones, bunkerQuestions;

        public static readonly MissionBudget[] Campaign =
        {
            new MissionBudget { missionId = "M1", maxCrates = 7, maxDrops = 3, phones = 1, bunkerQuestions = 5 },
            new MissionBudget { missionId = "M2", maxCrates = 7, maxDrops = 3, phones = 0, bunkerQuestions = 5 },
            new MissionBudget { missionId = "M3", maxCrates = 7, maxDrops = 3, phones = 0, bunkerQuestions = 5 },
            new MissionBudget { missionId = "M4", maxCrates = 7, maxDrops = 3, phones = 0, bunkerQuestions = 5 },
            new MissionBudget { missionId = "M5", maxCrates = 7, maxDrops = 3, phones = 0, bunkerQuestions = 5 },
        };
    }

    /// Luật kiểm tra dữ liệu trước phát hành (T40; BR-27–BR-30). Lỗi chặn bản nộp; cảnh báo thì không.
    public static class ContentRules
    {
        public const int MaxFieldStem = 120;
        public const int LatestYear = 1954;
        public static readonly string[] BannedWeapons = { "K-54", "K-50M" };

        /// Số ký tự hiển thị sau khi chuẩn hóa Unicode (BR-28): "ệ" gõ kiểu tổ hợp vẫn tính là 1.
        public static int VisibleLength(string text) =>
            string.IsNullOrEmpty(text) ? 0 : new StringInfo(text.Normalize(NormalizationForm.FormC)).LengthInTextElements;

        public static void CheckQuestions(IList<Question> questions, List<string> errors, List<string> warnings)
        {
            var ids = new HashSet<string>();
            foreach (var q in questions)
            {
                string at = $"Câu {q.id ?? "(không mã)"}";
                if (string.IsNullOrEmpty(q.id)) errors.Add($"{at}: thiếu mã câu");
                else if (!ids.Add(q.id)) errors.Add($"{at}: trùng mã trong ngân hàng (BR-27)");

                bool approved = q.status == "approved";
                var bucket = approved ? errors : warnings;

                if (q.level < 1 || q.level > 5) errors.Add($"{at}: cấp {q.level} ngoài 1–5");
                if (q.forms == null || q.forms.Length == 0 || q.forms.Any(f => f != "bunker" && f != "field"))
                    errors.Add($"{at}: forms phải là \"bunker\" và/hoặc \"field\"");

                var options = q.options ?? Array.Empty<QuestionOption>();
                if (options.Length < 2 || options.Length > 4) errors.Add($"{at}: cần 2–4 lựa chọn, đang có {options.Length}");
                if (options.Any(o => string.IsNullOrWhiteSpace(o.id) || string.IsNullOrWhiteSpace(o.text)))
                    errors.Add($"{at}: có lựa chọn rỗng");
                if (options.Select(o => o.id).Distinct().Count() != options.Length) errors.Add($"{at}: trùng mã đáp án trong câu");
                if (!options.Any(o => o.id == q.correct)) errors.Add($"{at}: correct \"{q.correct}\" không khớp đáp án nào");

                if (q.forms != null && q.forms.Contains("field") && VisibleLength(q.stem) > MaxFieldStem)
                    bucket.Add($"{at}: thân câu {VisibleLength(q.stem)} ký tự > {MaxFieldStem} (câu dùng ngoài trận, BR-28)");

                if (approved)
                {
                    if (string.IsNullOrWhiteSpace(q.source?.sourceId)) errors.Add($"{at}: câu approved thiếu nguồn (BR-28)");
                    if (string.IsNullOrWhiteSpace(q.fact)) errors.Add($"{at}: câu approved thiếu giải thích đáp án (BR-28)");
                }
                else warnings.Add($"{at}: trạng thái \"{q.status}\" — không được dùng trong bản nộp");
            }
        }

        /// Ngân sách câu (BR-28, BR-31): đủ câu đã duyệt cho trường hợp xấu nhất của mỗi màn.
        // ponytail: đếm theo từng cấp, coi mọi hòm cùng rơi vào một độ hiếm; câu dùng chung nhiều màn được đếm cho từng màn.
        public static void CheckBudget(IList<Question> questions, MissionBudget budget, List<string> errors)
        {
            var usable = questions.Where(q => q.status == "approved"
                                              && (q.missionId == budget.missionId || q.missionId == QuestionSelector.Campaign)).ToList();
            int Field(int min, int max) => usable.Count(q => q.level >= min && q.level <= max && q.forms != null && q.forms.Contains("field"));
            int bunker = usable.Count(q => q.level <= 3 && q.forms != null && q.forms.Contains("bunker"));
            int perRarity = budget.maxCrates + budget.maxDrops;
            int pity = budget.maxCrates / (PityTracker.Threshold + 1); // sai 3 hòm → 1 câu bảo hiểm ở hòm thứ 4

            void Need(string what, int have, int need)
            {
                if (have < need) errors.Add($"{budget.missionId}: thiếu câu {what}: có {have}, cần {need}");
            }

            Need("ngoài trận cấp 1 (bảo hiểm BR-16)", Field(1, 1), pity);
            Need("ngoài trận cấp 1–2 (hòm Thường)", Field(1, 2), perRarity + pity);
            Need("ngoài trận cấp 3 (hòm Khá + điện thoại)", Field(3, 3), perRarity + budget.phones);
            Need("ngoài trận cấp 4 (hòm Hiếm)", Field(4, 4), perRarity);
            Need("ngoài trận cấp 5 (hòm Rất hiếm)", Field(5, 5), perRarity);
            Need("trong hầm cấp 1–3", bunker, budget.bunkerQuestions);
        }

        public static void CheckWeapons(IList<WeaponDef> weapons, List<string> errors)
        {
            var ids = new HashSet<string>();
            foreach (var w in weapons)
            {
                string at = $"Vũ khí {w.id ?? "(không mã)"}";
                if (string.IsNullOrEmpty(w.id) || !ids.Add(w.id)) errors.Add($"{at}: thiếu hoặc trùng mã (BR-27)");
                if (string.IsNullOrWhiteSpace(w.name)) errors.Add($"{at}: thiếu tên");
                if (string.IsNullOrWhiteSpace(w.ammoType)) errors.Add($"{at}: thiếu loại đạn (BR-29)");
                if (BannedWeapons.Any(b => (w.id ?? "").Contains(b) || (w.name ?? "").Contains(b)))
                    errors.Add($"{at}: nằm trong danh sách loại K-54/K-50M (BR-18)");
                if (w.availableFrom <= 0) errors.Add($"{at}: chưa có availableFrom (cần căn cứ, BR-18)");
                else if (w.availableFrom > LatestYear) errors.Add($"{at}: availableFrom {w.availableFrom} > {LatestYear} (BR-18)");
                if (string.IsNullOrWhiteSpace(w.sourceId)) errors.Add($"{at}: thiếu nguồn sourceId (BR-29)");
                var s = w.stats;
                if (s == null || s.damage <= 0 || s.rpm <= 0 || s.mag <= 0 || s.reloadSec <= 0)
                    errors.Add($"{at}: chỉ số damage/rpm/mag/reloadSec phải > 0");
            }
        }

        /// Trọng số độ hiếm không âm, tổng bằng 1 ở 3 chữ số thập phân; không tự sửa tổng sai (BR-29).
        public static void CheckWeights(float[] weights, string where, List<string> errors)
        {
            if (weights == null || weights.Length != 4) { errors.Add($"{where}: cần 4 trọng số độ hiếm"); return; }
            if (weights.Any(w => w < 0f)) errors.Add($"{where}: trọng số âm");
            double sum = weights.Sum(w => (double)w);
            if (Math.Abs(sum - 1.0) > 0.0005) errors.Add($"{where}: tổng trọng số = {sum:0.###}, phải bằng 1");
        }

        public static void CheckChance(float chance, string where, List<string> errors)
        {
            if (chance < 0f || chance > 1f) errors.Add($"{where}: tỉ lệ {chance} ngoài 0–1 (BR-14)");
        }
    }
}
