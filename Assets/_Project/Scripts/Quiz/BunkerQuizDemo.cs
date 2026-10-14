using System.Collections.Generic;
using DBP.Core;
using UnityEngine;

namespace DBP.Quiz
{
    /// Script THỬ cho BunkerQuizPanel: chạy một lượt vài câu hỏi trong hầm (không đồng hồ).
    /// Gắn vào GameObject rỗng, kéo JSON vào Question Files, bấm Play rồi bấm "Vào hầm".
    /// Xóa sau khi thử xong. Bộ điều khiển thật sẽ do team nối.
    public class BunkerQuizDemo : MonoBehaviour
    {
        public TextAsset[] questionFiles;
        public string missionId = "M1";
        public bool includeDrafts = true;
        [Range(1, 5)] public int questionCount = 4;
        [Tooltip("Cấp của từng câu theo thứ tự (GDD: hầm dùng cấp 1–3). Hết danh sách thì lặp lại.")]
        public int[] levels = { 1, 2, 2, 3 };
        [Tooltip("Bật: bấm Vào hầm thì thế giới đứng yên (dùng WorldFreeze của team), xong thì chạy lại.")]
        public bool freezeWorld = true;
        [Tooltip("Hiện chuột để bấm nút \"Vào hầm\". Trong lúc đang hỏi, nếu có WorldFreeze thì nó tự lo chuột.")]
        public bool unlockCursor = true;

        private BunkerQuizPanel _panel;
        private JsonQuestionProvider _provider;
        private int _index;
        private int _correct;
        private Question _current;
        private bool _running;
        private string _result = "";

        private void Awake()
        {
            _panel = GetComponent<BunkerQuizPanel>();
            if (_panel == null) _panel = gameObject.AddComponent<BunkerQuizPanel>();
            _panel.Continued += OnContinued;
        }

        private void OnDestroy()
        {
            if (_panel != null) _panel.Continued -= OnContinued;
        }

        private void Update()
        {
            bool freezeHandlesCursor = freezeWorld && WorldFreeze.Instance != null;
            if (unlockCursor && !(freezeHandlesCursor && _running))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        private void OnGUI()
        {
            if (_running) return;

            if (GUI.Button(new Rect(Screen.width - 260, 20, 240, 50), "Vào hầm (" + questionCount + " câu)")) StartRun();
            if (_result.Length > 0) GUI.Label(new Rect(Screen.width - 520, 80, 500, 30), _result);
        }

        private void StartRun()
        {
            if (WorldFreeze.IsFrozen) return;   // đang chuyển cảnh 0,5 giây, chờ xong
            _result = "";
            if (_provider == null && questionFiles != null && questionFiles.Length > 0)
            {
                var texts = new List<string>();
                foreach (TextAsset f in questionFiles) if (f != null) texts.Add(f.text);
                _provider = JsonQuestionProvider.FromJson(texts, missionId, new QuestionSelectorOptions { IncludeDrafts = includeDrafts });
            }
            if (_provider == null)
            {
                _result = "Chưa gán file câu hỏi vào Question Files.";
                return;
            }

            _provider.MissionId = missionId;
            _provider.ResetSession();
            _index = 0;
            _correct = 0;
            _running = true;

            if (freezeWorld)
            {
                if (WorldFreeze.Instance != null) WorldFreeze.Instance.Freeze();
                else Debug.LogWarning("[BunkerQuizDemo] Không có WorldFreeze trong cảnh, nên thế giới KHÔNG đứng yên.");
            }
            ShowNext();
        }

        private void ShowNext()
        {
            // Bỏ qua câu thiếu (hết câu cấp đó) thay vì kẹt.
            while (_index < questionCount)
            {
                int level = (levels != null && levels.Length > 0) ? levels[_index % levels.Length] : 1;
                _current = _provider.Next(level, QuizForm.Bunker);
                if (_current != null)
                {
                    _panel.SetProgress(_index + 1, questionCount);
                    _panel.Show(_current, 0f, OnAnswer);
                    return;
                }
                Debug.LogWarning("[BunkerQuizDemo] Không còn câu hầm cấp " + level + ", bỏ qua.");
                _index++;
            }
            Finish();
        }

        private void OnAnswer(string answerId)
        {
            if (_current != null && _current.IsCorrectAnswer(answerId)) _correct++;
        }

        private void OnContinued()
        {
            _index++;
            if (_index >= questionCount) Finish();
            else ShowNext();
        }

        private void Finish()
        {
            _running = false;
            _panel.Hide();
            if (freezeWorld && WorldFreeze.Instance != null) WorldFreeze.Instance.Resume();
            _result = "Xong lượt hầm: đúng " + _correct + "/" + questionCount;
            Debug.Log("[BunkerQuizDemo] " + _result);
        }
    }
}
