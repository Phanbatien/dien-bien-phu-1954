using System.Linq;
using DBP.UI;
using UnityEngine;

namespace DBP.Missions
{
    /// Thoại vô tuyến Màn 3 (T47): sở chỉ huy gọi xuống khi mở màn và mỗi lần xong một mục tiêu.
    /// Người nói là đơn vị hư cấu, không gắn tên nhân vật thật (BR-01).
    public class RadioChatter : MonoBehaviour
    {
        [TextArea] public string[] lines =
        {
            "[Vô tuyến] Sở chỉ huy gọi Đại đội 1: pháo ta đang bắn chuẩn bị vào đồi E, D. Chờ pháo dứt thì xung phong. Hết.",
            "[Vô tuyến] Mũi xung kích đã áp sát hàng rào. Pháo sáng địch lên thì nằm im, tắt sáng mới tiến. Hết.",
            "[Vô tuyến] Đồi D1 đã cắm cờ! Các mũi tiếp tục đánh vào trung tâm cứ điểm. Hết.",
            "[Vô tuyến] Địch bắn pháo chặn và chuẩn bị phản kích. Đào hầm cá nhân, giữ chắc trận địa vừa chiếm. Hết.",
            "[Vô tuyến] Tốt lắm, các đồng chí! Giữ chốt đến sáng, chờ lệnh mới. Hết.",
        };
        public float firstDelay = 4f;

        int shown, lastDone;
        float startAt;

        void Start() => startAt = Time.time + firstDelay;

        void Update()
        {
            var mission = MissionController.Current;
            if (!mission || shown >= lines.Length) return;
            if (shown == 0) { if (Time.time >= startAt) Say(); return; }
            int done = mission.Objectives.Count(o => o && o.Done);
            if (done > lastDone) { lastDone = done; Say(); }
        }

        void Say() => CampaignNotices.Show(lines[shown++]);
    }
}
