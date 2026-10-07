using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Quiz
{
    // ponytail: giao diện IMGUI tạm để thử vòng câu hỏi. T18 (Thiên Trí) làm UI thật rồi gắn vào cùng GameObject.
    // IMGUI vẫn chạy khi Time.timeScale = 0. Chọn bằng chuột hoặc phím 1–4.
    public class DebugQuizView : MonoBehaviour, IQuizView
    {
        Question question;
        Action<string> onAnswer;
        float remaining, limit;
        GUIStyle stem, button;

        public void Show(Question q, float timeLimit, Action<string> answer)
        {
            question = q;
            onAnswer = answer;
            limit = remaining = timeLimit;
        }

        public void SetRemaining(float seconds) => remaining = seconds;

        public void Hide()
        {
            question = null;
            onAnswer = null;
        }

        void Update()
        {
            if (question == null || Keyboard.current == null) return;
            var keys = new[] { Keyboard.current.digit1Key, Keyboard.current.digit2Key, Keyboard.current.digit3Key, Keyboard.current.digit4Key };
            for (int i = 0; i < question.options.Length && i < keys.Length; i++)
                if (keys[i].wasPressedThisFrame) { onAnswer?.Invoke(question.options[i].id); return; }
        }

        void OnGUI()
        {
            if (question == null) return;
            stem ??= new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, alignment = TextAnchor.UpperCenter };
            button ??= new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft };

            float w = Mathf.Min(720, Screen.width - 32), h = 360;
            var box = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            GUI.Box(box, "");
            GUI.Box(box, "");
            GUILayout.BeginArea(new Rect(box.x + 16, box.y + 12, box.width - 32, box.height - 24));
            GUILayout.Label($"Cấp {question.level} · còn {remaining:0.0}s", stem);
            GUILayout.HorizontalSlider(remaining, 0, limit);
            GUILayout.Space(8);
            GUILayout.Label(question.stem, stem);
            GUILayout.Space(8);
            for (int i = 0; i < question.options.Length; i++)
            {
                var option = question.options[i];
                if (GUILayout.Button($"{i + 1}. {option.text}", button, GUILayout.Height(40)))
                    onAnswer?.Invoke(option.id);
            }
            GUILayout.EndArea();
        }
    }
}
