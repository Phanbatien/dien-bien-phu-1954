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
        /// Mỗi lần trúng đòn: mục tiêu, sát thương thực nhận, điểm trúng, có phải chí mạng (để hiện số sát thương).
        public static event Action<Health, float, Vector3, bool> AnyDamaged;

        /// Trúng đầu: điểm trúng nằm trong 0,38 m trên cùng của thân lính (chí mạng ×2).
        public static bool IsHeadshot(Health h, Vector3 point)
        {
            if (!h || h.Kind != TargetKind.Infantry) return false;
            var col = h.GetComponent<Collider>();
            return col && point.y >= col.bounds.max.y - 0.38f;
        }

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

        /// Cú đánh cuối cùng là của đồng đội (AI phe ta): mục tiêu chết thì không tính công cho người chơi.
        public bool LastHitByAlly { get; private set; }

        /// Đồng đội gây sát thương: vẫn trừ máu bình thường nhưng không cộng vào thành tích người chơi.
        public void TakeAllyDamage(float amount, DamageType type, Vector3 point, bool crit = false)
        {
            if (current <= 0f && initialized) return;
            LastHitByAlly = true;
            ApplyDamage(amount, type, point, crit);
        }

        public void TakeDamage(float amount, DamageType type, Vector3 point) => TakeDamage(amount, type, point, false);

        public void TakeDamage(float amount, DamageType type, Vector3 point, bool crit)
        {
            LastHitByAlly = false;
            ApplyDamage(amount, type, point, crit);
        }

        /// Tỉ lệ né đạn (chỉ huy địch); né thì không mất máu và báo AnyDodged.
        public float DodgeChance { get; set; }
        public static event Action<Health, Vector3> AnyDodged;

        void ApplyDamage(float amount, DamageType type, Vector3 point, bool crit = false)
        {
            Init();
            if (current <= 0f || amount <= 0f || IsImmuneTo(type)) return;
            if (type == DamageType.Bullet && DodgeChance > 0f && UnityEngine.Random.value < DodgeChance) { AnyDodged?.Invoke(this, point); return; }
            float applied = Mathf.Min(amount, current);
            current -= applied;
            TotalDamageTaken += applied;
            Damaged?.Invoke(applied);
            AnyDamaged?.Invoke(this, applied, point, crit);
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
