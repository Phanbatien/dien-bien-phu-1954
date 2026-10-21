using System;
using System.Collections;
using DBP.Core;
using DBP.Player;
using UnityEngine;

namespace DBP.Missions
{
    /// <summary>
    /// Sự kiện kích nổ khối bộc phá 960kg đồi A1 trong Màn 5 (Task T59, BR-3):
    /// - Khi người chơi bước vào trigger zone (OnTriggerEnter) -> bắt đầu chuỗi sự kiện nổ A1.
    /// - Sau khoảng delay (mặc định 1s) -> kích hoạt VFX bộc phá, phát SFX nổ vang trời, rung chuyển màn hình.
    /// - TUYỆT ĐỐI KHÔNG phụ thuộc vào đáp án Quiz nào (BR-3: Nổ A1 luôn xảy ra khi tới đoạn tương ứng).
    /// - Kế thừa Objective để tích hợp trực tiếp vào danh sách mục tiêu bắt buộc của MissionController.
    /// - Bắn sự kiện tĩnh A1Exploded để cutscene hoặc kịch bản màn chơi lắng nghe.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExplosionA1Event : Objective
    {
        /// <summary>
        /// Sự kiện tĩnh phát ra ngay khi bộc phá A1 phát nổ.
        /// </summary>
        public static event Action A1Exploded;

        [Header("Hiệu ứng nổ A1 (BR-3)")]
        [Tooltip("Prefab VFX khói lửa, đất đá nổ dữ dội của khối bộc phá 960kg.")]
        [SerializeField] GameObject explosionVFXPrefab;
        [Tooltip("Âm thanh tiếng nổ trầm hùng rung chuyển đồi A1.")]
        [SerializeField] AudioClip explosionSound;
        [Tooltip("Thời gian chờ trước khi bộc phá phát nổ (giây).")]
        [SerializeField] float delay = 1f;
        [Tooltip("Tọa độ tâm vụ nổ. Nếu để trống sẽ lấy chính transform của trigger.")]
        [SerializeField] Transform explosionPoint;

        [Header("Rung màn hình (Camera Shake)")]
        [Tooltip("Thời gian rung màn hình (giây).")]
        [SerializeField] float shakeDuration = 2.5f;
        [Tooltip("Biên độ rung lắc của camera.")]
        [SerializeField] float shakeMagnitude = 0.35f;

        [Header("Âm thanh phát tại chỗ")]
        [SerializeField] AudioSource audioSource;

        bool triggered;
        bool exploded;
        Collider triggerCollider;

        void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider)
            {
                triggerCollider.isTrigger = true;
            }

            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (string.IsNullOrEmpty(Title))
            {
                Title = "Kích nổ khối bộc phá đồi A1";
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (triggered) return;

            // Kiểm tra đối tượng bước vào có phải người chơi không
            if (other.CompareTag("Player") || other.GetComponentInParent<FirstPersonController>() != null)
            {
                TriggerExplosion();
            }
        }

        /// <summary>
        /// Kích hoạt chuỗi nổ đồi A1 (có thể gọi từ code hoặc trigger).
        /// Hoàn toàn không phụ thuộc kết quả Quiz (BR-3).
        /// </summary>
        public void TriggerExplosion()
        {
            if (triggered) return;
            triggered = true;

            // Vô hiệu hóa collider để không trigger lần thứ 2
            if (triggerCollider)
                triggerCollider.enabled = false;

            StartCoroutine(ExplosionSequence());
        }

        IEnumerator ExplosionSequence()
        {
            // Chờ khoảng thời gian delay trước khi khối bộc phá kích nổ
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            Vector3 blastPosition = explosionPoint ? explosionPoint.position : transform.position;

            // 1. Sinh hiệu ứng nổ VFX
            if (explosionVFXPrefab)
            {
                var vfx = Instantiate(explosionVFXPrefab, blastPosition, Quaternion.identity);
                Destroy(vfx, 8f);
            }

            // 2. Phát âm thanh nổ SFX
            if (explosionSound)
            {
                if (audioSource)
                    audioSource.PlayOneShot(explosionSound);
                else
                    AudioSource.PlayClipAtPoint(explosionSound, blastPosition);
            }
            else CampaignAudio.Effect("explosion_01", blastPosition);

            // 3. Rung chuyển màn hình góc nhìn người chơi
            StartCoroutine(DoCameraShake());

            // 4. Đánh dấu đã nổ và phát sự kiện tĩnh
            exploded = true;
            A1Exploded?.Invoke();

            // 5. Cập nhật trạng thái Objective
            Evaluate();
        }

        IEnumerator DoCameraShake()
        {
            // Tìm FirstPersonController hoặc Camera chính
            var fpc = FindObjectOfType<FirstPersonController>();
            Transform targetTransform = null;
            Vector3 originalLocalPos = Vector3.zero;

            if (fpc && fpc.cameraPivot)
            {
                targetTransform = fpc.cameraPivot;
                originalLocalPos = targetTransform.localPosition;
            }
            else if (Camera.main)
            {
                targetTransform = Camera.main.transform;
                originalLocalPos = targetTransform.localPosition;
            }

            if (!targetTransform) yield break;

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / shakeDuration;
                // Giảm dần biên độ rung theo thời gian
                float currentMag = Mathf.Lerp(shakeMagnitude, 0f, progress);

                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * currentMag;
                targetTransform.localPosition = originalLocalPos + randomOffset;

                yield return null;
            }

            // Đưa camera về vị trí cân bằng
            targetTransform.localPosition = originalLocalPos;
        }

        // ── Kế thừa Objective ──────────────────────────────────────────────────
        protected override bool Check() => exploded;

        public override string Progress => exploded
            ? "Khối bộc phá 960kg đồi A1 đã phát nổ thành công!"
            : (triggered ? "Bộc phá đang đếm ngược kích nổ..." : "Tiến vào vị trí kích nổ đồi A1");
    }
}
