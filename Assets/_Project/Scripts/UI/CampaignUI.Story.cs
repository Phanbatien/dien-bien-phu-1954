using System;
using System.Linq;
using DBP.Content;
using UnityEngine;
using W = DBP.UI.UIWidgets;

namespace DBP.UI
{
    /// Cốt truyện không điều khiển (T34, T61): mở đầu 90 giây, nhật ký trong hầm Màn 1, kết thúc chiến dịch,
    /// màn Nguồn tư liệu & Credits. Nhân vật lịch sử thật chỉ xuất hiện ở lời dẫn, không là nhân vật chơi (BR-01, BR-02, BR-32).
    public sealed partial class CampaignUI
    {
        public struct Slide { public string heading, body; }

        /// 6 khung × 15 giây = 90 giây (storyboard T34, xem docs/cot-truyen.md).
        public static readonly Slide[] Prologue =
        {
            new Slide { heading = "Cuối năm 1953", body = "Thực dân Pháp, có Mỹ giúp sức, biến lòng chảo Điện Biên Phủ thành tập đoàn cứ điểm mạnh nhất Đông Dương: 49 cứ điểm chia thành 3 phân khu, hai sân bay, pháo binh và xe tăng." },
            new Slide { heading = "Quyết định lịch sử", body = "Tháng 12/1953, Bộ Chính trị quyết định mở chiến dịch Điện Biên Phủ. Đại tướng Võ Nguyên Giáp được giao làm Chỉ huy trưởng kiêm Bí thư Đảng ủy mặt trận." },
            new Slide { heading = "Kéo pháo vào trận địa", body = "Bộ đội dùng sức người kéo pháo qua đèo cao, vực sâu. Khi dây tời đứt, chiến sĩ Tô Vĩnh Diện lao mình chèn bánh pháo, hy sinh để giữ khẩu pháo không lao xuống vực." },
            new Slide { heading = "Đánh chắc, tiến chắc", body = "Cuối tháng 1/1954, nhận thấy địch đã củng cố, Bộ chỉ huy chiến dịch chuyển phương châm từ “đánh nhanh, giải quyết nhanh” sang “đánh chắc, tiến chắc”. Pháo được kéo ra, chuẩn bị lại từ đầu." },
            new Slide { heading = "Hậu phương ra tiền tuyến", body = "Hơn 260.000 dân công cùng gần 21.000 xe đạp thồ, ngựa thồ, thuyền bè vượt hàng trăm cây số đường rừng, đưa lương thực, đạn dược ra mặt trận." },
            new Slide { heading = "17 giờ 05, ngày 13/3/1954", body = "Pháo ta đồng loạt khai hỏa vào cụm cứ điểm Him Lam. Chiến dịch Điện Biên Phủ bắt đầu.\n\nBạn là một chiến sĩ vô danh trong đội hình xung kích." },
        };

        /// Nhật ký của một chiến sĩ hư cấu trong hầm chữ A trước giờ nổ súng Màn 1.
        public static readonly Slide[] Diary =
        {
            new Slide { heading = "Nhật ký trong hầm — trưa 13/3/1954", body = "Đèn dầu chỉ còn một ngấn. Anh em ngồi sát vai nhau, lau súng lần thứ ba trong buổi sáng. Không ai nói to, chỉ nghe tiếng đất rơi lộp bộp mỗi khi máy bay địch lượn qua." },
            new Slide { heading = "Nhật ký trong hầm — chiều 13/3/1954", body = "Chính trị viên đọc thư động viên của Bộ chỉ huy chiến dịch. Tôi gấp lá thư mẹ vào túi áo ngực, cạnh tấm ảnh làng. Hào xung phong đã đào tới sát hàng rào Him Lam. Chiều nay, giờ G." },
        };

        public static readonly Slide[] Epilogue =
        {
            new Slide { heading = "17 giờ 30, ngày 7/5/1954", body = "Lá cờ “Quyết chiến — Quyết thắng” tung bay trên nóc hầm chỉ huy. Tướng De Castries cùng toàn bộ Bộ tham mưu tập đoàn cứ điểm bị bắt sống." },
            new Slide { heading = "56 ngày đêm", body = "Sau 56 ngày đêm “khoét núi, ngủ hầm, mưa dầm, cơm vắt”, quân và dân ta tiêu diệt và bắt sống toàn bộ tập đoàn cứ điểm Điện Biên Phủ, hơn 16.000 quân địch." },
            new Slide { heading = "Hiệp định Genève", body = "Ngày 21/7/1954, Hiệp định Genève được ký kết, lập lại hòa bình ở Đông Dương. Chiến thắng Điện Biên Phủ “lừng lẫy năm châu, chấn động địa cầu”." },
            new Slide { heading = "Tưởng nhớ", body = "Hàng nghìn chiến sĩ đã nằm lại trên lòng chảo này để có ngày hôm nay.\nTrò chơi được làm để tưởng nhớ và tri ân những người đã ngã xuống." },
        };

