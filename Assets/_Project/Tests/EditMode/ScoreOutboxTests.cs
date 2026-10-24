using System;
using System.IO;
using DBP.Ranking;
using DBP.Upgrades;
using NUnit.Framework;
using UnityEngine;

namespace DBP.Tests
{
    /// T62: hàng chờ còn sau khởi động lại, thử lại 5 s / 15 s / 60 s / 5 phút, không nhân đôi.
    public class ScoreOutboxTests
    {
        string path;

        [SetUp] public void Setup() => path = Path.Combine(Application.temporaryCachePath, "outbox-" + Guid.NewGuid().ToString("N"), "outbox.json");
        [TearDown] public void Cleanup() { if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path), true); }

        static RankedResult Result(string id) => new RankedResult { attemptId = id, profileId = "p", kills = 7 };

        [Test] public void Enqueue_SurvivesRestart_AndIgnoresDuplicates()
        {
            var outbox = new ScoreOutbox(path);
            Assert.IsTrue(outbox.Enqueue(Result("a1"), 0));
            Assert.IsFalse(outbox.Enqueue(Result("a1"), 0), "Cùng lượt chơi không được xếp hai lần");
            var reopened = new ScoreOutbox(path);
            Assert.AreEqual(1, reopened.Entries.Count);
            StringAssert.Contains("\"kills\":7", reopened.Entries[0].payload);
        }

        [Test] public void Failures_BackOff_5_15_60_300_ThenStayAt300()
        {
            var outbox = new ScoreOutbox(path);
            outbox.Enqueue(Result("a1"), 0);
            long now = 0;
            foreach (int seconds in new[] { 5, 15, 60, 300, 300 })
            {
                var due = outbox.NextDue(now);
                Assert.IsNotNull(due);
                outbox.MarkFailed(due.id, now);
                Assert.IsNull(outbox.NextDue(now + seconds * 1000L - 1), $"Chưa tới lượt thử lại sau {seconds} s");
                now += seconds * 1000L;
                Assert.IsNotNull(outbox.NextDue(now));
            }
        }

        [Test] public void Sent_RemovesEntry_AndSurvivesRestart()
        {
            var outbox = new ScoreOutbox(path);
            outbox.Enqueue(Result("a1"), 0);
            outbox.Enqueue(Result("a2"), 0);
            outbox.MarkSent("a1");
            var reopened = new ScoreOutbox(path);
            Assert.AreEqual(1, reopened.Entries.Count);
            Assert.AreEqual("a2", reopened.Entries[0].id);
        }

        [Test] public void CorruptFile_StartsEmpty_AndKeepsBackup()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{không phải json");
            var outbox = new ScoreOutbox(path);
            Assert.AreEqual(0, outbox.Entries.Count);
            Assert.IsTrue(File.Exists(path + ".hong"));
        }

        // Báo cáo kiểm tra 24/10, P2: người gửi giữ ảnh chụp hàng chờ trong lúc chờ mạng
        [Test] public void StaleSnapshot_MarkSentOrFailed_KeepsEntriesAddedMeanwhile()
        {
            new ScoreOutbox(path).Enqueue(Result("a1"), 0);
            var sender = new ScoreOutbox(path);          // người gửi mở hàng chờ, bắt đầu gửi a1
            new ScoreOutbox(path).Enqueue(Result("a2"), 0); // lượt thắng mới được xếp hàng
            new ScoreOutbox(path).Enqueue(Result("a3"), 0);
            sender.MarkFailed("a3", 0);
            sender.MarkSent("a1");
            var reopened = new ScoreOutbox(path);
            Assert.AreEqual(2, reopened.Entries.Count, "Kết quả xếp hàng trong lúc gửi không được mất");
            Assert.IsNotNull(reopened.NextDue(0));
            Assert.AreEqual("a2", reopened.NextDue(0).id);
        }

        // Báo cáo kiểm tra 24/10, P2: rút lại đồng ý phải chặn cả kết quả đã nằm trong hàng chờ
        [Test] public void WithdrawnConsent_DropsQueuedResult_WithoutSending()
        {
            using (ProfileService.UseTemporaryStore(Path.Combine(Path.GetDirectoryName(path), "profiles.json")))
            {
                var profile = ProfileService.Repository.Create("Kiểm tra", "", true);
                var outbox = new ScoreOutbox(path);
                outbox.Enqueue(new RankedResult { attemptId = "a1", profileId = profile.id }, 0);
                Assert.AreEqual("a1", ScoreOutboxSender.NextSendable(new ScoreOutbox(path), 0)?.id, "Còn đồng ý thì được gửi");

                ProfileService.Repository.SetConsent(false);
                Assert.IsNull(ScoreOutboxSender.NextSendable(new ScoreOutbox(path), 0), "Đã rút đồng ý thì không gửi");
                Assert.AreEqual(0, new ScoreOutbox(path).Entries.Count, "Kết quả của người rút đồng ý bị bỏ khỏi hàng chờ");
            }
        }

        [Test] public void UnknownOwner_IsNeverSent()
        {
            using (ProfileService.UseTemporaryStore(Path.Combine(Path.GetDirectoryName(path), "profiles.json")))
            {
                new ScoreOutbox(path).Enqueue(new RankedResult { attemptId = "a1", profileId = "khong-ton-tai" }, 0);
                Assert.IsNull(ScoreOutboxSender.NextSendable(new ScoreOutbox(path), 0));
            }
        }
    }
}
