using System;
using System.Collections.Generic;
using DBP.Content;
using DBP.Quiz;
using DBP.Upgrades;
using UnityEngine;

namespace DBP.UI
{
    /// Họp tác chiến (T25) không còn bắt buộc từ 28/10: câu hỏi chuyển vào hòm giữa trận. Giữ bộ chọn câu dùng chung
    /// và hàm cho test; buổi họp đã trả lời (nếu có) vẫn được giao cho màn chơi khi xuất trận.
    public sealed partial class CampaignUI
    {
        BunkerBriefing briefing;

        public BunkerBriefing Briefing { get { EnsureBriefing(); return briefing; } }
        bool BriefingReady => briefing != null && briefing.Ready;

        void EnsureBriefing()
        {
            string id = "M" + (SelectedMission + 1);
            if (briefing != null && briefing.MissionId == id) return;
            var errors = new List<string>();
            var bank = QuestionBank.LoadAll(errors);
            foreach (var e in errors) Debug.LogError($"[Họp tác chiến] {e}");
            briefing = BunkerBriefing.Create(id, Environment.TickCount, bank, Debug.isDebugBuild);
        }

        /// Dành cho test tự động: trả lời hết buổi họp (đúng hoặc sai).
        public void CompleteBriefingForTests(bool correct = true)
        {
            EnsureBriefing();
            while (!briefing.Complete)
            {
                var q = briefing.Current;
                string wrong = Array.Find(q.options, o => o.id != q.correct)?.id;
                briefing.Answer(correct ? q.correct : wrong);
            }
            if (ScreenState == CampaignScreen.Preparation) Show(CampaignScreen.Preparation); // mở khóa nút Xuất trận
        }
    }
}
