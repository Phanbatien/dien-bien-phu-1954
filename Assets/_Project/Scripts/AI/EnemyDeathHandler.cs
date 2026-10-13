using System.Collections;
using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// T08 – Đạt; BR-21:
    /// Gắn kèm Health: khi chết → kích hoạt hoạt ảnh ngã → chờ → mờ dần → Destroy.
    /// Không hiển thị máu me. Hiệu ứng biến mất không tạo thêm lượt ghi nhận hạ địch (Health.AnyDied đã bắn trước).
    /// Dùng cho lính, lính gác, tổ súng máy — bất kỳ kẻ địch nào cần hiệu ứng chết.
    [RequireComponent(typeof(Health))]
    public class EnemyDeathHandler : MonoBehaviour
    {
        [Header("Hoạt ảnh")]
        [Tooltip("Animator chứa trigger 'Dead'. Để trống nếu dùng ragdoll.")]
        [SerializeField] Animator animator;
        static readonly int AnimDead = Animator.StringToHash("Dead");

        [Header("Ragdoll (thay thế Animator)")]
        [Tooltip("Bật ragdoll khi chết thay vì Animator. Yêu cầu Rigidbody trên xương.")]
        [SerializeField] bool useRagdoll;
        [SerializeField] Rigidbody[] ragdollBones;

        [Header("Thời gian")]
        [Tooltip("Giây chờ trước khi bắt đầu mờ (cho hoạt ảnh ngã chạy xong).")]
        [SerializeField] float waitBeforeFade = 2f;
        [Tooltip("Giây mờ dần (alpha 1 → 0).")]
        [SerializeField] float fadeDuration = 1.5f;

        [Header("Vật lý")]
        [Tooltip("Tắt NavMeshAgent và Collider khi chết để không chặn đường.")]
        [SerializeField] bool disableCollidersOnDeath = true;

        Health health;
        bool dying;

        void Awake() => health = GetComponent<Health>();

        void OnEnable()  => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            if (dying) return;
            dying = true;
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            // 1. Tắt di chuyển & va chạm
            if (disableCollidersOnDeath)
            {
                var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent) navAgent.enabled = false;

                foreach (var col in GetComponentsInChildren<Collider>())
                    col.enabled = false;
            }

            // 2a. Ragdoll
            if (useRagdoll && ragdollBones != null && ragdollBones.Length > 0)
            {
                if (animator) animator.enabled = false;
                foreach (var rb in ragdollBones)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
            // 2b. Animator death trigger
            else if (animator)
            {
                animator.SetTrigger(AnimDead);
            }

            // 3. Chờ hoạt ảnh ngã
            yield return new WaitForSeconds(waitBeforeFade);

            // 4. Chuyển materials sang Transparent để fade
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                    EnableAlpha(mat);
            }

            // 5. Mờ dần
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                foreach (var rend in renderers)
                    foreach (var mat in rend.materials)
                    {
                        var c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                yield return null;
            }

            Destroy(gameObject);
        }

        /// Chuyển material Standard Shader sang chế độ Transparent.
        static void EnableAlpha(Material mat)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }
}
