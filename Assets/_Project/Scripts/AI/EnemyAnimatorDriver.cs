using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Cho model lính (T32) đứng thở / đi / chạy theo tốc độ thật của lính; dừng hoạt ảnh khi chết.
    /// Gắn trên model con của lính (có Animator dùng EnemySoldier.controller).
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");

        Animator animator;
        NavMeshAgent agent;
        Health health;
        Vector3 lastPosition;

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
                animator.speed = 0f;
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

        // Clip đi/chạy mượn từ Starter Assets có sự kiện bước chân; Long có thể phát tiếng bước bùn ở đây.
        void OnFootstep(AnimationEvent animationEvent) { }
        void OnLand(AnimationEvent animationEvent) { }
    }
}
