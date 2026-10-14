using System;
using System.Collections.Generic;
using UnityEngine;

namespace DBP.Quiz
{
    /// T17: bản "thật" của IQuestionProvider, thay PlaceholderQuestionProvider.
    /// Next(level, form) trả null khi thiếu câu hợp lệ (đúng hợp đồng trong QuizTypes.cs) và ghi cảnh báo.
    /// Mỗi màn dùng một provider riêng (hoặc đổi MissionId khi vào màn mới).
    public sealed class JsonQuestionProvider : IQuestionProvider
    {
        private readonly QuestionSelector _selector;

        /// Màn hiện tại, ví dụ "M1".
        public string MissionId { get; set; }

        /// Đặt true khi cần lấy từ nhóm câu dự trữ (nhánh dự phòng). Nhớ đặt lại false sau đó.
        public bool UseReserve { get; set; }

        public QuestionSelector Selector { get { return _selector; } }

        public JsonQuestionProvider(QuestionSelector selector, string missionId)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            _selector = selector;
            MissionId = missionId;
        }

        /// Dựng nhanh từ các chuỗi JSON (ví dụ nội dung TextAsset hoặc file trong StreamingAssets).
        public static JsonQuestionProvider FromJson(IEnumerable<string> jsonFiles, string missionId,
                                                    QuestionSelectorOptions options = null, int? seed = null)
        {
            List<Question> bank = QuizQuestionParser.ParseMany(jsonFiles);
            var selector = new QuestionSelector(bank, options, seed);

            Debug.Log("[JsonQuestionProvider] Nạp " + selector.PoolSize + " câu, bỏ " + selector.Rejected.Count + " câu lỗi.");
            foreach (string reason in selector.Rejected) Debug.LogWarning("[JsonQuestionProvider] Câu bị loại: " + reason);

            return new JsonQuestionProvider(selector, missionId);
        }

        public Question Next(int level, QuizForm form)
        {
            var query = QuestionQuery.ForLevel(MissionId, form, level, UseReserve);
            Question q;
            string error;
            if (_selector.TryPick(query, out q, out error)) return q;

            Debug.LogWarning("[JsonQuestionProvider] " + error);
            return null;
        }

        /// Bắt đầu lượt chơi mới (chơi lại màn): câu đã dùng được tính lại từ đầu.
        public void ResetSession()
        {
            _selector.ResetSession();
        }
    }
}
