using System.Collections.Generic;

namespace DBP.Quiz
{
    // ponytail: vài câu nháp để chạy thử vòng hòm → câu hỏi. T17 (Thiên Trí) thay bằng bộ chọn
    // đọc questions/*.json, lọc approved và không lặp trong lượt (BR-31). Không dùng cho bản phát hành.
    public class PlaceholderQuestionProvider : IQuestionProvider
    {
        static readonly Question[] Samples =
        {
            Make("Q-DRAFT-001", 1, "Trận mở màn chiến dịch Điện Biên Phủ (13/3/1954) đánh vào cứ điểm nào?",
                "a", "Chiều 13/3/1954 quân ta tiến công cứ điểm Him Lam.",
                "Him Lam", "Đồi A1", "Hồng Cúm", "Mường Thanh"),
            Make("Q-DRAFT-002", 2, "Anh hùng nào lấy thân mình lấp lỗ châu mai ở Him Lam?",
                "b", "Phan Đình Giót hi sinh khi lấp lỗ châu mai lô cốt Him Lam.",
                "Tô Vĩnh Diện", "Phan Đình Giót", "Bế Văn Đàn", "Cù Chính Lan"),
            Make("Q-DRAFT-003", 3, "Chiến dịch Điện Biên Phủ kết thúc thắng lợi vào ngày nào?",
                "c", "Chiều 7/5/1954 ta đánh vào hầm chỉ huy, bắt sống De Castries.",
                "13/3/1954", "30/3/1954", "7/5/1954", "21/7/1954"),
        };

        int next;

        public Question Next(int level, QuizForm form) => Samples[next++ % Samples.Length];

        static Question Make(string id, int level, string stem, string correct, string fact, params string[] options)
        {
            var list = new List<QuestionOption>();
            for (int i = 0; i < options.Length; i++)
                list.Add(new QuestionOption { id = ((char)('a' + i)).ToString(), text = options[i] });
            return new Question
            {
                id = id, level = level, stem = stem, correct = correct, fact = fact,
                forms = new[] { "bunker", "field" }, options = list.ToArray(), status = "draft",
            };
        }
    }
}
