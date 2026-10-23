using System.Collections;
using DBP.Core;
using DBP.Missions;
using DBP.Quiz;
using UnityEngine;

namespace DBP.Interaction
{
    /// Điện thoại gọi pháo chi viện (T31, BR-38):
    /// Giữ E 1 giây → câu hỏi cấp 3, thời hạn 15 giây.
    /// Đúng → pháo bắn trúng mục tiêu sau một khoảng trễ (strikeDelay).
    /// Sai / hết giờ → pháo trượt.
    /// Nếu mục tiêu đã bị phá hủy từ trước → không mở câu hỏi, không tính lượt dùng (BR-38).
    /// Mỗi điện thoại chỉ dùng thành công 1 lần trong lượt chơi.
    public class ArtilleryPhone : MonoBehaviour, IInteractable
    {
        [Tooltip("Bunker hoặc mục tiêu gắn liền với điện thoại chi viện này.")]
        [SerializeField] public Health targetHealth;

        [Tooltip("Vị trí pháo sẽ nổ tập trung.")]
        [SerializeField] public Transform targetPosition;

        [Tooltip("Hiệu ứng nổ pháo (VFX/SFX).")]
        [SerializeField] public GameObject artilleryStrikePrefab;

        [Tooltip("Thời gian trễ từ lúc gọi đến khi đạn pháo chạm mục tiêu (giây).")]
        [SerializeField] public float strikeDelay = 2f;

        [Tooltip("Sát thương đòn pháo (BR-20: lô cốt cần sát thương Explosive/Artillery lớn).")]
        [SerializeField] public float artilleryDamage = 600f;

        [Tooltip("Trạng thái đã sử dụng điện thoại trong lượt chơi.")]
        [SerializeField] public bool used;

        public Health TargetHealth { get => targetHealth; set => targetHealth = value; }
        public Transform TargetPosition { get => targetPosition; set => targetPosition = value; }
        public GameObject ArtilleryStrikePrefab { get => artilleryStrikePrefab; set => artilleryStrikePrefab = value; }
        public float StrikeDelay { get => strikeDelay; set => strikeDelay = value; }
        public float ArtilleryDamage { get => artilleryDamage; set => artilleryDamage = value; }
        public bool Used { get => used; set => used = value; }

        public string Prompt => "Giữ E: gọi pháo";
        public float HoldSeconds => 1f;

        public bool CanInteract(Interactor who)
        {
            if (used) return false;
            if (targetHealth == null || targetHealth.IsDead) return false;
            var session = FieldQuizSession.Instance;
            if (session != null && session.IsOpen) return false;
            return true;
        }

        public void Interact(Interactor who)
        {
            // Nếu mục tiêu đã phá → không mở câu, không dùng (BR-38)
            if (targetHealth == null || targetHealth.IsDead)
            {
                Debug.Log("[ArtilleryPhone] Mục tiêu đã bị phá huỷ, không mở câu hỏi (BR-38).");
                return;
            }

            var session = FieldQuizSession.Instance;
            if (session == null)
            {
                Debug.LogWarning("[ArtilleryPhone] Không tìm thấy FieldQuizSession trong scene.");
                return;
            }

            if (!session.TryOpen(3, 15f, OnResolved))
            {
                Debug.LogWarning("[ArtilleryPhone] Không thể mở câu hỏi chi viện pháo binh.");
                return;
            }

            used = true;
        }

        void OnResolved(QuizResult r)
        {
            if (r.outcome == QuizOutcome.Correct)
            {
                StartCoroutine(StrikeAfterDelay(strikeDelay));
            }
            else
            {
                Debug.Log("[ArtilleryPhone] Pháo trượt.");
            }
        }

        IEnumerator StrikeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            Vector3 strikePos = targetPosition != null 
                ? targetPosition.position 
                : (targetHealth != null ? targetHealth.transform.position : transform.position);

            CampaignAudio.Effect("cannon_generic", strikePos);

            if (artilleryStrikePrefab != null)
            {
                Instantiate(artilleryStrikePrefab, strikePos, Quaternion.identity);
            }
            else CombatEffects.Explosion(strikePos, 2.5f);

            if (targetHealth != null)
            {
                int bunkersBefore = MissionController.Current?.Attempt != null 
                    ? MissionController.Current.Attempt.BunkersDestroyed 
                    : 0;
                bool wasDeadBefore = targetHealth.IsDead;

                targetHealth.TakeDamage(artilleryDamage, DamageType.Artillery, strikePos);

                if (!wasDeadBefore && targetHealth.IsDead)
                {
                    if (MissionController.Current != null && MissionController.Current.Attempt != null)
                    {
                        // Nếu Health.AnyDied chưa tăng (hoặc target không thuộc TargetKind.Bunker), tăng BunkersDestroyed
                        if (MissionController.Current.Attempt.BunkersDestroyed == bunkersBefore)
                        {
                            MissionController.Current.Attempt.BunkersDestroyed++;
                        }
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            Vector3? dest = null;
            if (targetPosition != null) dest = targetPosition.position;
            else if (targetHealth != null) dest = targetHealth.transform.position;

            if (dest.HasValue)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, dest.Value);
                Gizmos.DrawWireSphere(dest.Value, 0.5f);
            }
        }
    }
}
