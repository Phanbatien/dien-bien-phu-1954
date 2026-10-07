using System;
using System.Collections.Generic;

namespace DBP.Weapons
{
    /// Túi đồ trong một lượt chơi (T21, BR-36, BR-19):
    /// tối đa 2 súng cá nhân; lựu đạn ngăn riêng; đồ nhặt không mang sang lượt khác.
    public class Inventory
    {
        public const int MaxWeapons = 2;

        public struct GiveResult
        {
            /// Súng bị thay ra khi túi đầy: để lại ở điểm tương tác, nhặt lại không qua câu hỏi.
            public Weapon Dropped;
            /// Đạn không vừa túi: để lại ở điểm nhận.
            public int LeftoverAmmo;
            /// Nhận súng trùng loại đang mang: đổi thành đạn, không tạo bản sao.
            public bool ConvertedToAmmo;
        }

        readonly List<Weapon> weapons = new List<Weapon>();

        public IReadOnlyList<Weapon> Weapons => weapons;
        public int CurrentIndex { get; private set; }
        public Weapon Current => weapons.Count > 0 ? weapons[CurrentIndex] : null;
        /// % trần đạn dự trữ theo nâng cấp (100 = gốc).
        public int ReservePercent { get; set; } = 100;
        public int Grenades { get; private set; }
        public int GrenadeCapacity { get; set; } = 2;

        public event Action Changed;

        /// Nhận súng mới từ hòm/súng rơi. ammo = lượng đạn khai báo cho phần thưởng.
        public GiveResult GiveWeapon(WeaponDef def, int ammo)
        {
            var same = Find(def.id);
            if (same != null)
                return Notify(new GiveResult { ConvertedToAmmo = true, LeftoverAmmo = same.AddAmmo(ammo) });

            var weapon = new Weapon(def, ReservePercent);
            int leftover = weapon.AddAmmo(ammo);
            var result = Equip(weapon);
            result.LeftoverAmmo = leftover;
            return Notify(result);
        }

        /// Nhặt lại khẩu súng đã thuộc sở hữu trong lượt: giữ nguyên số đạn, không tạo thêm (BR-36).
        public GiveResult TakeBack(Weapon weapon)
        {
            var same = Find(weapon.Def.id);
            if (same != null)
                return Notify(new GiveResult { ConvertedToAmmo = true, LeftoverAmmo = same.AddAmmo(weapon.TotalAmmo) });
            return Notify(Equip(weapon));
        }

        /// Thêm đạn cho mọi súng cùng loại đạn; trả về phần dư.
        public int AddAmmo(string ammoType, int amount)
        {
            foreach (var w in weapons)
            {
                if (amount <= 0) break;
                if (w.Def.ammoType == ammoType) amount = w.AddAmmo(amount);
            }
            Changed?.Invoke();
            return amount;
        }

        public int AddGrenades(int count)
        {
            int add = Math.Min(Math.Max(0, GrenadeCapacity - Grenades), Math.Max(0, count));
            Grenades += add;
            Changed?.Invoke();
            return count - add;
        }

        public bool UseGrenade()
        {
            if (Grenades <= 0) return false;
            Grenades--;
            Changed?.Invoke();
            return true;
        }

        public void Switch(int index)
        {
            if (index < 0 || index >= weapons.Count || index == CurrentIndex) return;
            CurrentIndex = index;
            Changed?.Invoke();
        }

        public void Next()
        {
            if (weapons.Count > 1) Switch((CurrentIndex + 1) % weapons.Count);
        }

        Weapon Find(string id) => weapons.Find(w => w.Def.id == id);

        GiveResult Equip(Weapon weapon)
        {
            var result = new GiveResult();
            if (weapons.Count < MaxWeapons)
            {
                weapons.Add(weapon);
                CurrentIndex = weapons.Count - 1;
            }
            else
            {
                result.Dropped = weapons[CurrentIndex]; // thay khẩu đang cầm
                weapons[CurrentIndex] = weapon;
            }
            return result;
        }

        GiveResult Notify(GiveResult result)
        {
            Changed?.Invoke();
            return result;
        }
    }
}
