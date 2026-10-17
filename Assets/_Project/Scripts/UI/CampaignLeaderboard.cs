using System;
using System.IO;
using DBP.Missions;
using DBP.Ranking;
using UnityEngine;
using W = DBP.UI.UIWidgets;
namespace DBP.UI
{
    public sealed partial class CampaignUI
    {
        bool boardKnowledge, boardFromResults;
        RankingGroup boardGroup;
        int boardPage;
        const int BoardPageSize = 6;
        public void OpenLeaderboard(bool fromResults = false)
        {
            boardFromResults = fromResults; boardPage = 0;
            boardGroup = fromResults ? mission?.Attempt.RankingResult?.group.Copy() : null;
            Show(CampaignScreen.Leaderboard);
        }
        void LeaderboardPage()
        {
            W.Label(page,"LeaderboardTitle",boardKnowledge ? "BẢNG XẾP HẠNG · KIẾN THỨC" : "BẢNG XẾP HẠNG · CHIẾN ĐẤU",64,107,1472,65,40,W.Gold);
            W.Button(page,"LeaderboardBack",boardFromResults ? "← Kết quả" : "← Menu",64,768,250,54,()=>Show(boardFromResults ? CampaignScreen.Results : CampaignScreen.Menu));
            try
            {
                var store = LeaderboardService.Open();
                var groups = store.Groups;
                if (groups.Count == 0) groups.Add(boardGroup ?? LeaderboardService.Group("M"+(SelectedMission+1),1));
                int index = boardGroup == null ? 0 : groups.FindIndex(g=>g.Equals(boardGroup));
                if (index < 0) { groups.Add(boardGroup.Copy()); index = groups.Count-1; }
                boardGroup = groups[index].Copy();
                W.Label(page,"LeaderboardConditions",$"Màn {boardGroup.missionId}  •  Độ khó {boardGroup.difficulty}  •  Nâng cấp (đạn / thay đạn / thể lực / lựu đạn): {boardGroup.upgrades}\nNội dung: {boardGroup.contentVersion}\nQuy tắc: {boardGroup.rulesVersion}",64,185,1472,112,20);
                W.Button(page,"CombatTab","Chiến đấu",64,315,270,54,()=>{boardKnowledge=false;boardPage=0;Show(CampaignScreen.Leaderboard);},boardKnowledge);
                W.Button(page,"KnowledgeTab","Kiến thức",350,315,270,54,()=>{boardKnowledge=true;boardPage=0;Show(CampaignScreen.Leaderboard);},!boardKnowledge);
                W.Label(page,"GroupCount",$"Nhóm điều kiện {index+1} / {groups.Count}",770,315,480,54,23);
                W.Button(page,"PrevRankGroup","←",1300,315,104,54,()=>{boardGroup=groups[index-1];boardPage=0;Show(CampaignScreen.Leaderboard);},index>0);
                W.Button(page,"NextRankGroup","→",1420,315,116,54,()=>{boardGroup=groups[index+1];boardPage=0;Show(CampaignScreen.Leaderboard);},index+1<groups.Count);
                var rows = store.Rows(boardGroup,boardKnowledge);
                boardPage = Mathf.Clamp(boardPage,0,Math.Max(0,(rows.Count-1)/BoardPageSize));
                W.Label(page,"RankHeading","HẠNG",78,384,100,42,20,W.Gold);
                W.Label(page,"ProfileHeading","HỒ SƠ / LỚP",190,384,360,42,20,W.Gold);
                W.Label(page,"StatsHeading",boardKnowledge ? "TỔNG ĐIỂM TRONG MỘT LƯỢT" : "HẠ ĐỊCH  /  LÔ CỐT  /  ĐẨY LÙI  /  SÁT THƯƠNG  /  THỜI GIAN",580,384,940,42,19,W.Gold);
                if (rows.Count == 0) W.Label(page,"LeaderboardEmpty","Chưa có lượt thắng hợp lệ trong nhóm điều kiện này.\nHoàn thành nhiệm vụ để lưu thành tích trên máy.",120,460,1380,150,28);
                for (int i=boardPage*BoardPageSize;i<Math.Min(rows.Count,(boardPage+1)*BoardPageSize);i++)
                {
                    int slot = i % BoardPageSize; float y = 430+slot*50; var row=rows[i];var r=row.result;
                    W.Panel(page,"LeaderboardRow"+slot,64,y,1472,46);
                    W.Label(page,"LeaderboardRank"+slot,row.rank.ToString(),78,y,100,46,25,W.Gold);
                    W.Label(page,"LeaderboardName"+slot,ShortLabel(r.displayName,17)+"\n"+ShortLabel(r.className,12)+"  #"+ShortLabel(r.profileId,7),190,y,365,46,17);
                    string stats = boardKnowledge ? r.knowledge+" điểm" : $"{r.kills}     /     {r.bunkers}     /     {r.waves}     /     {r.damage:0.###}     /     {r.milliseconds/1000m:0.000} s";
                    W.Label(page,"LeaderboardStats"+slot,stats,580,y,940,46,23);
                }
                W.Button(page,"PrevRankPage","←",1050,768,100,54,()=>{boardPage--;Show(CampaignScreen.Leaderboard);},boardPage>0);
                W.Label(page,"RankPageCount",$"Trang {boardPage+1} / {Math.Max(1,(rows.Count+BoardPageSize-1)/BoardPageSize)}",1170,768,230,54,23);
                W.Button(page,"NextRankPage","→",1420,768,116,54,()=>{boardPage++;Show(CampaignScreen.Leaderboard);},(boardPage+1)*BoardPageSize<rows.Count);
                W.Label(page,"TieHint","Đồng hạng 1, 1, 3 • Mỗi hồ sơ giữ lượt tốt nhất riêng cho từng bảng.",350,768,680,54,20);
                if(store.RecoveredBackup) status.text="Đang xem bản sao lưu sau lỗi dữ liệu thành tích.";
            }
            catch(Exception e) when(e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                W.Label(page,"LeaderboardError","Không thể đọc bảng xếp hạng. Dữ liệu gốc được giữ nguyên.\n"+e.Message,100,270,1400,250,27);
                W.Button(page,"RetryLeaderboardRead","Thử đọc lại",1050,768,486,54,()=>Show(CampaignScreen.Leaderboard));
            }
        }
        void RankingResultStatus(Attempt attempt)
        {
            bool valid = attempt.Outcome == MissionOutcome.Won && attempt.RankEligible && attempt.RankingResult != null && !string.IsNullOrWhiteSpace(attempt.RankingResult.profileId);
            string note = !string.IsNullOrEmpty(attempt.RankingSaveError) ? "Chưa lưu bảng xếp hạng: "+attempt.RankingSaveError
                : valid ? "Đã xét hai bảng tại máy; giữ lượt tốt nhất riêng cho mỗi bảng."
                : "Lượt này không đủ điều kiện xếp hạng (cần hồ sơ và lượt thắng hợp lệ).";
            bool retry = valid && !string.IsNullOrEmpty(attempt.RankingSaveError);
            W.Label(page,"RankingResultNote",note,160,678,retry ? 880 : 1280,48,19,W.Gold);
            if(retry) W.Button(page,"RetryRankingSave","Thử lưu thành tích",1060,682,380,42,()=>{LeaderboardService.TrySave(attempt);Show(CampaignScreen.Results);});
        }
    }
}
