using System;
using System.Collections;
using System.IO;
using System.Text;
using DBP.Content;
using DBP.Upgrades;
using UnityEngine;
using UnityEngine.Networking;

namespace DBP.Ranking
{
    /// Gửi dần hàng chờ T62 lên server ghi trong content/online.json ({"endpoint": "https://..."}).
    /// Chưa cấu hình endpoint thì không gửi gì, hàng chờ vẫn được giữ; game chạy ngoại tuyến bình thường.
    public sealed class ScoreOutboxSender : MonoBehaviour
    {
        [Serializable] sealed class OnlineConfig { public string endpoint; }

        public static string OutboxPath => Path.ChangeExtension(ProfileService.Repository.StoragePath, "outbox.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!ContentLoader.TryLoad<OnlineConfig>("online.json", out var config, out _) || string.IsNullOrWhiteSpace(config?.endpoint)) return;
            var go = new GameObject("ScoreOutboxSender");
            DontDestroyOnLoad(go);
            go.AddComponent<ScoreOutboxSender>().endpoint = config.endpoint;
        }

        string endpoint;
        bool sending;

        static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// Gọi khi lưu xong lượt thắng: chỉ xếp vào hàng chờ khi người chơi đã đồng ý công khai.
        public static void EnqueueIfConsented(RankedResult result)
        {
            if (result == null || !ProfileService.HasProfile || string.IsNullOrEmpty(ProfileService.Data.consentAt)) return;
            try { new ScoreOutbox(OutboxPath).Enqueue(result, NowMs); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Debug.LogWarning("[Outbox] " + e.Message); }
        }

        void Update()
        {
            if (sending || Time.frameCount % 30 != 0 || !ProfileService.HasProfile) return;
            var outbox = new ScoreOutbox(OutboxPath);
            var entry = outbox.NextDue(NowMs);
            if (entry != null) StartCoroutine(Send(outbox, entry));
        }

        IEnumerator Send(ScoreOutbox outbox, ScoreOutbox.Entry entry)
        {
            sending = true;
            using (var request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(entry.payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Idempotency-Key", entry.id);
                request.timeout = 10;
                yield return request.SendWebRequest();
                // 2xx: đã nhận; 409: server đã có lượt này (gửi lại không nhân đôi)
                bool done = request.result == UnityWebRequest.Result.Success || request.responseCode == 409;
                if (done) outbox.MarkSent(entry.id); else outbox.MarkFailed(entry.id, NowMs);
            }
            sending = false;
        }
    }
}