        const float SlideSeconds = 15f;
        Slide[] deck;
        int slideIndex;
        float slideStarted;
        Action deckDone;

        /// Mở một chuỗi khung (thời gian thực, tự chuyển sau 15 giây). Hết chuỗi hoặc bấm Bỏ qua thì gọi done.
        public void PlayStory(Slide[] slides, Action done)
        {
            deck = slides; slideIndex = 0; deckDone = done;
            slideStarted = Time.unscaledTime;
            Show(CampaignScreen.Story);
        }

        public void NextSlide()
        {
            if (ScreenState != CampaignScreen.Story || deck == null) return;
            if (++slideIndex >= deck.Length) { EndStory(); return; }
            slideStarted = Time.unscaledTime;
            Show(CampaignScreen.Story);
        }

        void EndStory()
        {
            var done = deckDone; deck = null; deckDone = null;
            (done ?? (() => Show(CampaignScreen.Menu)))();
        }

        void TickStory()
        {
            if (ScreenState == CampaignScreen.Story && deck != null && Time.unscaledTime - slideStarted >= SlideSeconds) NextSlide();
        }

        void StoryPage()
        {
            if (deck == null || slideIndex >= deck.Length) return;
            var slide = deck[slideIndex];
            W.Label(page, "StoryCount", $"{slideIndex + 1} / {deck.Length}", 200, 150, 1200, 40, 22, W.Gold);
            W.Label(page, "StoryHeading", slide.heading, 200, 200, 1200, 80, 44, W.Gold);
            W.Label(page, "StoryBody", slide.body, 200, 310, 1200, 330, 32);
            W.Panel(page, "StoryProgress", 200, 690, 1200 * (slideIndex + 1) / (float)deck.Length, 4, W.Gold);
            W.Button(page, "SkipStory", "Bỏ qua", 200, 736, 380, 68, EndStory);
            W.Button(page, "NextSlide", slideIndex + 1 < deck.Length ? "Tiếp →" : "Xong →", 1020, 736, 380, 68, NextSlide);
        }

        void CreditsPage()
        {
            W.Label(page, "Title", "Nguồn tư liệu & Credits", 64, 110, 1200, 76, 46);
            W.Panel(page, "TeamPanel", 64, 200, 520, 600);
            W.Label(page, "TeamHeading", "NHÓM THỰC HIỆN", 88, 214, 470, 44, 24, W.Gold);
            W.Label(page, "Team",
                "Phanbatien — Trưởng nhóm, FPS, tích hợp, build\n" +
                "Bùi Nguyễn Thành Đạt — AI địch, chiến đấu\n" +
                "Lê Nguyễn Đình Khôi — Môi trường, mô hình\n" +
                "Thiên Trí — Câu hỏi, tư liệu, cốt truyện\n" +
                "Trần Hoàng Long — Giao diện, âm thanh, xếp hạng\n\n" +
                "Tài nguyên: Unity Starter Assets, Polytope Studio, Simple Nature Pack, RSG, Defensive Props, Low Poly Weapon Series, tt-3d Low Poly War. Phông chữ Be Vietnam Pro (SIL OFL 1.1)\n\n" +
                "Dự án học tập FPT University, 2026. Không dùng vào mục đích thương mại.", 88, 266, 470, 520, 21);
            W.Panel(page, "SourcePanel", 610, 200, 926, 600);
            W.Label(page, "SourceHeading", "NGUỒN TƯ LIỆU CÂU HỎI VÀ CỐT TRUYỆN", 634, 214, 880, 44, 24, W.Gold);
            W.Label(page, "Sources", SourceList(), 634, 266, 880, 520, 18);
            W.Button(page, "CreditsBack", "← Về menu", 1096, 820, 440, 56, () => { if (mission) ReturnToMenu(false); else Show(CampaignScreen.Menu); });
        }

        static string SourceList()
        {
            if (!ContentLoader.TryLoad<SourceFile>("sources.json", out var file, out var error) || file?.sources == null)
                return "Không đọc được danh sách nguồn: " + error;
            var rows = file.sources.Where(s => s != null && !string.IsNullOrEmpty(s.title))
                .Select(s => "• " + s.title + (string.IsNullOrEmpty(s.publisher) ? "" : " — " + s.publisher)).Take(18).ToList();
            int more = file.sources.Length - rows.Count;
            return string.Join("\n", rows) + (more > 0 ? $"\n… và {more} nguồn khác (xem sources.json)" : "");
        }

        /// Nút trên menu: xem mở đầu, xem nguồn & credits. Gọi cuối Menu() để nằm trên các nhãn.
        void StoryMenuButtons()
        {
            W.Button(page, "PlayPrologue", "▶ Mở đầu chiến dịch", 1190, 112, 346, 52, () => PlayStory(Prologue, () => Show(CampaignScreen.Menu)));
            W.Button(page, "OpenCredits", "Nguồn & Credits", 1190, 176, 346, 52, () => Show(CampaignScreen.Credits));
        }
    }
}
