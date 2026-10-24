using System;
using UnityEngine;

namespace DBP.Quiz
{
    /// T25: giao diện câu hỏi TRONG HẦM (IMGUI). Không có đồng hồ.
    /// Luồng: hiện câu -> người chơi chọn -> tô xanh/đỏ + hiện phần giải thích (fact) -> bấm "Tiếp tục" (hoặc Enter/Space).
    /// Là một IQuizView, nên bộ điều khiển dùng giống QuizPanel; thời gian truyền vào bị bỏ qua.
    public class BunkerQuizPanel : MonoBehaviour, IQuizView
    {
        [Header("Chữ")]
        [Tooltip("Font có dấu tiếng Việt. Để trống = font mặc định (có thể mất dấu).")]
        public Font font;
        public string title = "HỌP TÁC CHIẾN";
        public int titleFontSize = 30;
        public int bodyFontSize = 34;
        public int answerFontSize = 30;
        public int factFontSize = 26;

        [Header("Hình (tùy chọn, để trống = dùng màu trơn)")]
        [Tooltip("Ảnh nền toàn màn hình, ví dụ bàn gỗ trong hầm.")]
        public Texture2D backgroundTexture;
        [Tooltip("Tờ giấy kraft/bản đồ đặt giữa màn hình.")]
        public Texture2D paperTexture;
        [Tooltip("Con dấu mờ ở góc tờ giấy (ví dụ TUYỆT MẬT).")]
        public Texture2D stampTexture;

        [Header("Màu")]
        public Color dimColor = new Color(0.05f, 0.04f, 0.03f, 0.80f);
        public Color paperColor = new Color(0.86f, 0.80f, 0.64f, 1f);
        public Color inkColor = new Color(0.16f, 0.12f, 0.08f, 1f);
        public Color accentColor = new Color(0.55f, 0.36f, 0.10f, 1f);
        public Color correctColor = new Color(0.30f, 0.55f, 0.28f, 1f);
        public Color wrongColor = new Color(0.70f, 0.25f, 0.20f, 1f);

        [Header("Hành vi")]
        public bool shuffleOptions = true;
        [Tooltip("Bấm Tiếp tục xong tự ẩn bảng. Tắt nếu bộ điều khiển tự gọi Hide().")]
        public bool autoHideOnContinue = true;

        /// Bắn ra khi người chơi bấm Tiếp tục (sau khi đã xem đáp án).
        public event Action Continued;

        private bool _visible;
        private bool _answered;
        private string _chosenId;
        private Question _question;
        private QuestionOption[] _options;
        private Action<string> _onAnswer;
        private int _current;
        private int _total;

        private GUIStyle _correctStyle, _wrongStyle;
        private GUIStyle _titleStyle, _bodyStyle, _answerStyle, _factStyle, _buttonStyle, _progressStyle;

        public bool IsVisible { get { return _visible; } }

        /// Ví dụ SetProgress(2, 4) hiện "Câu 2/4". Gọi trước Show. total = 0 thì ẩn dòng này.
        public void SetProgress(int current, int total)
        {
            _current = current;
            _total = total;
        }

