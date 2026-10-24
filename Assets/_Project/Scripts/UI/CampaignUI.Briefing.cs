using System;
using System.Collections.Generic;
using DBP.Content;
using DBP.Quiz;
using DBP.Upgrades;
using UnityEngine;
using W = DBP.UI.UIWidgets;

namespace DBP.UI
{
    /// Bước "Họp tác chiến" trong màn Chuẩn bị (T25). Câu hỏi hiện bằng BunkerQuizPanel của Thiên Trí.
    public sealed partial class CampaignUI
    {
        BunkerBriefing briefing;
        BunkerQuizPanel bunkerPanel;

        public BunkerBriefing Briefing => briefing;
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

        /// Gọi ở cuối Preparation(): trạng thái buổi họp và nút vào họp.
        void BriefingSection()
        {
            EnsureBriefing();
            string text;
            if (briefing.HasShortage)
                text = $"HỌP TÁC CHIẾN: thiếu câu hỏi trong hầm ({briefing.Questions.Count}/{BunkerBriefing.MinQuestions}). Báo nhóm nội dung (BR-31).";
            else if (!briefing.Complete)
                text = $"HỌP TÁC CHIẾN: trả lời {briefing.Questions.Count} câu trước khi xuất trận • không giới hạn thời gian";
            else
                text = $"Đã họp xong • Quyết Tâm chờ +{briefing.PendingResolve(ProfileService.Data.rewardedQuestions)} (chỉ nhận khi thắng màn)";
            W.Label(page, "BriefingInfo", text, 64, 694, 1472, 40, 22, W.Gold);
            if (!briefing.HasShortage && !briefing.Complete)
                W.Button(page, "Briefing", $"VÀO HỌP ({briefing.Results.Count}/{briefing.Questions.Count})", 520, 740, 560, 66, StartBriefing);
        }

        void StartBriefing()
        {
            if (ScreenState != CampaignScreen.Preparation || briefing == null || briefing.Complete) return;
            if (!bunkerPanel)
            {
                if (!TryGetComponent(out bunkerPanel)) bunkerPanel = gameObject.AddComponent<BunkerQuizPanel>();
                bunkerPanel.Continued += OnBriefingContinued;
            }
            ClearPage(); // ẩn các nút phía dưới để không bấm nhầm khi đang họp
            ShowBriefingQuestion();
        }

        void ShowBriefingQuestion() => bunkerPanel.Show(briefing.Current, 0f, id => briefing.Answer(id));

        void OnBriefingContinued()
        {
            if (briefing != null && !briefing.Complete) ShowBriefingQuestion();
            else Show(CampaignScreen.Preparation);
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
