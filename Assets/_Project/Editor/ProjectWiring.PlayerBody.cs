using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Nhân vật người chơi nhìn thấy được (30/10): thân nhân vật (model bộ đội của Long, cùng controller hoạt ảnh) gắn
    /// vào Player.prefab cho góc nhìn thứ ba; dao găm thật (Combat Knife) trước camera; lưỡi lê thật trên súng của bộ đội,
    /// lính địch và nhân vật người chơi để động tác đâm lê có lưỡi.
    public static partial class ProjectWiring
    {
        const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        const string KnifePrefab = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Close Combat/Combat Knife_A.prefab";
        const string BayonetPrefab = "Assets/Low Poly Weapon Series V 4/Prefabs/Previous/Attachments/UnderBarrels/Bayonets/Bayonet_A.prefab";

        static void EnsurePlayerBody()
        {
            // Lưỡi lê cho súng bộ đội trước, để thân người chơi chép từ đó có sẵn lưỡi lê
            var comrade = PrefabUtility.LoadPrefabContents(ComradePrefabPath);
            try
            {
                foreach (var gun in comrade.GetComponentsInChildren<Transform>(true).Where(t => t.name == "AlliedRifle").ToArray()) FitBayonet(gun);
                PrefabUtility.SaveAsPrefabAsset(comrade, ComradePrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(comrade); }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var old = root.transform.Find("ThanNhanVat");
                if (old) Object.DestroyImmediate(old.gameObject);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ComradePrefabPath)?.transform.Find("Model");
                if (source)
                {
                    var body = Object.Instantiate(source.gameObject, root.transform, false);
                    body.name = "ThanNhanVat";
                    foreach (var c in body.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    foreach (var r in body.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    body.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    // Bazooka trên tay nhân vật (góc thứ ba): cùng chỗ cầm với súng trường, bật khi đang dùng
                    var rifle = body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "AlliedRifle");
                    if (rifle)
                    {
                        var baz = BuildBazooka("BazookaThan");
                        baz.transform.SetParent(rifle.parent, false);
                        baz.transform.localPosition = rifle.localPosition;
                        baz.transform.localRotation = rifle.localRotation;
                        var ls = rifle.parent.lossyScale;
                        baz.transform.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
                        baz.SetActive(false);
                    }
                }

                var cam = root.GetComponentInChildren<Camera>(true).transform;
                var oldKnife = cam.Find("DaoGam_Model");
                if (oldKnife) Object.DestroyImmediate(oldKnife.gameObject);
                var knifeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(KnifePrefab);
                if (knifeAsset)
                {
                    var holder = new GameObject("DaoGam_Model").transform;
                    holder.SetParent(cam, false);
                    var knife = (GameObject)PrefabUtility.InstantiatePrefab(knifeAsset, holder);
                    foreach (var c in knife.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    AimAlong(knife.transform, holder, Vector3.forward, .3f); // mũi dao hướng tới trước, dài 30 cm
                    foreach (var r in knife.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    UrpMaterialUtility.Apply(knife);
                    holder.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// Lưỡi lê cho súng lính địch trong scene (súng tt-3d gắn ở tay phải).
        static int SceneBayonets()
        {
            int n = 0;
            foreach (var gun in All<Transform>().Where(t => (t.name.StartsWith("Rifle1") || t.name == "AlliedRifle") && t.GetComponentInParent<Animator>()).ToArray())
                if (FitBayonet(gun)) n++;
            return n;
        }

        static bool FitBayonet(Transform gun)
        {
            var old = gun.Find("LuoiLe_Model");
            if (old) Object.DestroyImmediate(old.gameObject);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(BayonetPrefab);
            if (!asset || !PointyAxis(gun, out var muzzleDir, out var tip)) return false;
            var holder = new GameObject("LuoiLe_Model").transform;
            holder.SetParent(gun, false);
            holder.position = tip;
            holder.rotation = Quaternion.LookRotation(muzzleDir, gun.up);
            var blade = (GameObject)PrefabUtility.InstantiatePrefab(asset, holder);
            foreach (var c in blade.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            AimAlong(blade.transform, holder, Vector3.forward, .32f);
            // Gốc lưỡi lê chạm đầu nòng, mũi chĩa ra trước
            var b = WorldBounds(blade);
            float back = Vector3.Dot(b.center - holder.position, muzzleDir) - Vector3.Dot(b.extents, new Vector3(Mathf.Abs(muzzleDir.x), Mathf.Abs(muzzleDir.y), Mathf.Abs(muzzleDir.z)));
            blade.transform.position -= muzzleDir * (back + .06f);
            UrpMaterialUtility.Apply(blade);
            return true;
        }

        /// Xoay + co giãn model sao cho đầu nhọn (mũi dao / lưỡi lê) chĩa theo trục 'forward' của khung 'frame', dài 'length' m.
        static void AimAlong(Transform model, Transform frame, Vector3 forward, float length)
        {
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity;
            if (!PointyAxis(model, out var tipDir, out _)) return;
            model.rotation = Quaternion.FromToRotation(tipDir, frame.TransformDirection(forward)) * model.rotation;
            var b = WorldBounds(model.gameObject);
            float now = Vector3.Dot(b.size, new Vector3(Mathf.Abs(frame.forward.x), Mathf.Abs(frame.forward.y), Mathf.Abs(frame.forward.z)));
            if (now > .001f) model.localScale *= length / now;
            b = WorldBounds(model.gameObject);
            model.position += frame.position - b.center;
        }

        /// Trục dài nhất của model và đầu nào nhọn hơn (mặt cắt ở 15% cuối nhỏ hơn): nòng súng, mũi dao, mũi lê.
        static bool PointyAxis(Transform t, out Vector3 worldDir, out Vector3 tipPoint)
        {
            worldDir = t.forward; tipPoint = t.position;
            var pts = t.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh)
                .SelectMany(m => m.sharedMesh.vertices.Select(v => m.transform.TransformPoint(v))).ToArray();
            if (pts.Length < 8) return false;
            var center = pts.Aggregate(Vector3.zero, (a, p) => a + p) / pts.Length;
            Vector3 best = Vector3.zero; float bestSpan = 0f;
            foreach (var axis in new[] { t.right, t.up, t.forward })
            {
                float span = pts.Max(p => Vector3.Dot(p - center, axis)) - pts.Min(p => Vector3.Dot(p - center, axis));
                if (span > bestSpan) { bestSpan = span; best = axis; }
            }
            float hi = pts.Max(p => Vector3.Dot(p - center, best)), lo = pts.Min(p => Vector3.Dot(p - center, best));
            float Section(bool top) => pts.Where(p => top ? Vector3.Dot(p - center, best) > hi - bestSpan * .15f : Vector3.Dot(p - center, best) < lo + bestSpan * .15f)
                .Select(p => Vector3.ProjectOnPlane(p - center, best).magnitude).DefaultIfEmpty(0f).Max();
            bool topPointy = Section(true) <= Section(false);
            worldDir = topPointy ? best : -best;
            tipPoint = center + worldDir * (topPointy ? hi : -lo);
            return true;
        }
    }
}
