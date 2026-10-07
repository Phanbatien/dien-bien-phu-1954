using DBP.Interaction;
using DBP.Missions;
using DBP.Player;
using DBP.Quiz;
using DBP.Upgrades;
using DBP.Weapons;
using NUnit.Framework;

namespace DBP.Tests
{
    /// Kiểm tra logic các việc của trưởng nhóm. Chạy: Window → General → Test Runner → EditMode.
    public class LeaderSystemsTests
    {
        static WeaponDef Def(string id, string ammo, int mag = 5, int reserve = 30) =>
            new WeaponDef { id = id, name = id, ammoType = ammo, stats = new WeaponStats { mag = mag, reserve = reserve } };

        // ── T06: thay đạn ──
        [Test]
        public void Reload_TakesOnlyWhatMagNeeds()
        {
            var w = new Weapon(Def("W-MOSIN", "7.62x54R"), 100, 30);
            w.TryConsume(); w.TryConsume();
            w.CompleteReload();
            Assert.AreEqual(5, w.InMag);
            Assert.AreEqual(28, w.Reserve);
        }

        [Test]
        public void Reload_WithLittleReserve_PartiallyFills()
        {
            var w = new Weapon(Def("W-MOSIN", "7.62x54R"), 100, 1);
            for (int i = 0; i < 5; i++) w.TryConsume();
            Assert.IsFalse(w.CanFire);
            w.CompleteReload();
            Assert.AreEqual(1, w.InMag);
            Assert.AreEqual(0, w.Reserve);
            Assert.IsFalse(w.CanReload);
        }

        // ── T21: túi đồ (BR-36) ──
        [Test]
        public void Inventory_ThirdWeapon_ReplacesCurrentAndDropsIt()
        {
            var inv = new Inventory();
            inv.GiveWeapon(Def("A", "x"), 0);
            inv.GiveWeapon(Def("B", "y"), 0);
            var r = inv.GiveWeapon(Def("C", "z"), 0);
            Assert.AreEqual(2, inv.Weapons.Count);
            Assert.AreEqual("B", r.Dropped.Def.id);
            Assert.AreEqual("C", inv.Current.Def.id);
        }

        [Test]
        public void Inventory_DuplicateWeapon_BecomesAmmoWithLeftover()
        {
            var inv = new Inventory();
            inv.GiveWeapon(Def("A", "x", reserve: 30), 25);
            var r = inv.GiveWeapon(Def("A", "x", reserve: 30), 10);
            Assert.IsTrue(r.ConvertedToAmmo);
            Assert.AreEqual(1, inv.Weapons.Count);
            Assert.AreEqual(30, inv.Current.Reserve);
            Assert.AreEqual(5, r.LeftoverAmmo);
        }

        [Test]
        public void Inventory_TakeBack_CreatesNoNewAmmo()
        {
            var inv = new Inventory();
            inv.GiveWeapon(Def("A", "x"), 10);
            inv.GiveWeapon(Def("B", "y"), 10);
            var dropped = inv.GiveWeapon(Def("C", "z"), 10).Dropped; // B rơi ra, còn 5 + 10
            int before = dropped.TotalAmmo;
            var swap = inv.TakeBack(dropped);
            Assert.AreEqual("C", swap.Dropped.Def.id);
            Assert.AreEqual(before, inv.Current.TotalAmmo);
        }

        [Test]
        public void Inventory_Ammo_OnlyGoesToCompatibleWeapons()
        {
            var inv = new Inventory();
            inv.GiveWeapon(Def("A", "x", reserve: 30), 0);
            Assert.AreEqual(20, inv.AddAmmo("y", 20));
            Assert.AreEqual(0, inv.AddAmmo("x", 20));
            Assert.AreEqual(20, inv.Current.Reserve);
        }

        [Test]
        public void Inventory_GrenadesOverCapacity_StayBehind()
        {
            var inv = new Inventory { GrenadeCapacity = 2 };
            Assert.AreEqual(1, inv.AddGrenades(3));
            Assert.AreEqual(2, inv.Grenades);
        }

        // ── T38: nâng cấp (BR-35) ──
        [Test]
        public void Upgrade_Buy_DeductsAndRecords()
        {
            var p = new ProfileData { resolve = 150 };
            Assert.AreEqual(BuyResult.Ok, UpgradeService.TryBuy(p, "U-AMMO-1", true));
            Assert.AreEqual(50, p.resolve);
            Assert.Contains("U-AMMO-1", p.upgrades);
        }

        [Test]
        public void Upgrade_Refusals_DoNotDeduct()
        {
            var p = new ProfileData { resolve = 150 };
            Assert.AreEqual(BuyResult.MissingPrerequisite, UpgradeService.TryBuy(p, "U-AMMO-2", true));
            Assert.AreEqual(BuyResult.NotInBunker, UpgradeService.TryBuy(p, "U-AMMO-1", false));
            p.resolve = 99;
            Assert.AreEqual(BuyResult.NotEnoughResolve, UpgradeService.TryBuy(p, "U-AMMO-1", true));
            Assert.AreEqual(99, p.resolve);
            Assert.IsEmpty(p.upgrades);
        }

