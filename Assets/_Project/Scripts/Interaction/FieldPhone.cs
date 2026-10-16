using System.Collections;
using DBP.Core;
using DBP.Quiz;
using UnityEngine;

namespace DBP.Interaction
{
    /// Điện thoại dã chiến gọi pháo 105 mm (T31; BR-38). Giữ E 1 giây → câu cấp 3, 15 giây.
    /// Đúng: pháo trúng mục tiêu được chỉ định. Sai/hết giờ: pháo trượt, không gây sát thương.
    /// Không hưởng và không tác động bảo hiểm của hòm. Luôn còn phương án khác (bộc phá, BR-17).
    public class FieldPhone : MonoBehaviour, IInteractable
    {
        [Tooltip("Mục tiêu gắn với điện thoại này (lô cốt, xe tăng…). Bắt buộc.")]
        public Health target;
        [Tooltip("Thời gian đạn pháo bay tới, tính theo thời gian chiến trường.")]
        public float shellDelay = 1.5f;
        public float missDistance = 18f;
        public float blastRadius = 4f;

        bool used;

        public string Prompt => "Giữ E: gọi pháo 105 mm";
        public float HoldSeconds => 1f;
        public bool Used => used;

        /// Mục tiêu đã bị phá trước khi mở câu: điện thoại không khả dụng, không tiêu thụ câu.
        public bool CanInteract(Interactor who)
        {
            var session = FieldQuizSession.Instance;
            return !used && target && !target.IsDead && session && !session.IsOpen;
        }

        public void Interact(Interactor who)
        {
            if (!target)
            {
                Debug.LogError($"[FieldPhone] {name} chưa gắn mục tiêu (lỗi cấu hình, BR-38).");
                return;
            }
            var session = FieldQuizSession.Instance;
            if (!session.TryOpen(3, 3, 15f, r => StartCoroutine(Strike(r.outcome == QuizOutcome.Correct))))
            {
                if (!session.IsOpen) used = true; // thiếu câu cấp 3: khóa điện thoại lỗi (BR-31)
                return;
            }
            used = true;
        }

        /// WaitForSeconds dùng thời gian game: đạn pháo chỉ bay khi chiến trường đã chạy lại.
        IEnumerator Strike(bool hit)
        {
            yield return new WaitForSeconds(shellDelay);
            if (!target) yield break;
            var point = target.transform.position;
            if (!hit)
            {
                var away = target.transform.position - transform.position;
                away.y = 0f;
                point += (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * missDistance;
            }
            ExplosionFlash.Spawn(point, blastRadius);
            // Mục tiêu đã bị phá trước lúc đạn tới: TakeDamage không tính phá lần hai.
            if (hit) target.TakeDamage(99999f, DamageType.Artillery, point);
        }
    }
}
