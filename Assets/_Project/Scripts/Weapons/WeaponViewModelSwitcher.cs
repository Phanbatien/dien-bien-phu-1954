using DBP.Player;
using UnityEngine;

namespace DBP.Weapons
{
    /// Đổi bộ tay + súng trên tay theo súng đang cầm (T55): súng có ống ngắm hiện súng ngắm, súng liên thanh
    /// (MAT-49, Kiểu 50) hiện tiểu liên, còn lại hiện súng trường. Gắn trên gốc mô hình tay góc nhìn thứ nhất.
    public class WeaponViewModelSwitcher : MonoBehaviour
    {
        public GameObject rifleModel;
        public GameObject smgModel;

        WeaponController weapons;
        FirstPersonArms arms;

        void Awake()
        {
            weapons = GetComponentInParent<WeaponController>();
            arms = GetComponent<FirstPersonArms>();
            if (smgModel) smgModel.SetActive(false); // tiểu liên cũ đã có trong bộ tay mới
        }

        void Update()
        {
            var current = weapons ? weapons.Inventory.Current : null;
            if (current == null || !arms) return;
            arms.ShowModel(current.Def.id == "W-BAZOOKA" ? "VM_Bazooka" : current.Def.stats.scopeZoom > 1f ? "VM_Scoped" : current.Def.Automatic ? "VM_Smg" : "VM_Rifle");
        }
    }
}
