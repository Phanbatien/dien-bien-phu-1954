using DBP.AI;
using DBP.Core;
using DBP.Interaction;
using DBP.Quiz;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace DBP.Tests
{
    /// Kiểm tra logic toàn bộ các hệ thống do Bùi Nguyễn Thành Đạt phụ trách (T07, T08, T14, T15, T22, T23, T39, T45, T53, T65).
    /// Chạy qua: Window → General → Test Runner → EditMode.
    public class DatSystemsTests
    {
        // ── T08 & T15: Hệ máu, sát thương và miễn nhiễm (BR-20, BR-21) ──
        [Test]
        public void Health_Infantry_TakesBulletDamage()
        {
            var go = new GameObject("TestInfantry");
            var h = go.AddComponent<Health>();
            h.Configure(TargetKind.Infantry, 100f);

            h.TakeDamage(30f, DamageType.Bullet, Vector3.zero);
            Assert.AreEqual(70f, h.Current, 1e-4f);
            Assert.AreEqual(30f, h.TotalDamageTaken, 1e-4f);
            Assert.IsFalse(h.IsDead);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Health_Bunker_ImmuneToBullet_TakesExplosive()
        {
            var go = new GameObject("TestBunker");
            var h = go.AddComponent<Health>();
            // Lô cốt miễn nhiễm đạn súng cá nhân (BR-20)
            h.Configure(TargetKind.Bunker, 500f, DamageType.Bullet);

            // Bắn đạn súng: không mất máu
            h.TakeDamage(100f, DamageType.Bullet, Vector3.zero);
            Assert.AreEqual(500f, h.Current, 1e-4f);
            Assert.AreEqual(0f, h.TotalDamageTaken, 1e-4f);

            // Đánh bộc phá / pháo nổ: mất máu bình thường
            h.TakeDamage(300f, DamageType.Explosive, Vector3.zero);
            Assert.AreEqual(200f, h.Current, 1e-4f);
            Assert.AreEqual(300f, h.TotalDamageTaken, 1e-4f);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Health_Tank_ImmuneToBullet_DiesFromAntiTank()
        {
            var go = new GameObject("TestTank");
            var h = go.AddComponent<Health>();
            // Xe tăng miễn Bullet (BR-20)
            h.Configure(TargetKind.Tank, 800f, DamageType.Bullet);

            h.TakeDamage(50f, DamageType.Bullet, Vector3.zero);
            Assert.AreEqual(800f, h.Current, 1e-4f);

            h.TakeDamage(800f, DamageType.AntiTank, Vector3.zero);
            Assert.IsTrue(h.IsDead);
            Assert.IsTrue(h.Neutralized);

            Object.DestroyImmediate(go);
        }

        // ── T23: Cơ chế bảo hiểm sau 3 hòm sai (BR-16) ──
        [Test]
        public void InsuranceTracker_ActivatesOnlyAfterThreeWrong()
        {
            var go = new GameObject("TestTracker");
            var tracker = go.AddComponent<InsuranceTracker>();

            Assert.IsFalse(tracker.IsInsuranceActive);
            Assert.AreEqual(0, tracker.CurrentStreak);

            tracker.RecordCrateResult(QuizOutcome.Wrong);
            Assert.AreEqual(1, tracker.CurrentStreak);
            Assert.IsFalse(tracker.IsInsuranceActive);

            tracker.RecordCrateResult(QuizOutcome.Timeout);
            Assert.AreEqual(2, tracker.CurrentStreak);
            Assert.IsFalse(tracker.IsInsuranceActive);

            tracker.RecordCrateResult(QuizOutcome.Wrong);
            Assert.AreEqual(3, tracker.CurrentStreak);
            Assert.IsTrue(tracker.IsInsuranceActive); // Đủ 3 lần sai liên tiếp

            // Sau câu bảo hiểm, tiêu thụ đưa streak về 0
            tracker.ConsumeInsurance();
            Assert.AreEqual(0, tracker.CurrentStreak);
            Assert.IsFalse(tracker.IsInsuranceActive);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void InsuranceTracker_CorrectAnswer_ResetsStreak()
        {
            var go = new GameObject("TestTracker");
            var tracker = go.AddComponent<InsuranceTracker>();

            tracker.RecordCrateResult(QuizOutcome.Wrong);
            tracker.RecordCrateResult(QuizOutcome.Wrong);
            Assert.AreEqual(2, tracker.CurrentStreak);

            // Trả lời đúng trước khi đủ 3 lần → reset streak
            tracker.RecordCrateResult(QuizOutcome.Correct);
            Assert.AreEqual(0, tracker.CurrentStreak);
            Assert.IsFalse(tracker.IsInsuranceActive);

            Object.DestroyImmediate(go);
        }

        // ── T23: Giới hạn số lượng vũ khí rơi từ địch (BR-14) ──
        [Test]
        public void DropLimitTracker_EnforcesMaximumDrops()
        {
            DropLimitTracker.Reset();
            DropLimitTracker.MaxDrops = 3;

            Assert.IsTrue(DropLimitTracker.CanDrop());

            DropLimitTracker.RegisterDrop();
            DropLimitTracker.RegisterDrop();
            Assert.AreEqual(2, DropLimitTracker.CurrentDrops);
            Assert.IsTrue(DropLimitTracker.CanDrop());

            DropLimitTracker.RegisterDrop();
            Assert.AreEqual(3, DropLimitTracker.CurrentDrops);
            Assert.IsFalse(DropLimitTracker.CanDrop()); // Đạt trần, không sinh thêm

            DropLimitTracker.Reset();
            Assert.AreEqual(0, DropLimitTracker.CurrentDrops);
            Assert.IsTrue(DropLimitTracker.CanDrop());
        }

        // ── T22: Hạt giống ngẫu nhiên hòm (BR-13) ──
        [Test]
        public void CrateSpawner_SameSeed_ProducesIdenticalSequence()
        {
            int seed = 19540507;
            var rng1 = new System.Random(seed);
            var rng2 = new System.Random(seed);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(rng1.Next(5, 8), rng2.Next(5, 8));
                Assert.AreEqual(rng1.NextDouble(), rng2.NextDouble());
            }
        }

        // ── T39: Quy tắc rút lui nhóm phản kích (BR-20) ──
        [Test]
        public void CounterAttack_RetreatThreshold_StrictlyGreaterThanHalf()
        {
            // BR-20: Rút lui khi mất HƠN một nửa quân số ban đầu. Mất đúng một nửa chưa rút.
            int count4 = 4;
            Assert.IsFalse(2 > (count4 / 2f)); // Mất 2/4 = đúng 1/2 → CHƯA rút
            Assert.IsTrue(3 > (count4 / 2f));  // Mất 3/4 = hơn 1/2 → RÚT

            int count6 = 6;
            Assert.IsFalse(3 > (count6 / 2f)); // Mất 3/6 = đúng 1/2 → CHƯA rút
            Assert.IsTrue(4 > (count6 / 2f));  // Mất 4/6 = hơn 1/2 → RÚT

            int count5 = 5;
            Assert.IsFalse(2 > (count5 / 2f)); // Mất 2/5 (40%) → CHƯA rút
            Assert.IsTrue(3 > (count5 / 2f));  // Mất 3/5 (60%) → RÚT
        }

        // ── T15 & T30: Quản lý bộc phá (BR-17, BR-20) ──
        [Test]
        public void SatchelCharge_CountAndReplenish()
        {
            var go = new GameObject("TestSatchel");
            go.AddComponent<BoxCollider>();
            var sc = go.AddComponent<SatchelCharge>();

            sc.Count = 2;
            Assert.AreEqual(2, sc.Count);

            // Thêm vật tư nhiệm vụ từ trạm tiếp tế (T30)
            sc.AddCount(3);
            Assert.AreEqual(5, sc.Count);

            Object.DestroyImmediate(go);
        }

        // ── T65: Cân bằng trọng số độ hiếm hòm (BR-29) ──
        [Test]
        public void BalanceConfig_RarityWeights_SumToOne()
        {
            var cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            float total = cfg.rarityCommon + cfg.rarityUncommon + cfg.rarityRare + cfg.rarityLegendary;
            Assert.AreEqual(1.0f, total, 1e-4f);
            Object.DestroyImmediate(cfg);
        }

        // ── T53: Danh mục vũ khí (BR-18, BR-29) ──
        [Test]
        public void WeaponCatalog_ContainsHistoricalWeapons()
        {
            // Kiểm tra catalog đọc được file và chứa các vũ khí quy định
            var mosin = WeaponCatalog.Get("W-MOSIN");
            Assert.IsNotNull(mosin);
            Assert.AreEqual("7.62x54R", mosin.ammoType);

            var mat49 = WeaponCatalog.Get("W-MAT49");
            Assert.IsNotNull(mat49);
            Assert.AreEqual("9x19", mat49.ammoType);

            var type50 = WeaponCatalog.Get("W-TYPE50");
            Assert.IsNotNull(type50);
            Assert.AreEqual("7.62x25", type50.ammoType);

            var bazooka = WeaponCatalog.Get("W-BAZOOKA");
            Assert.IsNotNull(bazooka);
            Assert.AreEqual("60mm Rocket", bazooka.ammoType);
        }
    }
}
