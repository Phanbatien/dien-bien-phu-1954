using System;
using UnityEngine;

namespace DBP.Quiz
{
    /// T18: giao diện câu hỏi ngoài trận (IMGUI), bản "thật" của IQuizView.
    /// Đồng hồ KHÔNG nằm ở đây: QuizTimer (QuizTypes.cs) đếm bằng thời gian thực và bộ điều khiển
    /// gọi SetRemaining mỗi khung hình; giao diện chỉ vẽ lại. Việc mở/khóa chuột do WorldFreeze lo.
    /// Chọn đáp án bằng chuột hoặc phím 1–4. Sau lần chọn đầu tiên, các nút bị khóa; bộ điều khiển gọi Hide().
    public class QuizPanel : MonoBehaviour, IQuizView
    {
        [Header("Giao diện")]
        [Tooltip("Gán font có dấu tiếng Việt nếu chữ bị mất dấu. Để trống = font mặc định.")]
        public Font font;
        public int bodyFontSize = 34;
        public int answerFontSize = 30;
        public bool shuffleOptions = true;
        public Color panelColor = new Color(0.10f, 0.12f, 0.09f, 0.96f);
        public Color barColor = new Color(0.85f, 0.75f, 0.30f, 1f);
        public Color barWarnColor = new Color(0.85f, 0.25f, 0.20f, 1f);

        private bool _visible;
        private bool _answered;
        private Question _question;
        private QuestionOption[] _options;
        private Action<string> _onAnswer;
        private float _limit = 1f;
        private float _remaining = 1f;

        private GUIStyle _bodyStyle;
        private GUIStyle _answerStyle;
        private GUIStyle _timerStyle;
        private GUIStyle _metaStyle;

        public bool IsVisible { get { return _visible; } }

        public void Show(Question question, float timeLimit, Action<string> onAnswer)
        {
            if (question == null || question.options == null || question.options.Length == 0)
            {
                Debug.LogError("QuizPanel: câu hỏi rỗng hoặc không có đáp án.");
                return;
            }

            _question = question;
            _options = (QuestionOption[])question.options.Clone();
            if (shuffleOptions)
            {
                for (int i = _options.Length - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    QuestionOption tmp = _options[i]; _options[i] = _options[j]; _options[j] = tmp;
                }
            }

            _limit = Mathf.Max(0.01f, timeLimit);
            _remaining = _limit;
            _onAnswer = onAnswer;
            _answered = false;
            _visible = true;
        }

        public void SetRemaining(float seconds)
        {
            _remaining = Mathf.Clamp(seconds, 0f, _limit);
        }

        public void Hide()
        {
            _visible = false;
            _question = null;
            _options = null;
            _onAnswer = null;
        }

        private void EnsureStyles()
        {
            if (_bodyStyle != null) return;

            _bodyStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = bodyFontSize, fontStyle = FontStyle.Bold };
            _answerStyle = DBP.UI.UITheme.ImguiButton(new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontSize = answerFontSize, padding = new RectOffset(28, 28, 14, 14) }, DBP.UI.UITheme.Wood);
            _bodyStyle.normal.textColor = DBP.UI.UITheme.Ink;
            _timerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = 28, fontStyle = FontStyle.Bold };
            _metaStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 22, fontStyle = FontStyle.Bold };
            _metaStyle.normal.textColor = DBP.UI.UITheme.Heading;
            _timerStyle.normal.textColor = DBP.UI.UITheme.Heading;

            // Phông chung của game (Be Vietnam Pro); gán font trong Inspector thì dùng font đó
            _bodyStyle.font = DBP.UI.UITheme.SemiBold; _answerStyle.font = DBP.UI.UITheme.SemiBold;
            _timerStyle.font = DBP.UI.UITheme.Bold; _metaStyle.font = DBP.UI.UITheme.Bold;
            _bodyStyle.fontStyle = _answerStyle.fontStyle = _timerStyle.fontStyle = _metaStyle.fontStyle = FontStyle.Normal;
            if (font != null)
            {
                _bodyStyle.font = font; _answerStyle.font = font; _timerStyle.font = font; _metaStyle.font = font;
            }
        }

        private void OnGUI()
        {
            if (!_visible || _question == null) return;
            EnsureStyles();

            // Thiết kế theo màn hình ảo cao 1080 để mọi độ phân giải đều không bị cắt đáp án.
            float s = Screen.height / 1080f;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            float vw = Screen.width / s;
            float vh = 1080f;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);

            float panelW = Mathf.Min(1200f, vw - 80f);
            float inner = panelW - 80f;
            float bodyH = _bodyStyle.CalcHeight(new GUIContent(_question.stem), inner);

            float[] answerH = new float[_options.Length];
            float answersTotal = 0f;
            for (int i = 0; i < _options.Length; i++)
            {
                answerH[i] = Mathf.Max(70f, _answerStyle.CalcHeight(new GUIContent((i + 1) + ".  " + _options[i].text), inner));
                answersTotal += answerH[i] + 16f;
            }

            float panelH = Mathf.Min(40f + 50f + 24f + bodyH + 30f + answersTotal + 40f, vh - 40f);
            float px = (vw - panelW) * 0.5f;
            float py = Mathf.Max(20f, (vh - panelH) * 0.5f);

            DBP.UI.UITheme.ImguiPanel(new Rect(px, py, panelW, panelH)); // khung gỗ + giấy da

            float frac = Mathf.Clamp01(_remaining / _limit);
            float x = px + 40f;
            float y = py + 40f;

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, inner * 0.5f, 50f), "Cấp " + _question.level, _metaStyle);
            GUI.Label(new Rect(x + inner * 0.5f, y, inner * 0.5f, 50f), "Còn " + Mathf.CeilToInt(_remaining) + " giây", _timerStyle);
            y += 50f;

            GUI.color = new Color(0.25f, 0.16f, 0.08f, 0.55f);
            GUI.DrawTexture(new Rect(x, y, inner, 14f), Texture2D.whiteTexture);
            GUI.color = frac > 0.25f ? barColor : barWarnColor;
            GUI.DrawTexture(new Rect(x, y, inner * frac, 14f), Texture2D.whiteTexture);
            y += 24f + 10f;

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, inner, bodyH + 4f), _question.stem, _bodyStyle);
            y += bodyH + 30f;

            GUI.enabled = !_answered;
            string chosen = null;
            for (int i = 0; i < _options.Length; i++)
            {
                if (GUI.Button(new Rect(x, y, inner, answerH[i]), (i + 1) + ".  " + _options[i].text, _answerStyle))
                    chosen = _options[i].id;
                y += answerH[i] + 16f;
            }

            Event e = Event.current;
            if (!_answered && chosen == null && e != null && e.type == EventType.KeyDown)
            {
                int idx = -1;
                if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha4) idx = e.keyCode - KeyCode.Alpha1;
                else if (e.keyCode >= KeyCode.Keypad1 && e.keyCode <= KeyCode.Keypad4) idx = e.keyCode - KeyCode.Keypad1;
                if (idx >= 0 && idx < _options.Length)
                {
                    chosen = _options[idx].id;
                    e.Use();
                }
            }

            GUI.enabled = oldEnabled;
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;

            if (chosen != null && !_answered)
            {
                _answered = true;
                Action<string> callback = _onAnswer;
                if (callback != null) callback(chosen);
            }
        }
    }
}
