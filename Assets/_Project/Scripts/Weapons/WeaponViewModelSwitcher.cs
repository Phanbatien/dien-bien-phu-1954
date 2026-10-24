using UnityEngine;

namespace DBP.Weapons
{
    /// Đổi mô hình súng trên tay theo súng đang cầm (T55): súng liên thanh (MAT-49, Kiểu 50) hiện tiểu liên,
    /// còn lại hiện súng trường. Gắn trên gốc mô hình tay góc nhìn thứ nhất.
    public class WeaponViewModelSwitcher : MonoBehaviour
    {
        public GameObject rifleModel;
        public GameObject smgModel;

        WeaponController weapons;

        void Awake() => weapons = GetComponentInParent<WeaponController>();

        void Update()
        {
            var current = weapons ? weapons.Inventory.Current : null;
            bool smg = current != null && current.Def.Automatic;
            if (rifleModel && rifleModel.activeSelf == smg) rifleModel.SetActive(!smg);
            if (smgModel && smgModel.activeSelf != smg) smgModel.SetActive(smg);
        }
    }
}
