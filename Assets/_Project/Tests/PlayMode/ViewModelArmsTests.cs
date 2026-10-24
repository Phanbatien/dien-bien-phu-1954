using System.Collections;
using System.Reflection;
using DBP.Player;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Player trong Sandbox: tay Infantry cầm WWII Rifle A, nòng theo camera, ống ngắm vẫn ẩn view model.
    public class ViewModelArmsTests
    {
        [UnityTest]
        public IEnumerator Player_HoldsRifle_FollowsLook_AndScopeHidesIt()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox");
            yield return null;
            yield return null;

            var player = GameObject.FindGameObjectWithTag("Player");
            var arms = Object.FindAnyObjectByType<FirstPersonArms>();
            var cam = Camera.main;
            Assert.IsNotNull(player);
            Assert.IsNotNull(arms);
            Assert.IsNotNull(cam);
            Assert.IsNotNull(arms.rifle);
            Assert.IsTrue(arms.rifle.IsChildOf(FindNamed(arms.transform, "hand.R")));
            Assert.Less(Vector3.Angle(arms.rifle.forward, cam.transform.forward), 5f);
            Assert.Less(FindNamed(arms.transform, "head").localScale.x, 0.01f);
            Assert.Less(FindNamed(arms.transform, "upper_leg.L").localScale.x, 0.01f);
            Assert.Less(FindNamed(arms.transform, "upper_leg.R").localScale.x, 0.01f);
            Assert.AreEqual(0, arms.GetComponentsInChildren<Collider>(true).Length);

            foreach (var renderer in arms.GetComponentsInChildren<Renderer>(true))
            {
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, renderer.shadowCastingMode);
                foreach (var mat in renderer.sharedMaterials)
                {
                    Assert.IsNotNull(mat);
                    Assert.IsNotNull(mat.shader);
                    StringAssert.StartsWith("Universal Render Pipeline", mat.shader.name);
                }
            }

            var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 0.8f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                bool onPlayer = hit.collider.transform == player.transform || hit.collider.transform.IsChildOf(player.transform);
                Assert.IsFalse(onPlayer, "Súng hoặc tay không được chặn tia bắn");
            }

            var pivot = cam.transform.parent;
            player.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            pivot.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            Assert.Less(Vector3.Angle(arms.rifle.forward, cam.transform.forward), 5f, "Nòng phải đi theo hướng nhìn");

            var before = arms.rifle.position;
            pivot.localPosition += Vector3.down * 0.5f;
            Assert.AreEqual(before.y - 0.5f, arms.rifle.position.y, 0.02f, "Ngồi phải hạ súng theo camera");

            var weapons = cam.GetComponent<WeaponController>();
            Assert.IsNotNull(weapons);
            var scoped = WeaponCatalog.Get("W-MOSIN-SCOPE");
            Assert.IsNotNull(scoped);
            weapons.Inventory.GiveWeapon(scoped, 20);
            Assert.AreEqual("W-MOSIN-SCOPE", weapons.Inventory.Current.Def.id);

            cam.fieldOfView = 10f;
            SetAiming(weapons, true);
            UpdateAim(weapons);
            Assert.IsFalse(arms.gameObject.activeInHierarchy, "Ống ngắm phải ẩn view model");

            SetAiming(weapons, false);
            UpdateAim(weapons);
            Assert.IsTrue(arms.gameObject.activeInHierarchy, "Thả ngắm phải hiện lại tay và súng");
        }

        static void SetAiming(WeaponController weapons, bool aiming)
        {
            var setter = typeof(WeaponController).GetProperty("IsAiming").GetSetMethod(true);
            setter.Invoke(weapons, new object[] { aiming });
        }

        static void UpdateAim(WeaponController weapons)
        {
            var method = typeof(WeaponController).GetMethod("UpdateAim", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(weapons, new object[] { weapons.Inventory.Current });
        }

        static Transform FindNamed(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            Assert.Fail($"Không thấy {name}");
            return null;
        }
    }
}
