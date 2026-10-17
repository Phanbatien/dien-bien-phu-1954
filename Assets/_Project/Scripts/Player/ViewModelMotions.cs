using DBP.Weapons;
using UnityEngine;

namespace DBP.Player
{
    /// Hoạt ảnh súng góc nhìn thứ nhất (T16): giật khi bắn, nạp khớp thời gian thay đạn, nhấp khi đi.
    /// Tư thế nghỉ giữ nguyên nòng theo camera. Nạp dùng ReloadProgress01 nên hết đúng lúc đạn vào băng
    /// (reloadSec × hệ số nâng cấp, Mosin 3,2 giây).
    [RequireComponent(typeof(FirstPersonArms))]
    public class ViewModelMotions : MonoBehaviour
    {
        public const float ReloadBoltDegrees = 18f;

        [SerializeField] float firePitch = 7f;
        [SerializeField] float kickRecover = 10f;
        [SerializeField] float walkBob = 0.012f;

        FirstPersonArms arms;
        WeaponController weapons;
        CharacterController body;
        float kick;

        void Awake() => arms = GetComponent<FirstPersonArms>();

        void OnEnable() => WeaponController.Fired += OnFired;

        void OnDisable() => WeaponController.Fired -= OnFired;

        void OnFired()
        {
            if (!isActiveAndEnabled) return;
            if (!weapons) weapons = GetComponentInParent<WeaponController>();
            if (weapons) kick = 1f;
        }

        void LateUpdate()
        {
            if (!arms || !arms.rifle) return;
            if (!weapons) weapons = GetComponentInParent<WeaponController>();
            if (!body) body = GetComponentInParent<CharacterController>();

            float reload = weapons && weapons.IsReloading ? weapons.ReloadProgress01 : 0f;
            kick = Mathf.MoveTowards(kick, 0f, kickRecover * Time.deltaTime);

            float speed = 0f;
            if (body)
            {
                var v = body.velocity;
                speed = new Vector3(v.x, 0f, v.z).magnitude;
            }
            float bob = 0f;
            if (speed > 0.2f)
            {
                float rate = speed > 5f ? 12f : 8f;
                bob = Mathf.Sin(Time.time * rate) * walkBob * Mathf.Clamp01(speed / 4f);
            }

            var rest = arms.gripLocalRotation;
            if (rest.x == 0f && rest.y == 0f && rest.z == 0f && rest.w == 0f)
                rest = Quaternion.identity;

            arms.rifle.localPosition = arms.gripLocalPosition + new Vector3(0f, bob, 0f);
            arms.rifle.localRotation = rest * Quaternion.Euler(-kick * firePitch, 0f, ReloadBolt(reload));
        }

        /// 0 ở đầu và cuối khoảng nạp, đỉnh ở giữa. Progress 0→1 là đúng khoảng thay đạn của WeaponController.
        public static float ReloadBolt(float progress01) =>
            Mathf.Sin(Mathf.Clamp01(progress01) * Mathf.PI) * ReloadBoltDegrees;
    }
}
