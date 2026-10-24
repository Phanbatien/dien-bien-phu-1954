using System;
using System.IO;
using DBP.Ranking;
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
    }
}
