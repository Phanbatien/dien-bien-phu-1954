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
            op.completed += _ =>
            {
                pending = false;
                if (!wanted) { Hide(); return; }
                // Camera trong hầm đưa nhẹ qua lại cho cảnh sống động
                foreach (var root in SceneManager.GetSceneByName(SceneName).GetRootGameObjects())
                    foreach (var cam in root.GetComponentsInChildren<Camera>())
                        if (!cam.GetComponent<DBP.Core.BunkerCameraSway>()) cam.gameObject.AddComponent<DBP.Core.BunkerCameraSway>();
            };
        }

        public static void Hide()
        {
            wanted = false;
            if (Loaded) SceneManager.UnloadSceneAsync(SceneName);
        }

        static bool pending, wanted;
    }
}
