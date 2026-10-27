using W = DBP.UI.UIWidgets;

namespace DBP.UI
{
    /// Xem toàn cảnh Hầm chữ A (ẩn bảng chuẩn bị): đèn dầu, bàn bản đồ, điện đài, giá súng.
    public sealed partial class CampaignUI
    {
        void BunkerViewPage()
        {
            W.Label(page, "BunkerCaption", "Hầm chữ A — nơi họp tác chiến và chuẩn bị trước giờ xuất trận", 64, 110, 1200, 50, 26);
            W.Button(page, "BackToPreparation", "← Về bảng chuẩn bị", 64, 760, 440, 66, () => Show(CampaignScreen.Preparation));
        }
    }
}
