using DBP.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Cho model lính (T32) đứng thở / đi / chạy theo tốc độ thật của lính; khi chết thì gục ngã xuống đất
    /// (không máu me – BR-21) trước khi EnemyDeathHandler làm mờ.
    /// Gắn trên model con của lính (có Animator dùng EnemySoldier.controller hoặc AlliedSoldier.controller).
    /// Từ 29/10 điều khiển thêm các động tác (ProjectWiring.Animations): tự ngồi nấp / trèo thang / trúng đạn / gục,
    /// còn bắn, đâm, đấm, ném, chỉ huy, hô hào thì AI gọi Play(this, "Tên").
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        static readonly int Speed = Animator.StringToHash("Speed");
        static readonly int CrouchHash = Animator.StringToHash("Crouch"), ClimbHash = Animator.StringToHash("Climb");
        static readonly int HitHash = Animator.StringToHash("Hit"), DeadHash = Animator.StringToHash("Dead");

        /// Phát một động tác (trigger) trên model của lính, nếu controller có động tác đó.
        public static void Play(Component soldier, string trigger)
        {
            var d = soldier ? soldier.GetComponentInChildren<EnemyAnimatorDriver>() : null;
            if (d) d.Trigger(Animator.StringToHash(trigger));
        }

        readonly System.Collections.Generic.HashSet<int> parameters = new System.Collections.Generic.HashSet<int>();
        LadderClimber climber;
        SoldierAI soldier;
        TrenchSoldierAI trench;
        DBP.Player.FirstPersonController playerBody;
        float nextHitAt;

        Animator animator;
        NavMeshAgent agent;
        Health health;
        ComradeFollower comrade;
        Vector3 lastPosition;
        float fallTime = -1f;
        Quaternion standRotation, fallRotation;
        Vector3 standPosition;
        const float FallSeconds = .75f;
        [Tooltip("The death clip already animates the complete fall; skip the legacy transform fall.")]
        public bool useAuthoredDeathAnimation;
        float previousSpeed;

        void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponentInParent<NavMeshAgent>();
            health = GetComponentInParent<Health>();
            comrade = GetComponentInParent<ComradeFollower>();
            climber = GetComponentInParent<LadderClimber>();
            soldier = GetComponentInParent<SoldierAI>();
            trench = GetComponentInParent<TrenchSoldierAI>();
            playerBody = GetComponentInParent<DBP.Player.FirstPersonController>();
            foreach (var p in animator.parameters) parameters.Add(p.nameHash);
            lastPosition = transform.position;
            // Lệch nhịp để cả nhóm lính không thở cùng một lúc
            animator.Play(0, 0, Random.value);
        }

        void OnEnable()
        {
            if (health) health.Damaged += OnHurt;
            ComradeFollower.AnyHurt += OnAllyHurt;
        }

        void OnDisable()
        {
            if (health) health.Damaged -= OnHurt;
            ComradeFollower.AnyHurt -= OnAllyHurt;
        }

        void OnHurt(float _) { if (!health.IsDead) Flinch(); }
        void OnAllyHurt(ComradeFollower c, float _, bool __) { if (c == comrade && !c.IsDead) Flinch(); }

        void Flinch()
        {
            if (Time.time < nextHitAt) return;
            nextHitAt = Time.time + .5f;
            Trigger(HitHash);
        }

        void Trigger(int hash) { if (parameters.Contains(hash) && animator.isActiveAndEnabled) animator.SetTrigger(hash); }
        void SetBool(int hash, bool on) { if (parameters.Contains(hash)) animator.SetBool(hash, on); }

        void Update()
        {
            if (health && health.IsDead || comrade && comrade.IsDead)
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
            if (speed > .15f && previousSpeed <= .15f)
                Trigger(Animator.StringToHash("StartWalking"));
            previousSpeed = speed;
            SetBool(ClimbHash, climber && climber.IsClimbing || playerBody && playerBody.IsClimbing);
            SetBool(CrouchHash, playerBody ? playerBody.IsCrouching
                : comrade ? comrade.IsCrouching
                : trench ? trench.CurrentState == TrenchSoldierAI.State.HaoCover
                : soldier && soldier.CurrentState == SoldierAI.State.Suppress);
        }

        void Fall()
        {
            if (fallTime < 0f)
            {
                fallTime = 0f;
                if (parameters.Contains(Animator.StringToHash("StartWalking")))
                    animator.ResetTrigger("StartWalking");
                // Có động tác gục thì khuỵu gối trong lúc đổ; không có thì đứng hình như cũ
                if (parameters.Contains(DeadHash)) animator.SetTrigger(DeadHash);
                else animator.speed = 0f;
                if (useAuthoredDeathAnimation)
                {
                    for (int layer = 1; layer < animator.layerCount; layer++) animator.SetLayerWeight(layer, 0f);
                }
                standRotation = transform.localRotation;
                standPosition = transform.localPosition;
                // Ngã ngửa hoặc gục về trước, hơi xoay người để các xác không nằm thẳng hàng
                float pitch = Random.value < .6f ? -90f : 90f;
                fallRotation = Quaternion.Euler(pitch, Random.Range(-35f, 35f), Random.Range(-12f, 12f)) * standRotation;
            }
            if (useAuthoredDeathAnimation) return;
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
