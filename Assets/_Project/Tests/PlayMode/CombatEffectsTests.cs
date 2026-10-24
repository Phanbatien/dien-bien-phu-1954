using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DBP.AI;
using DBP.Core;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DBP.Tests
{
    public class CombatEffectsTests
    {
        static ParticleSystem[] Particles => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Where(p => p.transform.root.name == "CombatEffects").ToArray();
        static void Effect(string method, params object[] args)
        {
            var type = typeof(CampaignAudio).Assembly.GetType("DBP.Core.CombatEffects");
            Assert.NotNull(type, "T67 needs a bounded combat effects service.");
            type.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; InputLock.ResetAll();
            yield return SceneManager.LoadSceneAsync("Sandbox"); yield return null;
            // Sandbox giờ có AI thật (lô cốt, xe tăng, lính bắn): tắt để chỉ đo hiệu ứng đang kiểm tra.
            foreach (var h in Object.FindObjectsByType<DBP.Core.Health>(FindObjectsSortMode.None))
                if (h.Kind != DBP.Core.TargetKind.Player) h.gameObject.SetActive(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; InputLock.ResetAll();
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
        }
        [UnityTest] public IEnumerator ActualWeaponFire_EmitsMuzzleAndImpact_ConsumesOneRound()
        {
            var weapon = new Weapon(WeaponCatalog.Get("W-MAT49"));
            var controller = Object.FindAnyObjectByType<WeaponController>();
            var cam = Camera.main;
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = cam.transform.position + cam.transform.forward * 3;
            target.transform.localScale = Vector3.one * 2;
            Physics.SyncTransforms();
            typeof(WeaponController).GetMethod("Fire", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { weapon });
            yield return null;
            Assert.AreEqual(weapon.Def.stats.mag - 1, weapon.InMag);
            Assert.IsTrue(Particles.Any(p => p.name.StartsWith("Muzzle") && p.particleCount > 0), "Real firing must show muzzle flash.");
            Assert.IsTrue(Particles.Any(p => p.name.StartsWith("Dust") && p.particleCount > 0), "Real hit must show dust.");
            Object.Destroy(target);
        }
        [UnityTest] public IEnumerator VisualBursts_AreBounded_AndDoNotConsumeGameplayRandomness()
        {
            Effect("Explosion", Vector3.zero, 1f);
            var state = UnityEngine.Random.state;
            int count = Particles.Length;
            for (int i = 0; i < 200; i++)
            {
                Effect("Explosion", Vector3.zero, 1f);
                Effect("Muzzle", Vector3.one, Quaternion.identity, 1f);
                Effect("Impact", Vector3.one, Vector3.up, true);
            }
            Assert.AreEqual(count, Particles.Length, "Sustained fire must reuse its pool.");
            Assert.LessOrEqual(count, 40, "Scene effect pool must stay small.");
            Assert.AreEqual(state, UnityEngine.Random.state);
            yield return null;
            Assert.IsTrue(Particles.All(p => p.GetComponent<Renderer>().sharedMaterial.shader.isSupported));
        }
        [UnityTest] public IEnumerator GrenadeSmoke_SurvivesEmitterDestruction_FreezesAndExpires()
        {
            var go = new GameObject("VfxGrenade"); go.transform.position = Camera.main.transform.position + Vector3.forward * 25;
            var grenade = go.AddComponent<Grenade>(); grenade.Explode();
            yield return null;
            Assert.IsFalse(go);
            var smoke = Particles.FirstOrDefault(p => p.name.StartsWith("Smoke") && p.particleCount > 0);
            Assert.NotNull(smoke, "Explosion smoke must outlive the grenade.");
            Time.timeScale = 0;
            yield return null; // Let the already-scheduled particle step finish before measuring a frozen interval.
            float before = smoke.time;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(before, smoke.time, .001f, "World freeze must freeze smoke.");
            Time.timeScale = 1;
            yield return new WaitForSeconds(3.5f);
            Assert.IsTrue(Particles.All(p => p.particleCount == 0), "All bursts must expire.");
        }
        [UnityTest] public IEnumerator SceneChange_RemovesOldEffectPool()
        {
            Effect("Explosion", Vector3.zero, 1f);
            var old = Particles[0].transform.root.gameObject;
            yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
            Assert.IsFalse(old, "Effects must belong to their mission scene.");
        }
    }
}
