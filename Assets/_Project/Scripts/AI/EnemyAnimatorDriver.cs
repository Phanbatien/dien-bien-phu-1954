using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Cho model lính (T32) đứng thở / đi / chạy theo tốc độ thật của lính; khi chết thì gục ngã xuống đất
    /// (không máu me – BR-21) trước khi EnemyDeathHandler làm mờ.
    /// Gắn trên model con của lính (có Animator dùng EnemySoldier.controller).
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");

        Animator animator;
        NavMeshAgent agent;
        Health health;
        Vector3 lastPosition;
        float fallTime = -1f;
        Quaternion standRotation, fallRotation;
        Vector3 standPosition;
        const float FallSeconds = .75f;

        void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponentInParent<NavMeshAgent>();
            health = GetComponentInParent<Health>();
            lastPosition = transform.position;
            // Lệch nhịp để cả nhóm lính không thở cùng một lúc
            animator.Play(0, 0, Random.value);
        }

        void Update()
        {
            if (health && health.IsDead)
            {
                Fall();
                return;
            }
            float speed;
            if (agent && agent.enabled) speed = agent.velocity.magnitude;
            else
            {
                speed = Time.deltaTime > 0f ? (transform.position - lastPosition).magnitude / Time.deltaTime : 0f;
                lastPosition = transform.position;
            }
            animator.SetFloat(Speed, speed, 0.15f, Time.deltaTime);
        }

        void Fall()
        {
            if (fallTime < 0f)
            {
                fallTime = 0f;
                animator.speed = 0f;
                standRotation = transform.localRotation;
                standPosition = transform.localPosition;
                // Ngã ngửa hoặc gục về trước, hơi xoay người để các xác không nằm thẳng hàng
                float pitch = Random.value < .6f ? -90f : 90f;
                fallRotation = Quaternion.Euler(pitch, Random.Range(-35f, 35f), Random.Range(-12f, 12f)) * standRotation;
            }
            if (fallTime >= FallSeconds) return;
            fallTime += Time.deltaTime;
            float t = Mathf.Clamp01(fallTime / FallSeconds);
            // Khuỵu gối trước (chậm), rồi đổ nhanh dần như bị trọng lực kéo, nảy nhẹ khi chạm đất
            float eased = t * t * t;
            if (t >= 1f) eased = 1f;
            else if (t > .85f) eased = 1f + Mathf.Sin((t - .85f) / .15f * Mathf.PI) * -.04f;
            transform.localRotation = Quaternion.SlerpUnclamped(standRotation, fallRotation, eased);
            transform.localPosition = standPosition + Vector3.down * (.25f * Mathf.Sin(t * Mathf.PI * .5f)) + Vector3.up * (.18f * t);
        }

        // Clip đi/chạy mượn từ Starter Assets có sự kiện bước chân; Long có thể phát tiếng bước bùn ở đây.
        void OnFootstep(AnimationEvent animationEvent) { }
        void OnLand(AnimationEvent animationEvent) { }
    }
}
