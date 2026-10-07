using System;
using System.Collections.Generic;

namespace DBP.Upgrades
{
    public enum UpgradeBranch { AmmoReserve, ReloadSpeed, Stamina, GrenadeCapacity }

    public enum BuyResult { Ok, UnknownId, AlreadyOwned, MissingPrerequisite, NotEnoughResolve, NotInBunker, SaveFailed }

    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public UpgradeBranch branch;
        public int level;
        public int cost;
        public string requires;
    }

    /// Bảng nâng cấp BR-35: 4 nhánh × 3 cấp, giá 100/200/300. Giá trị thử nghiệm, cân bằng ở T65.
    public static class UpgradeCatalog
    {
        static readonly string[] Codes = { "AMMO", "RELOAD", "STAMINA", "GRENADE" };
        public static readonly IReadOnlyList<UpgradeDef> All = Build();

        static List<UpgradeDef> Build()
        {
            var list = new List<UpgradeDef>();
            for (int b = 0; b < Codes.Length; b++)
                for (int lv = 1; lv <= 3; lv++)
                    list.Add(new UpgradeDef
                    {
                        id = Id((UpgradeBranch)b, lv),
                        branch = (UpgradeBranch)b,
                        level = lv,
                        cost = 100 * lv,
                        requires = lv > 1 ? Id((UpgradeBranch)b, lv - 1) : null,
                    });
            return list;
        }

        public static string Id(UpgradeBranch branch, int level) => $"U-{Codes[(int)branch]}-{level}";

        public static UpgradeDef Get(string id)
        {
            foreach (var u in All) if (u.id == id) return u;
            return null;
        }
    }

    /// Hiệu lực nâng cấp lúc xuất trận. Cấp hiện tại thay thế cấp trước, không cộng dồn (BR-35).
    public struct PlayerModifiers
    {
        public int ammoReservePercent; // 100 / 110 / 120 / 130
        public float reloadMultiplier; // 1 / 0,9 / 0,8 / 0,7
        public float staminaMultiplier; // 1 / 1,1 / 1,2 / 1,3
        public int grenadeBonus;        // 0 / 1 / 2 / 3

        public static PlayerModifiers FromLevels(int ammo, int reload, int stamina, int grenade) => new PlayerModifiers
        {
            ammoReservePercent = 100 + 10 * ammo,
            reloadMultiplier = 1f - 0.1f * reload,
            staminaMultiplier = 1f + 0.1f * stamina,
            grenadeBonus = grenade,
        };
    }

    public static class UpgradeService
    {
        /// Mua trong hầm trước khi ra trận (BR-35). Thiếu điểm / thiếu cấp trước / đã có → từ chối, không trừ điểm.
        /// Thành công: trừ điểm và ghi nâng cấp trên cùng đối tượng; người gọi lưu hồ sơ một lần.
        public static BuyResult TryBuy(ProfileData profile, string id, bool inBunker)
        {
            if (!inBunker) return BuyResult.NotInBunker;
            var upgrade = UpgradeCatalog.Get(id);
            if (upgrade == null) return BuyResult.UnknownId;
            if (profile.upgrades.Contains(id)) return BuyResult.AlreadyOwned;
            if (upgrade.requires != null && !profile.upgrades.Contains(upgrade.requires)) return BuyResult.MissingPrerequisite;
            if (profile.resolve < upgrade.cost) return BuyResult.NotEnoughResolve;

            profile.resolve -= upgrade.cost;
            profile.upgrades.Add(id);
            return BuyResult.Ok;
        }

        public static int Level(ProfileData profile, UpgradeBranch branch)
        {
            int level = 0;
            foreach (var id in profile.upgrades)
            {
                var u = UpgradeCatalog.Get(id);
                if (u != null && u.branch == branch && u.level > level) level = u.level;
            }
            return level;
        }

        public static PlayerModifiers Modifiers(ProfileData profile) => PlayerModifiers.FromLevels(
            Level(profile, UpgradeBranch.AmmoReserve),
            Level(profile, UpgradeBranch.ReloadSpeed),
            Level(profile, UpgradeBranch.Stamina),
            Level(profile, UpgradeBranch.GrenadeCapacity));
    }
}
