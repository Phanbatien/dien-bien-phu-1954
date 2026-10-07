using System;
using UnityEngine;

namespace DBP.Core
{
    public enum DamageType { Bullet, Explosive, Artillery, AntiTank }
    public enum TargetKind { Player, Infantry, Bunker, MachineGunNest, Tank }

    /// Hợp đồng chung: mọi thứ nhận sát thương (người chơi, lính, lô cốt, xe tăng).
    public interface IDamageable
    {
        void TakeDamage(float amount, DamageType type, Vector3 point);
    }

    /// Máu dùng chung. Lô cốt/xe tăng khai báo immuneTo = Bullet (BR-20).
    /// AI (T07/T08 – Đạt) đặt Retreated khi lính rút khỏi khu vực giao chiến.
    public class Health : MonoBehaviour, IDamageable
    {
        /// Bắn ra mỗi khi bất kỳ Health nào chết: MissionController dùng để đếm địch hạ, lô cốt phá (BR-24).
        public static event Action<Health> AnyDied;

        [SerializeField] float maxHealth = 100f;
        [SerializeField] TargetKind kind = TargetKind.Infantry;
        [Tooltip("Loại sát thương không có tác dụng. Lô cốt: Bullet (BR-20).")]
        [SerializeField] DamageType[] immuneTo = Array.Empty<DamageType>();

        bool initialized;
        float current;

        public float Max => maxHealth;
        public float Current { get { Init(); return current; } }
        public TargetKind Kind => kind;
        public bool IsDead => Current <= 0f;
        public bool Retreated { get; set; }
        /// Đã bị hạ hoặc đã rút: dùng cho đợt phản kích và chiếm hào (BR-20, BR-32).
        public bool Neutralized => IsDead || Retreated;
        /// Tổng sát thương thực nhận; hồi máu không xóa (BR-24 tiêu chí 4).
        public float TotalDamageTaken { get; private set; }

        public event Action<float> Damaged;
        public event Action Died;

        void Awake() => Init();

        void Init()
        {
            if (initialized) return;
            initialized = true;
            current = maxHealth;
        }

        public void Configure(TargetKind newKind, float max, params DamageType[] immune)
        {
            kind = newKind;
            maxHealth = max;
            immuneTo = immune ?? Array.Empty<DamageType>();
            initialized = true;
            current = max;
        }

        public bool IsImmuneTo(DamageType type) => Array.IndexOf(immuneTo, type) >= 0;

        public void TakeDamage(float amount, DamageType type, Vector3 point)
        {
            Init();
            if (current <= 0f || amount <= 0f || IsImmuneTo(type)) return;
            float applied = Mathf.Min(amount, current);
            current -= applied;
            TotalDamageTaken += applied;
            Damaged?.Invoke(applied);
            if (current <= 0f)
            {
                Died?.Invoke();
                AnyDied?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            Init();
            if (current > 0f) current = Mathf.Min(maxHealth, current + amount);
        }
    }
}
