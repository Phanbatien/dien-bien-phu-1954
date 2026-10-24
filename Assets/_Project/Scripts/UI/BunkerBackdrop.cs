using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.UI
{
    /// Hầm chữ A (T09/T25) hiện phía sau màn hình chuẩn bị: nạp cộng thêm khi vào, gỡ khi rời.
    /// Scene không có trong Build Settings thì bỏ qua, giao diện vẫn dùng được.
    public static class BunkerBackdrop
    {
        public const string SceneName = "Bunker_HamChuA";

        public static bool Available => Application.CanStreamedLevelBeLoaded(SceneName);
        public static bool Loaded => SceneManager.GetSceneByName(SceneName).isLoaded;

        public static void Show()
        {
            wanted = true;
            if (Loaded || pending || !Application.CanStreamedLevelBeLoaded(SceneName)) return;
            pending = true;
            var op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            if (op == null) { pending = false; return; }
            op.completed += _ => { pending = false; if (!wanted) Hide(); };
        }

        public static void Hide()
        {
            wanted = false;
            if (Loaded) SceneManager.UnloadSceneAsync(SceneName);
        }

        static bool pending, wanted;
    }
}
