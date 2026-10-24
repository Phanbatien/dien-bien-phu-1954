using System.IO;
using DBP.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.EditorTools
{
    /// Mẫu bộ đội ta (hướng 1): dùng thân Infantry nhuộm quân phục xanh lá, bỏ mũ sắt và ba lô Tây,
    /// đội mũ cối có lưới ngụy trang lá cây dựng bằng khối. 3 đồng đội đi cùng người chơi ở mỗi màn.
    public static partial class ProjectWiring
    {
        static bool comradeBuilt; // dựng lại mẫu bộ đội một lần mỗi lượt nối scene
        const string ComradePrefabPath = "Assets/_Project/Prefabs/BoDoi.prefab";

        static GameObject ComradePrefab(bool rebuild)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ComradePrefabPath);
            if (existing && !rebuild) return existing;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(InfantryPrefabPath);
            if (!src) return null;
            var root = new GameObject("BoDoi");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(src, root.transform);
            model.name = "Model";
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var b = WorldBounds(model);
            model.transform.localScale = Vector3.one * (1.72f / Mathf.Max(0.01f, b.size.y)); // người Việt thấp hơn lính Âu chút
            b = WorldBounds(model);
            model.transform.position += Vector3.up * -b.min.y;
            UrpMaterialUtility.Apply(root);

            // Quân phục xanh lá: nhuộm vật liệu thân; bỏ mũ sắt, ba lô của lính Tây
            var uniform = TintedCopy("BoDoiQuanPhuc", new Color(0.55f, 0.78f, 0.45f));
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string n = r.name.ToLowerInvariant();
                if (n.Contains("helmet") || n.Contains("backpack")) { r.enabled = false; continue; }
                if (uniform)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = TintedCopy("BoDoi_" + (mats[i] ? mats[i].name : "mat"), new Color(0.55f, 0.78f, 0.45f), mats[i]);
                    r.sharedMaterials = mats;
                }
            }

            if (!model.TryGetComponent<Animator>(out var animator)) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SoldierControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (!model.GetComponent<EnemyAnimatorDriver>()) model.AddComponent<EnemyAnimatorDriver>();

            // Mũ cối trên đầu
            var head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head)
            {
                var helmet = new GameObject("MuCoi");
                helmet.transform.SetParent(head, false);
                helmet.transform.position = head.position + Vector3.up * 0.075f + root.transform.forward * 0.01f;
                helmet.transform.rotation = root.transform.rotation;
                var lossy = head.lossyScale;
                helmet.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
                helmet.AddComponent<MeshFilter>().sharedMesh = SaveFlora("MuCoi", PithHelmet());
                helmet.AddComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    LoadOrMakeMat("MuCoiVai", new Color(0.42f, 0.47f, 0.3f)),
                    LoadOrMakeMat("BangMuCoi", new Color(0.3f, 0.34f, 0.2f)),
                };
            }
            // Súng trường trên tay phải
            var hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var rifle = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            if (hand && rifle)
            {
                var gun = (GameObject)PrefabUtility.InstantiatePrefab(rifle, hand);
                gun.transform.localPosition = Vector3.zero;
                gun.transform.localRotation = Quaternion.Euler(0, 90, 0);
                foreach (var c in gun.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0.2f, 1.35f, 0.7f);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f; agent.height = 1.75f; agent.speed = 2.2f; agent.angularSpeed = 360f; agent.stoppingDistance = 0.8f;
            agent.avoidancePriority = 70;
            root.AddComponent<ComradeFollower>().muzzle = muzzle;
            UrpMaterialUtility.Apply(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ComradePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Material TintedCopy(string name, Color tint, Material source = null)
        {
            string path = $"Assets/_Project/Art/Materials/Greybox/{name.Replace(' ', '_')}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                if (!source) return null;
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (source) mat.CopyPropertiesFromMaterial(source);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// Mũ cối (mũ bấc bọc vải): chỏm tròn, vành rộng xuống dốc, băng vải, núm thông hơi trên đỉnh.
        static Mesh PithHelmet()
        {
            var m = new PartsMesh(2);
            // Chỏm tròn: 8 vòng theo đường cong elip (rộng 0,135 m, cao 0,13 m)
            const int rings = 8; const float r0 = 0.135f, h = 0.13f;
            for (int i = 0; i < rings; i++)
            {
                float t0 = i / (float)rings, t1 = (i + 1) / (float)rings;
                float rr = r0 * Mathf.Sqrt(1f - t0 * t0);
                m.Rod(new Vector3(0, t0 * h, 0), new Vector3(0, t1 * h, 0), Mathf.Max(0.02f, rr), 0);
            }
            // Vành mũ dốc: hai đĩa chồng, đĩa dưới rộng hơn, trước/sau dài hơn hai bên
            m.Rod(new Vector3(0, -0.004f, 0), new Vector3(0, 0.008f, 0), 0.17f, 0);
            m.Rod(new Vector3(0, -0.022f, 0), new Vector3(0, -0.012f, 0), 0.2f, 0);
            m.Box(new Vector3(0, -0.017f, 0.15f), new Vector3(0.22f, 0.01f, 0.12f), Quaternion.Euler(12, 0, 0), 0);
            m.Box(new Vector3(0, -0.017f, -0.15f), new Vector3(0.22f, 0.01f, 0.12f), Quaternion.Euler(-12, 0, 0), 0);
            // Băng vải quanh chân chỏm, núm thông hơi trên đỉnh
            m.Rod(new Vector3(0, 0.008f, 0), new Vector3(0, 0.03f, 0), 0.138f, 1);
            m.Rod(new Vector3(0, h - 0.005f, 0), new Vector3(0, h + 0.018f, 0), 0.022f, 0);
            return m.Build("MuCoi");
        }

        /// 3 đồng đội đi cùng người chơi (sau lưng, hai bên), dựng lại mỗi lần nối scene.
        static int WireComrades()
        {
            var old = GameObject.Find("DongDoi");
            if (old) Object.DestroyImmediate(old);
            var prefab = ComradePrefab(!comradeBuilt);
            comradeBuilt = true;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (!prefab || !player) return 0;
            var root = new GameObject("DongDoi").transform;
            var fwd = player.transform.forward; fwd.y = 0; fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, fwd);
            var slots = new[] { new Vector2(-2.5f, -1.5f), new Vector2(2.8f, -2.2f), new Vector2(-1f, -4f) };
            int n = 0;
            foreach (var slot in slots)
            {
                var pos = player.transform.position + right * slot.x + fwd * slot.y;
                if (!NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas) && !GroundHit(pos, out _)) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                go.name = "BoDoi_" + n;
                go.transform.position = NavMesh.SamplePosition(pos, out hit, 4f, NavMesh.AllAreas) ? hit.position : pos;
                go.transform.rotation = Quaternion.LookRotation(fwd);
                go.GetComponent<ComradeFollower>().slot = slot;
                n++;
            }
            return n;
        }
    }
}
