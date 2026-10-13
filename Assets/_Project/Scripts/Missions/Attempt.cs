using System;
using System.Collections.Generic;
using DBP.Interaction;
using DBP.Quiz;

namespace DBP.Missions
{
    public enum MissionOutcome { None, Won, Lost, Abandoned }

    /// Một lượt chơi màn (BR-33): mã riêng, seed mới, chỉ số chiến đấu và câu đã trả lời.
    /// Chơi lại luôn tạo Attempt mới; không mang đồ/điểm tạm từ lượt cũ.
    public class Attempt
    {
        public string AttemptId { get; } = Guid.NewGuid().ToString("N");
        public string MissionId { get; }
        public int Seed { get; }

        // Tiêu chí bảng xếp hạng chiến đấu, theo thứ tự (BR-24)
        public int Kills;
        public int BunkersDestroyed;
        public int WavesRepelled;
        public float DamageTaken;
        public double BattleStart, BattleEnd;

        /// false khi lượt gặp lỗi thiếu câu (BR-31): vẫn thắng được nhưng không lên bảng.
        public bool RankEligible = true;
        public readonly List<QuizResult> Answers = new List<QuizResult>();

        /// Chuỗi sai của hòm (BR-16); lượt mới bắt đầu từ 0.
        public readonly PityTracker Pity = new PityTracker();
        /// Ngẫu nhiên của lượt (súng rơi…), suy ra từ seed để tái hiện khi kiểm tra (BR-13).
        public readonly System.Random Rng;
        public int WeaponDrops;
        /// Ghi hồ sơ lỗi khi quyết toán: màn kết quả phải báo, không hiện "đã lưu" (BR-37).
        public bool SaveFailed;

        public MissionOutcome Outcome { get; private set; } = MissionOutcome.None;
        public double CompletionSeconds => BattleEnd - BattleStart;

        public Attempt(string missionId, int seed)
        {
            MissionId = missionId;
            Seed = seed;
            Rng = new System.Random(seed ^ 0x5EED);
        }

        /// Chốt kết quả đúng một lần (BR-32).
        public bool Settle(MissionOutcome outcome)
        {
            if (Outcome != MissionOutcome.None || outcome == MissionOutcome.None) return false;
            Outcome = outcome;
            return true;
        }
    }

    public static class MissionRules
    {
        /// Hết máu là thua; hết máu và hoàn thành mục tiêu cùng lúc thì ưu tiên thua (BR-32).
        public static MissionOutcome Evaluate(bool playerDead, bool objectivesComplete) =>
            playerDead ? MissionOutcome.Lost
            : objectivesComplete ? MissionOutcome.Won
            : MissionOutcome.None;
    }
}
