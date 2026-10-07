using UnityEngine;

namespace DBP.Core
{
    /// Khóa điều khiển người chơi (đi, ngắm, bắn, xoay camera).
    /// Đếm số lần khóa để câu hỏi, menu tạm dừng, màn kết quả không mở khóa nhầm của nhau.
    public static class InputLock
    {
        static int count;

        public static bool Locked => count > 0;
        public static void Push() => count++;
        public static void Pop() => count = Mathf.Max(0, count - 1);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll() => count = 0;
    }
}
