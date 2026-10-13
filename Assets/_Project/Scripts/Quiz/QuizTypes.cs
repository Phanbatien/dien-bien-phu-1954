using System;

namespace DBP.Quiz
{
    /// Một câu hỏi theo schema GDD §8 (questions/m1…m5.json). Đáp án đúng ghi theo id, không theo vị trí.
    [Serializable]
    public class Question
    {
        public string id;
        public string missionId;
        public int level;
        public string[] forms;
        public string stem;
        public QuestionOption[] options;
        public string correct;
        public string fact;
        public QuestionSource source;
        public string status;
        public int revision;
    }

    [Serializable]
    public class QuestionSource
    {
        public string sourceId;
        public string page;
    }

    [Serializable]
    public class QuestionOption
    {
        public string id;
        public string text;
    }

    public enum QuizForm { Bunker, Field }
    public enum QuizOutcome { Correct, Wrong, Timeout }

    /// Kết quả một câu ngoài trận. T19 (Long) tính điểm từ đây (BR-25).
    [Serializable]
    public struct QuizResult
    {
        public string questionId;
        public int level;
        public float timeLimit;
        public float remaining;
        public QuizOutcome outcome;
    }

    /// Nguồn câu hỏi: QuestionSelector (T17). Trả null khi thiếu câu hợp lệ (BR-31).
    public interface IQuestionProvider
    {
        /// Câu có cấp trong [minLevel, maxLevel], dùng được ở hình thức form, chưa gặp trong lượt.
        Question Next(int minLevel, int maxLevel, QuizForm form);
    }

    /// Giao diện câu hỏi. Bản thật: UI câu hỏi ngoài trận của T18 (Thiên Trí); bản tạm là DebugQuizView.
    /// Phải chạy bằng thời gian thực vì lúc hiển thị Time.timeScale = 0.
    public interface IQuizView
    {
        void Show(Question question, float timeLimit, Action<string> onAnswer);
        void SetRemaining(float seconds);
        void Hide();
    }

    /// Đồng hồ câu ngoài trận (BR-10): chỉ nhận lựa chọn đầu tiên trước hạn;
    /// đúng hoặc sau hạn là hết giờ. Thời gian thực, không dừng khi mở menu/mất tiêu điểm.
    public class QuizTimer
    {
        readonly double deadline;

        public bool Settled { get; private set; }
        public float RemainingAtSettle { get; private set; }

        public QuizTimer(float timeLimit, double now) => deadline = now + timeLimit;

        public float Remaining(double now) => (float)Math.Max(0.0, deadline - now);

        public bool TryAnswer(double now)
        {
            if (Settled || now >= deadline) return false;
            Settled = true;
            RemainingAtSettle = Remaining(now);
            return true;
        }

        public bool CheckTimeout(double now)
        {
            if (Settled || now < deadline) return false;
            Settled = true;
            RemainingAtSettle = 0f;
            return true;
        }
    }
}
