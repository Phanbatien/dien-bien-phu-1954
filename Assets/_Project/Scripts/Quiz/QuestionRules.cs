using System;

namespace DBP.Quiz
{
    /// Các quy tắc kiểm tra cho lớp Question của nhóm (QuizTypes.cs), viết dạng hàm mở rộng
    /// để không phải sửa QuizTypes.cs.
    public static class QuestionRules
    {
        public static bool IsApproved(this Question q)
        {
            return q != null && string.Equals(q.status, "approved", StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasForm(this Question q, QuizForm form)
        {
            if (q == null || q.forms == null) return false;
            string key = form == QuizForm.Field ? "field" : "bunker";
            for (int i = 0; i < q.forms.Length; i++)
            {
                if (string.Equals(q.forms[i], key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// So theo id đáp án (không theo vị trí), nên xáo đáp án khi hiển thị vẫn chấm đúng.
        public static bool IsCorrectAnswer(this Question q, string answerId)
        {
            return q != null && string.Equals(answerId, q.correct, StringComparison.Ordinal);
        }

        /// Kiểm tra tối thiểu để game không lỗi khi hiển thị câu.
        /// Kiểm tra đầy đủ (≤120 ký tự, có nguồn, đủ câu mỗi độ hiếm...) là việc của Validate Content (T40).
        public static bool TryValidateStructure(this Question q, out string reason)
        {
            if (q == null) { reason = "câu rỗng"; return false; }
            if (string.IsNullOrWhiteSpace(q.id)) { reason = "thiếu id"; return false; }
            if (string.IsNullOrWhiteSpace(q.missionId)) { reason = "thiếu missionId"; return false; }
            if (q.level < 1 || q.level > 5) { reason = "level ngoài khoảng 1–5"; return false; }
            if (string.IsNullOrWhiteSpace(q.stem)) { reason = "thiếu nội dung câu hỏi (stem)"; return false; }
            if (q.forms == null || q.forms.Length == 0) { reason = "thiếu forms"; return false; }
            if (q.options == null || q.options.Length < 2 || q.options.Length > 4)
            {
                reason = "số đáp án phải từ 2 đến 4";
                return false;
            }

            bool foundCorrect = false;
            for (int i = 0; i < q.options.Length; i++)
            {
                QuestionOption o = q.options[i];
                if (o == null || string.IsNullOrWhiteSpace(o.id)) { reason = "đáp án thiếu id"; return false; }
                for (int j = 0; j < i; j++)
                {
                    if (q.options[j] != null && q.options[j].id == o.id) { reason = "trùng id đáp án"; return false; }
                }
                if (o.id == q.correct) foundCorrect = true;
            }
            if (!foundCorrect) { reason = "correct không khớp id đáp án nào"; return false; }

            reason = null;
            return true;
        }
    }
}
