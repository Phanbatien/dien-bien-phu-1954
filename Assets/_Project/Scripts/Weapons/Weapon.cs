namespace DBP.Weapons
{
    /// Trạng thái một khẩu súng trong lượt chơi: đạn trong băng và đạn dự trữ (T06).
    public class Weapon
    {
        public WeaponDef Def { get; }
        public int InMag { get; private set; }
        public int Reserve { get; private set; }
        /// Trần đạn dự trữ = gốc × % nâng cấp, làm tròn xuống (BR-35).
        public int ReserveCap { get; }

        public Weapon(WeaponDef def, int reservePercent = 100, int startReserve = 0)
        {
            Def = def;
            ReserveCap = def.stats.reserve * reservePercent / 100;
            InMag = def.stats.mag;
            AddAmmo(startReserve);
        }

        public bool CanFire => InMag > 0;
        public bool CanReload => InMag < Def.stats.mag && Reserve > 0;

        public bool TryConsume()
        {
            if (InMag <= 0) return false;
            InMag--;
            return true;
        }

        public void CompleteReload()
        {
            int take = System.Math.Min(Def.stats.mag - InMag, Reserve);
            InMag += take;
            Reserve -= take;
        }

        /// Thêm đạn dự trữ tới trần; trả về phần dư (để lại ở điểm nhận, BR-36).
        public int AddAmmo(int amount)
        {
            if (amount <= 0) return 0;
            int add = System.Math.Min(System.Math.Max(0, ReserveCap - Reserve), amount);
            Reserve += add;
            return amount - add;
        }

        /// Tổng đạn đang có (dùng khi đổi súng trùng loại thành đạn).
        public int TotalAmmo => InMag + Reserve;
    }
}
