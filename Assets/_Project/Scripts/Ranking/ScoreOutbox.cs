using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DBP.Ranking
{
    /// T62 (điểm cộng): hàng chờ gửi thành tích online. Ghi ra đĩa nên còn nguyên sau khi tắt game;
    /// thử lại sau 5 s, 15 s, 60 s rồi mỗi 5 phút; mã lượt chơi (attemptId) là khóa chống gửi trùng,
    /// server dùng khóa này để bỏ qua bản gửi lại và tự chấm lại từ số liệu thô (BR-26, BR-39).
    public sealed class ScoreOutbox
    {
        public static readonly int[] RetrySeconds = { 5, 15, 60, 300 };

        [Serializable] public sealed class Entry
        {
            public string id;        // attemptId: khóa idempotency
            public string payload;   // RankedResult dạng JSON, gửi nguyên văn
            public int attempts;
            public long nextAtMs;
        }
        [Serializable] sealed class FileData { public List<Entry> entries = new List<Entry>(); }

        readonly string path;
        FileData data;

        public ScoreOutbox(string path)
        {
            this.path = path;
            data = new FileData();
            if (!File.Exists(path)) return;
            try { data = JsonUtility.FromJson<FileData>(File.ReadAllText(path)) ?? new FileData(); }
            catch (ArgumentException) { File.Copy(path, path + ".hong", true); } // giữ bản hỏng để đối chiếu, bắt đầu hàng chờ mới
            data.entries ??= new List<Entry>();
        }

        public IReadOnlyList<Entry> Entries => data.entries;

        /// Thêm một lượt thắng hợp lệ của người đã đồng ý công khai. Lượt đã có trong hàng chờ thì bỏ qua.
        public bool Enqueue(RankedResult result, long nowMs)
        {
            if (result == null || string.IsNullOrEmpty(result.attemptId) || data.entries.Any(e => e.id == result.attemptId)) return false;
            data.entries.Add(new Entry { id = result.attemptId, payload = JsonUtility.ToJson(result), nextAtMs = nowMs });
            Save();
            return true;
        }

        public Entry NextDue(long nowMs) => data.entries.Where(e => e.nextAtMs <= nowMs).OrderBy(e => e.nextAtMs).FirstOrDefault();

        /// Server đã nhận (kể cả trả lời "đã có"): xóa khỏi hàng chờ.
        public void MarkSent(string id)
        {
            if (data.entries.RemoveAll(e => e.id == id) > 0) Save();
        }

        public void MarkFailed(string id, long nowMs)
        {
            var entry = data.entries.FirstOrDefault(e => e.id == id);
            if (entry == null) return;
            entry.nextAtMs = nowMs + RetrySeconds[Math.Min(entry.attempts, RetrySeconds.Length - 1)] * 1000L;
            entry.attempts++;
            Save();
        }

        void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
}
