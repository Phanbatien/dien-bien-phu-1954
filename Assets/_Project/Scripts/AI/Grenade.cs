using System.Collections;
using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// Lựu đạn chày nổ theo vùng (T14 – Đạt; BR-20).
    /// Ném G hoặc chạy bằng script: khi chạm vật thể hoặc hết fuse → nổ → sát thương theo bán kính → đẩy Rigidbody.
    /// Sát thương giảm tuyến tính từ tâm ra ngoài; không gây sát thương cho lô cốt (immuneTo Explosive = false theo mặc định).
    [RequireComponent(typeof(Rigidbody))]
    public class Grenade : MonoBehaviour
    {
        [Header("Nổ")]
        [Tooltip("Bán kính nổ (m).")]
        [SerializeField] float blastRadius = 6f;
        [Tooltip("Sát thương tối đa tại tâm nổ.")]
        [SerializeField] float maxDamage = 80f;
        [Tooltip("Sát thương tối thiểu ở rìa bán kính (có thể = 0).")]
        [SerializeField] float minDamage = 5f;
        [Tooltip("Thời gian fuse (giây). 0 = nổ khi chạm.")]
        [SerializeField] float fuseSec = 3.5f;
        [Tooltip("Lực đẩy vật lý tại tâm (áp dụng cho Rigidbody).")]
        [SerializeField] float blastForce = 450f;
        [SerializeField] LayerMask damageMask = ~0;

        [Header("VFX / SFX")]
        [SerializeField] GameObject explosionPrefab; // T67 – Khôi gán sau
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip explosionClip;

        bool exploded;
        Rigidbody rb;

        void Awake() => rb = GetComponent<Rigidbody>();

        /// Lựu đạn chưa nổ: lính địch thấy thì chạy tránh.
        public static readonly System.Collections.Generic.List<Grenade> Live = new System.Collections.Generic.List<Grenade>();
        public float BlastRadius => blastRadius;
        /// Bộc phá: bán kính và cỡ nổ nhân power, ngòi ngắn hơn. Gọi ngay sau Instantiate.
        public void Boost(float power)
        {
            blastRadius *= power; blastForce *= power; // nổ rộng hơn nhưng không mạnh hơn: đứng sát vẫn sống nếu đầy máu
            fuseSec = Mathf.Min(fuseSec, 2.2f);
            boost = power;
            transform.localScale *= 1.6f;
        }
        float boost = 1f;
        /// Lựu đạn của địch: lính địch chết vì nó không tính công người chơi.
        public bool CreditPlayer { get; set; } = true;
        void OnEnable() => Live.Add(this);
        void OnDisable() => Live.Remove(this);

        void Start()
        {
            if (fuseSec > 0f)
                Invoke(nameof(Explode), fuseSec);
        }

        void OnCollisionEnter(Collision _)
        {
            // Nổ khi chạm nếu không có fuse
            if (fuseSec <= 0f) Explode();
        }

        [ContextMenu("Explode Now")]
        public void Explode()
        {
            if (exploded) return;
            exploded = true;
            Live.Remove(this);
            CancelInvoke(nameof(Explode));

            // VFX
            if (explosionPrefab)
                Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 5f);
            else CombatEffects.Explosion(transform.position, boost);

            // SFX
            if (explosionClip)
            {
                // Phát âm thanh ở điểm nổ độc lập với GameObject (sẽ bị Destroy ngay sau)
                AudioSource.PlayClipAtPoint(explosionClip, transform.position);
            }
            else CampaignAudio.Effect("explosion_02", transform.position);

            // Tìm mọi IDamageable trong bán kính
            var hits = Physics.OverlapSphere(transform.position, blastRadius, damageMask, QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null) continue;

                float dist  = Vector3.Distance(transform.position, col.bounds.center);
                float t     = Mathf.Clamp01(dist / blastRadius);
                float amount = Mathf.Lerp(maxDamage, minDamage, t); // tuyến tính giảm dần

                // Kiểm tra che khuất (line-of-sight từ tâm nổ đến tâm collider)
                var dir = col.bounds.center - transform.position;
                if (dir.sqrMagnitude > 0.01f &&
                    Physics.Raycast(transform.position, dir.normalized, out var hitInfo, dir.magnitude, damageMask, QueryTriggerInteraction.Ignore))
                {
                    // Nếu có vật cản dày (không phải chính collider đó) thì giảm 60%
                    if (hitInfo.collider != col) amount *= 0.4f;
                }

                if (!CreditPlayer && dmg is Health h) h.TakeAllyDamage(amount, DamageType.Explosive, col.bounds.center);
                else dmg.TakeDamage(amount, DamageType.Explosive, col.bounds.center);

                // Đẩy Rigidbody (lính ngã, vật bay)
                if (col.attachedRigidbody)
                    col.attachedRigidbody.AddExplosionForce(blastForce, transform.position, blastRadius, 1f, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }

        // Hiển thị vùng nổ trong Editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, blastRadius);
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }
}
