using DBP.Missions;
using UnityEngine;

namespace DBP.AI
{
    /// T23 (BR-14): Giới hạn số lượng vũ khí tối đa có thể rơi từ lính địch trong một lượt chơi.
    /// Mặc định tối đa 5 khẩu mỗi lượt. Tự động reset khi lượt chơi kết thúc.
    public static class DropLimitTracker
    {
        public static int MaxDrops = 5;
        static int currentDrops = 0;

        public static int CurrentDrops => currentDrops;

        static DropLimitTracker()
        {
            MissionController.Ended += OnMissionEnded;
        }

        static void OnMissionEnded(Attempt _)
        {
            Reset();
        }

        /// Kiểm tra xem còn có thể rơi súng trong lượt chơi này hay không.
        public static bool CanDrop() => currentDrops < MaxDrops;

        /// Ghi nhận một lần rơi súng thành công.
        public static void RegisterDrop()
        {
            currentDrops++;
        }

        /// Đặt lại số lượng súng đã rơi về 0.
        public static void Reset()
        {
            currentDrops = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            currentDrops = 0;
            MaxDrops = 5;
        }
    }
}