        [Test]
        public void Upgrade_CannotBuyTwice()
        {
            var p = new ProfileData { resolve = 1000 };
            UpgradeService.TryBuy(p, "U-RELOAD-1", true);
            Assert.AreEqual(BuyResult.AlreadyOwned, UpgradeService.TryBuy(p, "U-RELOAD-1", true));
            Assert.AreEqual(900, p.resolve);
        }

        [Test]
        public void Upgrade_LevelsReplace_NotStack()
        {
            var p = new ProfileData { resolve = 1000 };
            UpgradeService.TryBuy(p, "U-RELOAD-1", true);
            UpgradeService.TryBuy(p, "U-RELOAD-2", true);
            var m = UpgradeService.Modifiers(p);
            Assert.AreEqual(0.8f, m.reloadMultiplier, 1e-5f);
            Assert.AreEqual(100, m.ammoReservePercent);
        }

        [Test]
        public void Upgrade_ReserveCap_RoundsDown()
        {
            var w = new Weapon(Def("A", "x", reserve: 25), 110);
            Assert.AreEqual(27, w.ReserveCap); // 27,5 → 27
        }

        // ── T13: giữ E 1 giây (BR-08) ──
        [Test]
        public void Hold_ReleasedAt09s_DoesNotOpen()
        {
            var hold = new HoldProgress();
            var crate = new object();
            for (int i = 0; i < 9; i++) Assert.IsFalse(hold.Tick(crate, true, 0.1f, 1f));
            Assert.IsFalse(hold.Tick(crate, false, 0.1f, 1f));
            for (int i = 0; i < 9; i++) Assert.IsFalse(hold.Tick(crate, true, 0.1f, 1f)); // phải giữ lại từ đầu
        }

        [Test]
        public void Hold_FullSecond_OpensOnce_ThenNeedsRelease()
        {
            var hold = new HoldProgress();
            var crate = new object();
            bool opened = false;
            for (int i = 0; i < 11; i++) opened |= hold.Tick(crate, true, 0.1f, 1f);
            Assert.IsTrue(opened);
            for (int i = 0; i < 20; i++) Assert.IsFalse(hold.Tick(crate, true, 0.1f, 1f));
        }

        [Test]
        public void Hold_ChangingTarget_Restarts()
        {
            var hold = new HoldProgress();
            object a = new object(), b = new object();
            for (int i = 0; i < 8; i++) hold.Tick(a, true, 0.1f, 1f);
            for (int i = 0; i < 9; i++) Assert.IsFalse(hold.Tick(b, true, 0.1f, 1f));
        }

        // ── T13: đồng hồ câu ngoài trận (BR-10) ──
        [Test]
        public void QuizTimer_AnswerAtDeadline_IsRejected()
        {
            var t = new QuizTimer(12f, 100.0);
            Assert.IsFalse(t.TryAnswer(112.0));
            Assert.IsTrue(t.CheckTimeout(112.0));
            Assert.AreEqual(0f, t.RemainingAtSettle);
        }

        [Test]
        public void QuizTimer_OnlyFirstAnswerCounts()
        {
            var t = new QuizTimer(15f, 0.0);
            Assert.IsTrue(t.TryAnswer(9.0));
            Assert.IsFalse(t.TryAnswer(10.0));
            Assert.IsFalse(t.CheckTimeout(20.0));
            Assert.AreEqual(6f, t.RemainingAtSettle, 1e-4f);
        }

        // ── T29: thắng/thua (BR-32) ──
        [Test]
        public void Mission_DeathAndObjectivesSameFrame_IsLoss()
        {
            Assert.AreEqual(MissionOutcome.Lost, MissionRules.Evaluate(true, true));
            Assert.AreEqual(MissionOutcome.Won, MissionRules.Evaluate(false, true));
            Assert.AreEqual(MissionOutcome.None, MissionRules.Evaluate(false, false));
        }

        [Test]
        public void Attempt_SettlesOnlyOnce_AndHasFreshIds()
        {
            var a = new Attempt("M1", 1);
            Assert.IsTrue(a.Settle(MissionOutcome.Won));
            Assert.IsFalse(a.Settle(MissionOutcome.Lost));
            Assert.AreEqual(MissionOutcome.Won, a.Outcome);
            Assert.AreNotEqual(a.AttemptId, new Attempt("M1", 1).AttemptId);
        }

        // ── T12: thể lực ──
        [Test]
        public void Stamina_Exhausted_MustRecoverBeforeSprinting()
        {
            var s = new StaminaModel(100f) { DrainPerSecond = 50f, RegenPerSecond = 10f, RegenDelay = 0f, RecoverFraction = 0.3f };
            s.Tick(2f, true);
            Assert.IsTrue(s.Exhausted);
            Assert.IsFalse(s.CanSprint);
            s.Tick(2f, false); // 20/100
            Assert.IsFalse(s.CanSprint);
            s.Tick(1f, false); // 30/100
            Assert.IsTrue(s.CanSprint);
        }
    }
}
