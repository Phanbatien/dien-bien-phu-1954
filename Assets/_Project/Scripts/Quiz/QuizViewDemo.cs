using UnityEngine;

namespace DBP.Quiz
{
    /// Script THỬ cho QuizPanel + JsonQuestionProvider + QuizTimer, không cần hòm hay WorldFreeze.
    /// Gắn vào một GameObject rỗng, bấm Play, bấm nút "Thử câu hỏi" ở góc trên bên trái.
    /// Xóa khi đã thử xong. Không dùng cho bản phát hành.
    public class QuizViewDemo : MonoBehaviour
    {
        [Header("Thử")]
        [Tooltip("Số giây của câu. Đổi lần lượt 12 / 15 / 18 / 20 để đo.")]
        public float seconds = 12f;
        [Range(1, 5)] public int level = 2;
        [Tooltip("Giả lập thế giới đứng yên (timeScale = 0).")]
        public bool freezeWorld = true;
        public bool unlockCursor = true;

        [Header("Câu hỏi (để trống = dùng 1 câu mẫu)")]
        public TextAsset[] questionFiles;
        public string missionId = "M1";
        [Tooltip("Bật vì câu mới soạn đều là draft. Bản thật phải TẮT.")]
        public bool includeDrafts = true;

        private QuizPanel _panel;
        private JsonQuestionProvider _provider;
        private QuizTimer _timer;
        private Question _current;
        private bool _active;
        private string _last = "";

        private void Awake()
        {
            _panel = GetComponent<QuizPanel>();
            if (_panel == null) _panel = gameObject.AddComponent<QuizPanel>();
        }

        private void Update()
        {
            if (unlockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (!_active) return;

            double now = Time.realtimeSinceStartupAsDouble;
            _panel.SetRemaining(_timer.Remaining(now));
            if (_timer.CheckTimeout(now)) Finish(QuizOutcome.Timeout);
        }

        private void OnGUI()
        {
            if (_active) return;

            if (GUI.Button(new Rect(20, 20, 240, 50), "Thử câu hỏi (" + seconds + " giây)")) StartQuestion();
            if (_last.Length > 0) GUI.Label(new Rect(20, 80, 900, 30), _last);
        }

        private void StartQuestion()
        {
            _current = PickQuestion();
            _timer = new QuizTimer(seconds, Time.realtimeSinceStartupAsDouble);
            _active = true;
            if (freezeWorld) Time.timeScale = 0f;
            _panel.Show(_current, seconds, OnAnswer);
        }

        private void OnAnswer(string answerId)
        {
            if (!_active) return;
            double now = Time.realtimeSinceStartupAsDouble;

            if (_timer.TryAnswer(now))
                Finish(_current.IsCorrectAnswer(answerId) ? QuizOutcome.Correct : QuizOutcome.Wrong);
            else if (_timer.CheckTimeout(now))
                Finish(QuizOutcome.Timeout);
        }

        private void Finish(QuizOutcome outcome)
        {
            _active = false;
            _panel.Hide();
            if (freezeWorld) Time.timeScale = 1f;

            string name = outcome == QuizOutcome.Correct ? "ĐÚNG" : outcome == QuizOutcome.Wrong ? "SAI" : "HẾT GIỜ (tính là sai)";
            _last = name + "  |  " + _current.id + "  |  còn " + _timer.RemainingAtSettle.ToString("0.0") + "/" + seconds + " giây";
            Debug.Log("[QuizViewDemo] " + _last);
        }

        private Question PickQuestion()
        {
            if (_provider == null && questionFiles != null && questionFiles.Length > 0)
            {
                var texts = new System.Collections.Generic.List<string>();
                foreach (TextAsset file in questionFiles) if (file != null) texts.Add(file.text);
                _provider = JsonQuestionProvider.FromJson(texts, missionId, new QuestionSelectorOptions { IncludeDrafts = includeDrafts });
            }

            if (_provider != null)
            {
                _provider.MissionId = missionId;
                Question q = _provider.Next(level, QuizForm.Field);
                if (q == null)
                {
                    _provider.ResetSession();           // hết câu khi thử nhiều lần: bắt đầu lượt mới
                    q = _provider.Next(level, QuizForm.Field);
                }
                if (q != null) return q;
            }
            return Sample();
        }

        private static Question Sample()
        {
            return new Question
            {
                id = "SAMPLE-001", missionId = "M1", level = 2, status = "approved",
                forms = new[] { "field" },
                stem = "Phương châm tác chiến mới của chiến dịch Điện Biên Phủ là gì?",
                options = new[]
                {
                    new QuestionOption { id = "a", text = "Đánh nhanh, giải quyết nhanh" },
                    new QuestionOption { id = "b", text = "Đánh chắc, tiến chắc" },
                    new QuestionOption { id = "c", text = "Vườn không nhà trống" },
                    new QuestionOption { id = "d", text = "Đáp án thứ tư dài hơn một chút để thử xuống dòng khi chữ quá dài so với khung" }
                },
                correct = "b",
                fact = "Ngày 26/1/1954 ta đổi sang đánh chắc, tiến chắc."
            };
        }
    }
}
