using System.Collections;
using System.Linq;
using DBP.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Hoạt ảnh từng loại nhân vật (29/10): bộ đội, lính địch, chỉ huy đều có đủ động tác; chụp từng tư thế để soát bằng mắt
    /// (đặt biến môi trường SHOT_DIR).
    public class CharacterAnimationTests
    {
        static readonly (string state, int layer)[] Poses =
        {
            ("Locomotion", 0), ("Crouch", 0), ("Climb", 0), ("Death", 0),
            ("Shoot", 1), ("Stab", 1), ("Punch", 1), ("Throw", 1), ("Hit", 1), ("Command", 1), ("Rally", 1),
        };

        [UnityTest] public IEnumerator SuppliedClips_AreUsed_AlliesCrouchFire_EnemiesStabAndThrow()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null;
            var allyClips = ComradeFollower.All.First().GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
            CollectionAssert.Contains(allyClips, "Crouch Fire Rifle", "Bộ đội ngồi bắn bằng clip thật");
            foreach (var soldier in SoldierAI.All.Take(6))
            {
                var clips = soldier.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
                CollectionAssert.Contains(clips, "Stabbing", "Địch đâm dao bằng clip thật");
                CollectionAssert.Contains(clips, "Grenade Throw", "Địch ném lựu đạn bằng clip thật");
            }
        }

        [UnityTest] public IEnumerator Allies_DoNotCrouch_WhenHitWithNoEnemyInSight()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null;
            var ally = ComradeFollower.All.First();
            var animator = ally.GetComponentInChildren<Animator>();
            ally.TakeHit(1f); // trúng đạn: trước đây bộ đội ngồi xuống 1,2 giây
            yield return null; yield return null;
            Assert.IsFalse(animator.GetBool("Crouch"), "Trúng đạn mà không có địch trong tầm: không ngồi");
        }

        [UnityTest] public IEnumerator EveryCharacter_HasActionAnimations_AndPosesDiffer()
        {
            yield return SceneManager.LoadSceneAsync("M1_HimLam"); yield return null; yield return null;
            var ally = ComradeFollower.All.First().GetComponentInChildren<Animator>();
            var enemy = SoldierAI.All.First(s => !s.GetComponent<OfficerLeader>()).GetComponentInChildren<Animator>();
            var officer = Object.FindObjectsByType<OfficerLeader>(FindObjectsSortMode.None).Select(o => o.GetComponentInChildren<Animator>()).FirstOrDefault(a => a);
            Time.timeScale = 0f;
            string dir = System.Environment.GetEnvironmentVariable("SHOT_DIR");
            foreach (var (who, animator) in new[] { ("bo_doi", ally), ("linh_dich", enemy), ("chi_huy", officer) })
            {
                if (!animator) continue;
                foreach (var (state, layer) in Poses)
                    Assert.IsTrue(animator.HasState(layer, Animator.StringToHash(state)), $"{who}: thiếu động tác {state}");

                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // lính ở xa camera chính vẫn phải cập nhật tư thế
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var cam = new GameObject("CamSoat").AddComponent<Camera>();
                var t = animator.transform;
                cam.transform.position = t.position + t.forward * 2.3f + t.right * 1.1f + Vector3.up * 1.2f;
                cam.transform.LookAt(t.position + Vector3.up * .9f);
                Vector3 rest = default;
                float standHips = 0f;
                foreach (var (state, layer) in Poses)
                {
                    animator.Play("Locomotion", 0, 0f); animator.Play("Trong", 1, 0f);
                    animator.Play(state, layer, .45f);
                    animator.Update(0f);
                    float hips = animator.GetBoneTransform(HumanBodyBones.Hips).position.y - t.position.y;
                    if (state == "Locomotion") standHips = hips;
                    if (state == "Crouch") Assert.Less(hips, standHips - .2f, $"{who}: ngồi nấp phải hạ thấp người");
                    if (state == "Locomotion") rest = hand.position - t.position;
                    else if (state != "Hit") Assert.Greater(Vector3.Distance(rest, hand.position - t.position), .05f, $"{who}/{state}: tay phải phải đổi tư thế");
                    if (string.IsNullOrEmpty(dir)) continue;
                    var rt = new RenderTexture(640, 640, 24);
                    cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; RenderTexture.active = rt;
                    var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"anim_{who}_{state}.png"), tex.EncodeToPNG());
                    Object.Destroy(rt); Object.Destroy(tex);
                }
                Object.Destroy(cam.gameObject);
            }
            Time.timeScale = 1f;
        }
    }
}
