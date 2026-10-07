using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DBP.Interaction
{
    /// Hợp đồng cho mọi thứ người chơi tương tác bằng E: hòm, súng rơi, điện thoại, đồ để lại, ụ súng.
    public interface IInteractable
    {
        string Prompt { get; }
        /// 0 = nhấn E; > 0 = phải giữ E liên tục (hòm/súng rơi/điện thoại: 1 giây, BR-08).
        float HoldSeconds { get; }
        bool CanInteract(Interactor who);
        void Interact(Interactor who);
    }

    /// Đếm thời gian giữ E (BR-08). Thả phím, đổi mục tiêu hoặc mất mục tiêu thì làm lại từ đầu.
    /// Sau khi kích hoạt phải thả E ra mới tính lần giữ mới.
    public class HoldProgress
    {
        public float Elapsed { get; private set; }
        public object Target { get; private set; }
        bool awaitingRelease;

        public void Cancel()
        {
            Elapsed = 0f;
            Target = null;
        }

        /// Trả true đúng khung hình đủ thời gian giữ.
        public bool Tick(object target, bool holding, float dt, float required)
        {
            if (!holding) awaitingRelease = false;
            if (!holding || target == null || awaitingRelease)
            {
                Cancel();
                return false;
            }
            if (!ReferenceEquals(target, Target))
            {
                Target = target;
                Elapsed = 0f;
            }
            Elapsed += dt;
            if (Elapsed < required) return false;
            Cancel();
            awaitingRelease = true;
            return true;
        }
    }
}