        public void Show(Question question, float timeLimit, Action<string> onAnswer)
        {
            if (question == null || question.options == null || question.options.Length == 0)
            {
                Debug.LogError("BunkerQuizPanel: câu hỏi rỗng hoặc không có đáp án.");
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

            _onAnswer = onAnswer;
            _answered = false;
            _chosenId = null;
            _visible = true;
        }

        /// Trong hầm không có đồng hồ: bỏ qua.
        public void SetRemaining(float seconds) { }

        public void Hide()
        {
            _visible = false;
            _question = null;
            _options = null;
            _onAnswer = null;
            _chosenId = null;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = titleFontSize, fontStyle = FontStyle.Bold };
            _progressStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontSize = titleFontSize - 4 };
            _bodyStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = bodyFontSize, fontStyle = FontStyle.Bold };
            _answerStyle = DBP.UI.UITheme.ImguiButton(new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontSize = answerFontSize, padding = new RectOffset(28, 28, 14, 14) }, DBP.UI.UITheme.Wood);
            _factStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = factFontSize, fontStyle = FontStyle.Italic };
            _correctStyle = DBP.UI.UITheme.ImguiButton(_answerStyle, DBP.UI.UITheme.Green);
            _wrongStyle = DBP.UI.UITheme.ImguiButton(_answerStyle, DBP.UI.UITheme.Red);
            _buttonStyle = DBP.UI.UITheme.ImguiButton(new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = answerFontSize, fontStyle = FontStyle.Bold }, DBP.UI.UITheme.Green);

            if (font != null)
            {
                _titleStyle.font = font; _progressStyle.font = font; _bodyStyle.font = font;
                _answerStyle.font = font; _factStyle.font = font; _buttonStyle.font = font;
            }
        }

        private void OnGUI()
        {
            if (!_visible || _question == null) return;
            EnsureStyles();

            float s = Screen.height / 1080f;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Color oldBg = GUI.backgroundColor;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            float vw = Screen.width / s;
            float vh = 1080f;

            // Nền: ảnh (nếu có) rồi phủ tối nhẹ.
            if (backgroundTexture != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(0, 0, vw, vh), backgroundTexture, ScaleMode.ScaleAndCrop);
            }
            GUI.color = dimColor;
            GUI.DrawTexture(new Rect(0, 0, vw, vh), Texture2D.whiteTexture);

            // Chiều cao từng phần.
            float panelW = Mathf.Min(1200f, vw - 80f);
            float inner = panelW - 100f;
            float bodyH = _bodyStyle.CalcHeight(new GUIContent(_question.stem), inner);

            float[] answerH = new float[_options.Length];
            float answersTotal = 0f;
            for (int i = 0; i < _options.Length; i++)
            {
                answerH[i] = Mathf.Max(70f, _answerStyle.CalcHeight(new GUIContent((i + 1) + ".  " + _options[i].text), inner));
                answersTotal += answerH[i] + 14f;
            }

            string fact = string.IsNullOrEmpty(_question.fact) ? "" : _question.fact;
            float factH = (_answered && fact.Length > 0) ? _factStyle.CalcHeight(new GUIContent(fact), inner - 20f) + 24f : 0f;
            float footerH = _answered ? 80f : 0f;

            float panelH = Mathf.Min(50f + 60f + 20f + bodyH + 24f + answersTotal + factH + footerH + 40f, vh - 40f);
            float px = (vw - panelW) * 0.5f;
            float py = Mathf.Max(20f, (vh - panelH) * 0.5f);

            // Tờ giấy.
            if (paperTexture != null)
            {
                GUI.color = paperColor;
                GUI.DrawTexture(new Rect(px, py, panelW, panelH), paperTexture, ScaleMode.StretchToFill);
            }
            else DBP.UI.UITheme.ImguiPanel(new Rect(px, py, panelW, panelH)); // khung gỗ + giấy da

            // Con dấu mờ ở góc phải dưới.
            if (stampTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(new Rect(px + panelW - 260f, py + panelH - 200f, 220f, 160f), stampTexture, ScaleMode.ScaleToFit);
            }

            // Viền mỏng.
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(px, py, panelW, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(px, py + panelH - 4f, panelW, 4f), Texture2D.whiteTexture);

            float x = px + 50f;
            float y = py + 40f;

            // Tiêu đề + tiến độ.
            GUI.color = accentColor;
            GUI.Label(new Rect(x, y, inner * 0.6f, 50f), title, _titleStyle);
            if (_total > 0) GUI.Label(new Rect(x + inner * 0.6f, y, inner * 0.4f, 50f), "Câu " + _current + "/" + _total, _progressStyle);
            y += 56f;
            GUI.color = inkColor;
            GUI.DrawTexture(new Rect(x, y, inner, 2f), Texture2D.whiteTexture);
            y += 20f;

            // Câu hỏi.
            GUI.color = inkColor;
            GUI.Label(new Rect(x, y, inner, bodyH + 4f), _question.stem, _bodyStyle);
            y += bodyH + 24f;

            // Đáp án.
            string chosen = null;
            for (int i = 0; i < _options.Length; i++)
            {
                QuestionOption opt = _options[i];
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                GUI.enabled = !_answered;

                var style = _answerStyle;
                if (_answered)
                {
                    bool isCorrect = _question.IsCorrectAnswer(opt.id);
                    bool isChosen = opt.id == _chosenId;
                    if (isCorrect) style = _correctStyle;        // đáp án đúng: xanh
                    else if (isChosen) style = _wrongStyle;      // đã chọn sai: đỏ
                    GUI.enabled = true; // vẫn vẽ rõ chữ, chỉ không nhận bấm (xử lý bên dưới)
                }

                bool clicked = GUI.Button(new Rect(x, y, inner, answerH[i]), (i + 1) + ".  " + opt.text, style);
                if (clicked && !_answered) chosen = opt.id;
                y += answerH[i] + 14f;
            }
            GUI.backgroundColor = oldBg;
            GUI.enabled = true;

            // Phím 1–4 để chọn.
            Event e = Event.current;
            if (!_answered && chosen == null && e != null && e.type == EventType.KeyDown)
            {
                int idx = -1;
                if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha4) idx = e.keyCode - KeyCode.Alpha1;
                else if (e.keyCode >= KeyCode.Keypad1 && e.keyCode <= KeyCode.Keypad4) idx = e.keyCode - KeyCode.Keypad1;
                if (idx >= 0 && idx < _options.Length) { chosen = _options[idx].id; e.Use(); }
            }

            // Phần giải thích + nút Tiếp tục (chỉ sau khi trả lời).
            bool doContinue = false;
            if (_answered)
            {
                if (fact.Length > 0)
                {
                    GUI.color = inkColor;
                    GUI.Label(new Rect(x + 10f, y + 4f, inner - 20f, factH), fact, _factStyle);
                    y += factH;
                }

                GUI.color = Color.white;
                if (GUI.Button(new Rect(x + inner - 260f, y + 10f, 260f, 60f), "Tiếp tục  ▶", _buttonStyle)) doContinue = true;

                if (e != null && e.type == EventType.KeyDown &&
                    (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space))
                {
                    doContinue = true;
                    e.Use();
                }
            }

            GUI.enabled = oldEnabled;
            GUI.color = oldColor;
            GUI.backgroundColor = oldBg;
            GUI.matrix = oldMatrix;

            if (chosen != null && !_answered)
            {
                _answered = true;
                _chosenId = chosen;
                Action<string> cb = _onAnswer;
                if (cb != null) cb(chosen);
            }
            else if (doContinue)
            {
                if (autoHideOnContinue) Hide();
                Action handler = Continued;
                if (handler != null) handler();
            }
        }
    }
}
