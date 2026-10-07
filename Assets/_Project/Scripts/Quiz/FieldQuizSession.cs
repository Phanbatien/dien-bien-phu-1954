using System;
using DBP.Core;
using UnityEngine;

namespace DBP.Quiz
{
    /// Vòng đời một câu hỏi ngoài trận (T13; BR-09–BR-12):
    /// mở câu → đóng băng thế giới → đếm giờ thực → chốt đáp án → phát thưởng → 0,5 s chuyển tiếp → chạy lại.
    /// Hòm vũ khí, súng rơi (T22–T23) và điện thoại gọi pháo (T31) đều gọi TryOpen.
    public class FieldQuizSession : MonoBehaviour
    {
        public static FieldQuizSession Instance { get; private set; }

        /// Mỗi câu đã chốt: Attempt ghi lại, T19 tính điểm kiến thức.
        public static event Action<QuizResult> Answered;
        /// Thiếu câu giữa lượt (BR-31): lượt không được lên bảng xếp hạng.
        public static event Action<int> QuestionShortage;

        [SerializeField] WorldFreeze freeze;

        public IQuestionProvider Questions { get; set; }
        public IQuizView View { get; set; }
        /// true từ lúc mở câu tới hết 0,5 s chuyển tiếp: không mở câu thứ hai (BR-12).
        public bool IsOpen { get; private set; }

        QuizTimer timer;
        Question current;
        float timeLimit;
        Action<QuizResult> onResolved;

        static double Now => Time.realtimeSinceStartupAsDouble;

        void Awake()
        {
            Instance = this;
            if (!freeze) freeze = FindAnyObjectByType<WorldFreeze>();
            if (Questions == null) Questions = new PlaceholderQuestionProvider();
            if (View == null) View = TryGetComponent<IQuizView>(out var view) ? view : gameObject.AddComponent<DebugQuizView>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// Trả false nếu không mở được: đang có câu khác, hoặc thiếu câu (khi đó đối tượng phải tự khóa, BR-31).
        public bool TryOpen(int level, float limitSeconds, Action<QuizResult> resolved)
        {
            if (IsOpen || !freeze) return false;
            var question = Questions.Next(level, QuizForm.Field);
            if (question == null)
            {
                Debug.LogWarning($"[Quiz] Thiếu câu cấp {level} cho hình thức ngoài trận (BR-31).");
                QuestionShortage?.Invoke(level);
                return false;
            }

            IsOpen = true;
            current = question;
            timeLimit = limitSeconds;
            onResolved = resolved;
            freeze.Freeze();
            View.Show(question, limitSeconds, OnAnswer);
            timer = new QuizTimer(limitSeconds, Now); // bắt đầu khi câu và lựa chọn đã hiện
            return true;
        }

        void Update()
        {
            if (!IsOpen || timer == null || timer.Settled) return;
            View.SetRemaining(timer.Remaining(Now));
            if (timer.CheckTimeout(Now)) Finish(QuizOutcome.Timeout);
        }

        void OnAnswer(string optionId)
        {
            if (!IsOpen || timer == null || !timer.TryAnswer(Now)) return;
            Finish(optionId == current.correct ? QuizOutcome.Correct : QuizOutcome.Wrong);
        }

        void Finish(QuizOutcome outcome)
        {
            var result = new QuizResult
            {
                questionId = current.id,
                level = current.level,
                timeLimit = timeLimit,
                remaining = timer.RemainingAtSettle,
                outcome = outcome,
            };
            View.Hide();
            onResolved?.Invoke(result); // phát thưởng một lần, trong lúc thế giới còn đứng yên
            onResolved = null;
            Answered?.Invoke(result);
            freeze.Resume(() => IsOpen = false);
        }
    }
}
