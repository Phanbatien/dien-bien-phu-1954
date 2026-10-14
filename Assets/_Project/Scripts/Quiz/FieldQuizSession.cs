using System;
using System.Collections.Generic;
using DBP.Content;
using DBP.Core;
using DBP.Missions;
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
            if (View == null) View = TryGetComponent<IQuizView>(out var view) ? view : gameObject.AddComponent<DebugQuizView>();
        }

        /// Nạp ngân hàng câu theo màn và seed của lượt (T11, T17). Câu nháp chỉ được dùng trong Editor/bản Development.
        void Start()
        {
            if (Questions != null) return;
            var errors = new List<string>();
            var all = QuestionBank.LoadAll(errors);
            foreach (var e in errors) Debug.LogError($"[Quiz] {e}");
            var mission = MissionController.Current;
            Questions = new QuestionSelector(all,
                mission ? mission.MissionId : "",
                mission ? mission.Attempt.Seed : Environment.TickCount,
                Debug.isDebugBuild);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// Trả false nếu không mở được: đang có câu khác, hoặc thiếu câu (khi đó đối tượng phải tự khóa, BR-31).
        public bool TryOpen(int level, float limitSeconds, Action<QuizResult> resolved) =>
            TryOpen(level, level, limitSeconds, resolved);

        public bool TryOpen(int minLevel, int maxLevel, float limitSeconds, Action<QuizResult> resolved)
        {
            if (IsOpen || !freeze) return false;
            var question = Questions?.Next(minLevel, maxLevel, QuizForm.Field);
            if (question == null)
            {
                Debug.LogWarning($"[Quiz] Thiếu câu cấp {minLevel}–{maxLevel} cho hình thức ngoài trận (BR-31).");
                QuestionShortage?.Invoke(minLevel);
                return false;
            }

            IsOpen = true;
            current = question;
            timeLimit = limitSeconds;
            onResolved = resolved;
            freeze.Freeze();
            timer = new QuizTimer(limitSeconds, Now); // bắt đầu cùng lúc câu và lựa chọn hiện ra
            View.Show(question, limitSeconds, OnAnswer);
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
