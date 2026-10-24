using System.Collections;
using System.Linq;
using System.Text;
using DBP.Core;
using DBP.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Đứng trước mỗi hòm trong 5 màn, nhìn vào hòm: E phải nhắm trúng hòm (Interactor.Focus).
    public class CrateInteractionTests
    {
        static readonly System.Reflection.MethodInfo FindFocus = typeof(Interactor).GetMethod("FindFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        static readonly string[] Scenes = { "M1_HimLam", "M2_GiuDocLap", "M3_DoiED", "M4_LongHao", "M5_ChieuMungBay" };

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox"); // trả lại scene sạch cho test sau
            Time.timeScale = 1f; InputLock.ResetAll();
        }

        [UnityTest]
        public IEnumerator EveryCrate_CanBeFocusedFromInFront([ValueSource(nameof(Scenes))] string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null; yield return null;
            var begin = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "BeginMission");
            if (begin) begin.onClick.Invoke();
            Time.timeScale = 1f;
            yield return null;

            // Chỉ kiểm tra hòm: tắt lính địch để không ai chạy ngang che tầm nhìn lúc dò
            foreach (var h in Object.FindObjectsByType<Health>(FindObjectsSortMode.None))
                if (h.Kind != TargetKind.Player) h.gameObject.SetActive(false);
            var player = GameObject.FindGameObjectWithTag("Player");
            var interactor = player.GetComponent<Interactor>();
            Assert.IsNotNull(interactor, "Người chơi thiếu Interactor");
            var cam = player.GetComponentInChildren<Camera>();
            var controller = player.GetComponent<CharacterController>();
            var crates = Object.FindObjectsByType<QuizCrate>(FindObjectsSortMode.None).Where(c => !c.Used).ToArray();
            Assert.IsNotEmpty(crates, $"{scene}: không có hòm");

            var report = new StringBuilder();
            int failed = 0;
            foreach (var crate in crates)
            {
                var bounds = crate.GetComponentsInChildren<Collider>().Select(c => c.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                // Đứng cách hòm 1,4 m, nhìn vào tâm hòm
                var away = player.transform.position - bounds.center; away.y = 0;
                if (away.sqrMagnitude < 0.01f) away = Vector3.back;
                var feet = bounds.center + away.normalized * 1.4f;
                feet.y = bounds.min.y;
                if (controller) controller.enabled = false;
                player.transform.position = feet;
                if (controller) controller.enabled = true;
                player.transform.rotation = Quaternion.LookRotation(-away.normalized);
                yield return null;
                // Bộ điều khiển giữ góc cúi riêng nên đặt hướng nhìn ngay trước khi dò, không chờ khung sau
                cam.transform.rotation = Quaternion.LookRotation(bounds.center - cam.transform.position);
                var focus = FindFocus.Invoke(interactor, null);
                if (!ReferenceEquals(focus, crate))
                {
                    failed++;
                    var hit = string.Join(" → ", Physics.RaycastAll(cam.transform.position, cam.transform.forward, 3f, ~0, QueryTriggerInteraction.Ignore)
                        .OrderBy(x => x.distance).Select(x => $"{x.collider.name}[{x.collider.GetType().Name} {x.distance:0.00}m, của người chơi={x.collider.transform.IsChildOf(player.transform)}]"));
                    report.AppendLine($"{crate.name} @ {bounds.center}: focus={focus}, tia trúng {hit}, CanInteract={crate.CanInteract(interactor)}");
                }
                else if (crate == crates[0] && !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("SHOT_DIR")))
                {
                    var rt = new RenderTexture(1280, 720, 24);
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; rt.Release();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("SHOT_DIR"), $"crate_{scene}.png"), tex.EncodeToPNG());
                }
            }
            Assert.AreEqual(0, failed, $"{scene}: {failed}/{crates.Length} hòm không nhắm được\n{report}");
        }
    }
}
