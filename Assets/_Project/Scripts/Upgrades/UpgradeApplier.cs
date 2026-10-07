using DBP.Player;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Upgrades
{
    /// Áp nâng cấp đã mua vào người chơi lúc xuất trận (T38; BR-35). Gắn trên Player.
    /// Chạy ở Awake để có hiệu lực trước khi phát súng khởi đầu.
    [DefaultExecutionOrder(-100)]
    public class UpgradeApplier : MonoBehaviour
    {
        [SerializeField] int baseGrenadeCapacity = 2;

        void Awake()
        {
            var mods = UpgradeService.Modifiers(ProfileService.Data);
            var weapons = GetComponentInChildren<WeaponController>();
            if (weapons)
            {
                weapons.Inventory.ReservePercent = mods.ammoReservePercent;
                weapons.Inventory.GrenadeCapacity = baseGrenadeCapacity + mods.grenadeBonus;
                weapons.ReloadMultiplier = mods.reloadMultiplier;
            }
            if (TryGetComponent<FirstPersonController>(out var body))
                body.SetStaminaMultiplier(mods.staminaMultiplier);
        }
    }
}
