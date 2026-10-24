using System.Collections;
using DBP.Core;
using DBP.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DBP.Tests
{
    /// Hòm ngẫu nhiên phải nằm trên mặt đất và không dính chướng ngại vật.
    public class CratePlacementTests
    {
        Scene scene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            scene = SceneManager.CreateScene("CratePlacementTest");
            SceneManager.SetActiveScene(scene);
            Box("Ground", new Vector3(0, 7.5f, 0), new Vector3(60, 1, 60)); // mặt đất cao 8 m như địa hình Màn 1
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        static GameObject Box(string name, Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = size;
            return go;
        }

        static bool Overlaps(Vector3 ground)
        {
            var center = ground + Vector3.up * (CratePlacement.HalfExtents.y + 0.05f);
            foreach (var c in Physics.OverlapBox(center, CratePlacement.HalfExtents, Quaternion.identity))
                if (c.name != "Ground") return true;
            return false;
        }

        [UnityTest]
        public IEnumerator BuriedPoint_SnapsUpToGround()
        {
            yield return null;
            Assert.IsTrue(CratePlacement.TryFindSpot(Vector3.zero, Quaternion.identity, out var ground), "Điểm (0,0,0) bị chôn phải được đưa lên mặt đất");
            Assert.AreEqual(8f, ground.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator PointInsideWall_MovesToFreeSpot()
        {
            Box("VachHao", new Vector3(0, 9f, 0), new Vector3(0.5f, 2f, 10f));
            yield return null;
            Assert.IsTrue(CratePlacement.TryFindSpot(new Vector3(0, 8, 0), Quaternion.identity, out var ground));
            Assert.IsFalse(Overlaps(ground), "Hòm không được dính vách hào");
            Assert.LessOrEqual(Vector3.Distance(new Vector3(ground.x, 8, ground.z), new Vector3(0, 8, 0)), 3.01f);
        }

        [UnityTest]
        public IEnumerator FullyBlockedArea_IsRejected()
        {
            Box("ToaNha", new Vector3(0, 10f, 0), new Vector3(10f, 4f, 10f));
            yield return null;
            Assert.IsFalse(CratePlacement.TryFindSpot(new Vector3(0, 8, 0), Quaternion.identity, out _), "Không có chỗ trống thì bỏ điểm");
        }

        [UnityTest]
        public IEnumerator NeverPlacedOnBunkerRoof()
        {
            var bunker = Box("LoCot", new Vector3(0, 9.25f, 0), new Vector3(4f, 2.5f, 4f));
            bunker.AddComponent<Health>().Configure(TargetKind.Bunker, 300, DamageType.Bullet);
            yield return null;
            Assert.IsTrue(CratePlacement.TryFindSpot(new Vector3(0, 12, 0), Quaternion.identity, out var ground));
            Assert.AreEqual(8f, ground.y, 0.01f, "Đặt dưới đất cạnh lô cốt, không đặt trên nóc");
            Assert.IsFalse(Overlaps(ground));
        }

        [UnityTest]
        public IEnumerator SameSpotTwice_SecondCrateDoesNotOverlapFirst()
        {
            yield return null;
            Assert.IsTrue(CratePlacement.TryFindSpot(new Vector3(0, 8, 0), Quaternion.identity, out var first));
            QuizCrate.Spawn(first, Quaternion.identity, new CrateReward(), true, null);
            Assert.IsTrue(CratePlacement.TryFindSpot(new Vector3(0, 8, 0), Quaternion.identity, out var second));
            Assert.Greater(Vector3.Distance(first, second), 0.5f, "Hai hòm ở cùng điểm khai báo phải tách nhau");
        }
    }
}
