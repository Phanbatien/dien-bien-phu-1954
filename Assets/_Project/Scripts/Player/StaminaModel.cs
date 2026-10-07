using UnityEngine;

namespace DBP.Player
{
    /// Thể lực chạy (T12). Chạy hết thể lực thì "kiệt sức": phải hồi lại tới ngưỡng mới chạy tiếp.
    /// Lớp thuần C# để test được ngoài Unity.
    public class StaminaModel
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool Exhausted { get; private set; }

        public float DrainPerSecond = 20f;
        public float RegenPerSecond = 15f;
        public float RegenDelay = 1f;
        public float RecoverFraction = 0.3f;

        float sinceSprint;

        public StaminaModel(float max)
        {
            Max = max;
            Current = max;
        }

        /// Đổi trần thể lực (nâng cấp BR-35) và hồi đầy.
        public void SetMax(float max)
        {
            Max = max;
            Current = max;
            Exhausted = false;
        }

        public bool CanSprint => !Exhausted && Current > 0f;

        /// sprinting: người chơi đang giữ chạy và thực sự di chuyển.
        public void Tick(float dt, bool sprinting)
        {
            if (sprinting && CanSprint)
            {
                sinceSprint = 0f;
                Current = Mathf.Max(0f, Current - DrainPerSecond * dt);
                if (Current <= 0f) Exhausted = true;
                return;
            }

            sinceSprint += dt;
            if (sinceSprint >= RegenDelay)
                Current = Mathf.Min(Max, Current + RegenPerSecond * dt);
            if (Exhausted && Current + 0.001f >= Max * RecoverFraction) Exhausted = false; // 0,3f không chính xác tuyệt đối
        }
    }
}
