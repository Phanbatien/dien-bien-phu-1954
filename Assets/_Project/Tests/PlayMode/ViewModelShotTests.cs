using System.Collections;
using System.IO;
using System.Linq;
using DBP.Player;
using DBP.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Mỗi loại súng hiện đúng bộ tay + súng; đặt SHOT_DIR để chụp ảnh góc nhìn thứ nhất.
    public class ViewModelShotTests
    {
        [UnityTest] public IEnumerator EachWeaponKind_ShowsItsViewModel()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox");
            yield return null; yield return null;
            var cam = Camera.main;
            var weapons = cam.GetComponent<WeaponController>();
            var arms = Object.FindAnyObjectByType<FirstPersonArms>();
            string dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            foreach (var (id, model) in new[] { ("W-MOSIN", "VM_Rifle"), ("W-MAT49", "VM_Smg"), ("W-MOSIN-SCOPE", "VM_Scoped"), ("W-BAZOOKA", "VM_Bazooka") })
            {
                weapons.Inventory.GiveWeapon(WeaponCatalog.Get(id), 30);
                yield return null; yield return null;
                var shown = arms.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("VM_")).Select(t => t.name).ToArray();
                CollectionAssert.AreEqual(new[] { model }, shown, id);
                var soldierArms = arms.GetComponentsInChildren<MeshFilter>()
                    .Where(m => m.sharedMesh && m.sharedMesh.name.Contains("SentinelArms")).ToArray();
                Assert.AreEqual(2, soldierArms.Length, "Both hands must come from the new allied soldier: " + id);
                foreach (var arm in soldierArms)
                {
                    Assert.Greater(arm.sharedMesh.vertexCount, 100, "The soldier arm must retain its model geometry");
                    var texture = arm.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap");
                    Assert.IsNotNull(texture);
                    Assert.AreEqual("Meshy_AI_Jungle_Sentinel_20261014_texture", texture.name);
                }
                if (string.IsNullOrEmpty(dir)) continue;
                var rt = new RenderTexture(1280, 720, 24);
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; rt.Release();
                File.WriteAllBytes(Path.Combine(dir, $"vm_{model}.png"), tex.EncodeToPNG());
            }
        }
    }
}
