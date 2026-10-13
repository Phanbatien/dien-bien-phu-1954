using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DBP.Player;
using DBP.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DBP.EditorTools
{
    /// Dựng prefab tay thứ nhất từ Infantry + WWII Rifle A, rồi gắn vào Player.
    /// Không sửa FBX hay prefab gốc của gói ngoài.
    public static class PlayerViewModelBuilder
    {
        const string ModelPath = "Assets/tt-3d/LowPolyWarStarterPack/Character/Models/Infantry.fbx";
        const string ArmMeshPath = "Assets/_Project/Art/ViewModel/InfantryArms.asset";
        const string RiflePath = "Assets/Low Poly Weapon Series V 4/Prefabs/Low Poly Series V 4 New/Weapons/WWII_Rifle_A.prefab";
        const string ViewPath = "Assets/_Project/Prefabs/ViewModel_Arms.prefab";
        const string PlayerPath = "Assets/_Project/Prefabs/Player.prefab";
        const string RightUpper = "upper_arm.R";
        const string RightLower = "lower_arm.R";
        const string RightHand = "hand.R";
        const string LeftUpper = "upper_arm.L";
        const string LeftLower = "lower_arm.L";
        const string LeftHand = "hand.L";

        static readonly Vector3 HeadInCamera = new Vector3(0f, -0.04f, 0.08f);
        const float TriggerForward = 0.22f;
        const float TriggerInward = 0.08f;
        const float TriggerDown = 0.04f;

        static Scene preview;

        [MenuItem("DBP/Player/Dựng view model tay")]
        public static void BuildMenu() => Build();

        public static void BuildCI() => Build();

        public static void Build()
        {
            preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var viewPrefab = BuildViewModel();
                VerifyViewModel(viewPrefab);
                WirePlayer(viewPrefab);
                VerifyPlayer();
                AssetDatabase.SaveAssets();
                Debug.Log("[ViewModel] Đã gắn tay Infantry và WWII Rifle A vào Player.");
            }
            finally
            {
                if (preview.IsValid())
                    EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static GameObject BuildViewModel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var rifleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePath);
            if (!model) throw new InvalidOperationException($"Thiếu model {ModelPath}");
            if (!rifleAsset) throw new InvalidOperationException($"Thiếu súng {RiflePath}");
            RequireReadable(ModelPath);

            var root = Spawn(model);
            root.name = "ViewModel_Arms";
            Unpack(root);

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera);
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            KeepArmsOnly(root);

            var rifle = Spawn(rifleAsset);
            rifle.name = "WWII_Rifle_A";
            Unpack(rifle);
            foreach (var collider in rifle.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            var space = root.transform;
            var rightUpper = FindBone(space, RightUpper);
            var rightLower = FindBone(space, RightLower);
            var rightHand = FindBone(space, RightHand);
            var leftUpper = FindBone(space, LeftUpper);
            var leftLower = FindBone(space, LeftLower);
            var leftHand = FindBone(space, LeftHand);

            var posed = new[] { rightUpper, rightLower, leftUpper, leftLower };
            var bind = new Quaternion[posed.Length];
            for (int i = 0; i < posed.Length; i++)
                bind[i] = posed[i].localRotation;

            var rightTarget = rightUpper.position
                + space.forward * TriggerForward
                - space.right * TriggerInward
                - space.up * TriggerDown;
            rightTarget = ClampReach(rightUpper, rightLower, rightHand, rightTarget, 0.9f);
            SolveArm(rightUpper, rightLower, rightHand, rightTarget, rightUpper.position + space.right * 0.35f - space.up * 0.25f);

            var trigger = FindBone(rifle.transform, "WWII_Rifle_A_Trigger");
            Vector3 triggerLocal = trigger.localPosition;
            rifle.transform.SetParent(rightHand, false);
            rifle.transform.localRotation = Quaternion.Inverse(rightHand.rotation) * space.rotation;
            rifle.transform.localPosition = Vector3.zero;
            rifle.transform.position += rightHand.position - rifle.transform.TransformPoint(triggerLocal);
            rifle.transform.position += -space.up * 0.015f + space.forward * 0.01f;

            float reach = Vector3.Distance(leftUpper.position, leftLower.position)
                + Vector3.Distance(leftLower.position, leftHand.position);
            float along = ChooseSupportDistance(leftUpper, reach, rifle.transform, triggerLocal.z);
            Vector3 support = rifle.transform.TransformPoint(new Vector3(-0.02f, -0.01f, triggerLocal.z + along));
            support = ClampReach(leftUpper, leftLower, leftHand, support, 0.9f);
            SolveArm(leftUpper, leftLower, leftHand, support, leftUpper.position - space.right * 0.35f - space.up * 0.2f);

            float rightError = Vector3.Distance(rightHand.position, rightTarget);
            float leftError = Vector3.Distance(leftHand.position, support);
            Debug.Log($"[ViewModel] Tay phải lệch {rightError:0.000} m, tay trái lệch {leftError:0.000} m, tay trái cách cò {along:0.00} m.");
            if (rightError > 0.06f)
                throw new InvalidOperationException($"Tay phải không với tới điểm cầm ({rightError:0.00} m).");
            if (leftError > 0.08f)
                throw new InvalidOperationException($"Tay trái không với tới ốp súng ({leftError:0.00} m).");

            UrpMaterialUtility.Apply(root);
            SilenceRenderers(root);

            var offsets = new List<FirstPersonArms.BoneOffset>();
            for (int i = 0; i < posed.Length; i++)
            {
                var delta = Quaternion.Inverse(bind[i]) * posed[i].localRotation;
                if (Quaternion.Angle(Quaternion.identity, delta) < 0.25f)
                {
                    posed[i].localRotation = bind[i];
                    continue;
                }
                offsets.Add(new FirstPersonArms.BoneOffset { boneName = posed[i].name, localOffset = delta });
                posed[i].localRotation = bind[i];
            }

            var arms = root.GetComponent<FirstPersonArms>();
            if (!arms) arms = root.AddComponent<FirstPersonArms>();
            arms.rifle = rifle.transform;
            arms.gripLocalPosition = rifle.transform.localPosition;
            arms.gripLocalRotation = rifle.transform.localRotation;
            arms.pose = offsets.ToArray();
            arms.collapsedBones = new[] { "head", "upper_leg.L", "upper_leg.R" };
            arms.rifleInCamera = new Vector3(0.16f, -0.2f, 0.62f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ViewPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (!prefab) throw new InvalidOperationException($"Không ghi được {ViewPath}");
            return prefab;
        }

        static void VerifyViewModel(GameObject viewPrefab)
        {
            var camGo = new GameObject("VerifyCamera");
            SceneManager.MoveGameObjectToScene(camGo, preview);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            try
            {
                var view = Spawn(viewPrefab);
                view.transform.SetParent(camGo.transform, false);
                PlaceUnderCamera(view.transform);
                var arms = view.GetComponent<FirstPersonArms>();
                arms.PrepareRig();
                arms.Apply();
                AssertHold(cam, arms, view.transform);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }

        static void WirePlayer(GameObject viewPrefab)
        {
            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var cam = player.transform.Find("CameraPivot/MainCamera");
                if (!cam) throw new InvalidOperationException("Player thiếu CameraPivot/MainCamera");

                var old = cam.Find("ViewModel_Placeholder");
                if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var previous = cam.Find("ViewModel_Arms");
                if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);

                var view = Spawn(viewPrefab);
                view.transform.SetParent(cam, false);
                view.name = "ViewModel_Arms";
                PlaceUnderCamera(view.transform);

                var weapons = cam.GetComponent<WeaponController>();
                if (!weapons) throw new InvalidOperationException("MainCamera thiếu WeaponController");
                var so = new SerializedObject(weapons);
                var prop = so.FindProperty("viewModel");
                if (prop == null) throw new InvalidOperationException("WeaponController không có trường viewModel");
                prop.objectReferenceValue = view;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        static void VerifyPlayer()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            var player = Spawn(asset);
            try
            {
                var cam = player.GetComponentInChildren<Camera>();
                var arms = player.GetComponentInChildren<FirstPersonArms>(true);
                if (!cam || !arms) throw new InvalidOperationException("Player chưa có camera hoặc FirstPersonArms");
                arms.PrepareRig();
                arms.Apply();
                AssertHold(cam, arms, arms.transform);

                var weapons = cam.GetComponent<WeaponController>();
                var so = new SerializedObject(weapons);
                var view = so.FindProperty("viewModel").objectReferenceValue as GameObject;
                if (view != arms.gameObject)
                    throw new InvalidOperationException("WeaponController.viewModel không trỏ vào ViewModel_Arms");
                AssertFollowAndScope(player, cam, arms, weapons);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }
        }

        static void AssertFollowAndScope(GameObject player, Camera cam, FirstPersonArms arms, WeaponController weapons)
        {
            var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 0.8f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.transform == player.transform || hit.collider.transform.IsChildOf(player.transform))
                    throw new InvalidOperationException($"Tia bắn trúng {hit.collider.name}");
            }

            var pivot = cam.transform.parent;
            player.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            pivot.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            float angle = Vector3.Angle(arms.rifle.forward, cam.transform.forward);
            if (angle > 5f)
                throw new InvalidOperationException($"Nòng không theo hướng nhìn ({angle:0.0}°)");

            var before = arms.rifle.position;
            pivot.localPosition += Vector3.down * 0.5f;
            if (Mathf.Abs(arms.rifle.position.y - (before.y - 0.5f)) > 0.02f)
                throw new InvalidOperationException("Súng không hạ theo camera khi ngồi");

            var scoped = WeaponCatalog.Get("W-MOSIN-SCOPE");
            if (scoped == null) throw new InvalidOperationException("Thiếu W-MOSIN-SCOPE");
            weapons.Inventory.GiveWeapon(scoped, 20);
            if (weapons.Inventory.Current == null || weapons.Inventory.Current.Def.id != "W-MOSIN-SCOPE")
                throw new InvalidOperationException("Không chuyển được sang súng ống ngắm");

            cam.fieldOfView = 10f;
            SetAiming(weapons, true);
            InvokeUpdateAim(weapons);
            if (arms.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Ống ngắm không ẩn view model");

            SetAiming(weapons, false);
            InvokeUpdateAim(weapons);
            if (!arms.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Thả ngắm không hiện lại view model");
        }

        static void SetAiming(WeaponController weapons, bool aiming)
        {
            typeof(WeaponController).GetProperty("IsAiming").GetSetMethod(true)
                .Invoke(weapons, new object[] { aiming });
        }

        static void InvokeUpdateAim(WeaponController weapons)
        {
            typeof(WeaponController).GetMethod("UpdateAim", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(weapons, new object[] { weapons.Inventory.Current });
        }

        static void PlaceUnderCamera(Transform view)
        {
            view.localRotation = Quaternion.identity;
            view.localScale = Vector3.one;
            var head = FindBone(view, "head");
            Vector3 headInRoot = view.InverseTransformPoint(head.position);
            view.localPosition = HeadInCamera - headInRoot;
        }

        static void AssertHold(Camera cam, FirstPersonArms arms, Transform view)
        {
            if (!arms.rifle) throw new InvalidOperationException("View model chưa gắn súng");
            if (!arms.rifle.IsChildOf(FindBone(view, RightHand)))
                throw new InvalidOperationException("Súng không nằm trên hand.R");

            float angle = Vector3.Angle(arms.rifle.forward, cam.transform.forward);
            if (angle > 12f)
                throw new InvalidOperationException($"Nòng súng lệch camera {angle:0.0}°");

            Vector3 rifleInCam = cam.transform.InverseTransformPoint(arms.rifle.position);
            if (rifleInCam.z < 0.2f || rifleInCam.z > 0.8f || Mathf.Abs(rifleInCam.x) > 0.4f || rifleInCam.y < -0.45f || rifleInCam.y > 0.05f)
                throw new InvalidOperationException($"Súng ngoài tầm nhìn {rifleInCam}");

            foreach (var boneName in new[] { "head", "upper_leg.L", "upper_leg.R" })
            {
                var bone = FindBone(view, boneName);
                if (bone.localScale.x > 0.01f)
                    throw new InvalidOperationException($"{boneName} chưa được thu nhỏ");
            }

            if (arms.GetComponentsInChildren<Collider>(true).Length > 0)
                throw new InvalidOperationException("View model còn collider, sẽ chặn tia bắn");

            float near = MinPositiveDepth(cam, arms.rifle.GetComponentsInChildren<Renderer>());
            if (near < cam.nearClipPlane + 0.01f)
                throw new InvalidOperationException($"Súng cắt mặt phẳng gần của camera (sâu {near:0.00} m)");

            foreach (var renderer in arms.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                    throw new InvalidOperationException($"{renderer.name} vẫn đổ bóng");
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (!mat || !UrpMaterialUtility.IsUrpCompatible(mat))
                        throw new InvalidOperationException($"Vật liệu không tương thích URP trên {renderer.name}");
                }
            }
        }

        static float MinPositiveDepth(Camera cam, Renderer[] renderers)
        {
            float min = float.PositiveInfinity;
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                var center = bounds.center;
                var ext = bounds.extents;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    var corner = center + Vector3.Scale(ext, new Vector3(x, y, z));
                    float depth = cam.transform.InverseTransformPoint(corner).z;
                    if (depth > 0f && depth < min) min = depth;
                }
            }
            return min;
        }

        static float ChooseSupportDistance(Transform shoulder, float reach, Transform rifle, float triggerZ)
        {
            float chosen = 0.08f;
            for (float along = 0.30f; along >= 0.08f; along -= 0.02f)
            {
                Vector3 point = rifle.TransformPoint(new Vector3(-0.02f, -0.01f, triggerZ + along));
                if (Vector3.Distance(shoulder.position, point) <= reach * 0.88f)
                    return along;
            }
            return chosen;
        }

        static Vector3 ClampReach(Transform upper, Transform lower, Transform hand, Vector3 target, float fraction)
        {
            float reach = (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position)) * fraction;
            Vector3 delta = target - upper.position;
            if (delta.magnitude <= reach || delta.sqrMagnitude < 1e-6f) return target;
            return upper.position + delta.normalized * reach;
        }

        static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 pole)
        {
            for (int i = 0; i < 12; i++)
            {
                PointBone(lower, hand, target);
                PointBone(upper, hand, target);
                SwingElbow(upper, lower, hand, pole);
            }
        }

        static void PointBone(Transform bone, Transform end, Vector3 target)
        {
            Vector3 toEnd = end.position - bone.position;
            Vector3 toTarget = target - bone.position;
            if (toEnd.sqrMagnitude < 1e-8f || toTarget.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(toEnd, toTarget) * bone.rotation;
        }

        static void SwingElbow(Transform upper, Transform lower, Transform hand, Vector3 pole)
        {
            Vector3 axis = hand.position - upper.position;
            if (axis.sqrMagnitude < 1e-6f) return;
            Vector3 current = Vector3.ProjectOnPlane(lower.position - upper.position, axis);
            Vector3 desired = Vector3.ProjectOnPlane(pole - upper.position, axis);
            if (current.sqrMagnitude < 1e-6f || desired.sqrMagnitude < 1e-6f) return;
            upper.rotation = Quaternion.FromToRotation(current, desired) * upper.rotation;
        }

        static void SilenceRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var so = new SerializedObject(renderer);
                var culling = so.FindProperty("m_SmallMeshCulling");
                if (culling != null)
                {
                    culling.intValue = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (renderer is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen = true;
                    skin.localBounds = new Bounds(new Vector3(0f, 1.2f, 0.45f), new Vector3(3f, 3f, 3f));
                }
            }
        }

        static void RequireReadable(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || !importer.isReadable)
                throw new InvalidOperationException($"Bật Read/Write trên {path} rồi dựng lại view model.");
        }

        /// Bộ binh là một mesh. Giữ đỉnh thuộc vai, tay, bàn tay; tắt mũ và ba lô vì chúng nằm trong camera.
        static void KeepArmsOnly(GameObject root)
        {
            SkinnedMeshRenderer body = null;
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.gameObject.name == "Infantry_helmet" || skin.gameObject.name == "Infantry_backpack")
                {
                    skin.gameObject.SetActive(false);
                    continue;
                }
                if (body) throw new InvalidOperationException($"Nhiều mesh thân: {body.name} và {skin.name}");
                body = skin;
            }
            if (!body) throw new InvalidOperationException("Không thấy mesh Infantry");

            var mesh = ExtractArmMesh(body.sharedMesh, body.bones);
            var dir = Path.GetDirectoryName(ArmMeshPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(ArmMeshPath))
                AssetDatabase.DeleteAsset(ArmMeshPath);
            AssetDatabase.CreateAsset(mesh, ArmMeshPath);
            body.sharedMesh = mesh;
        }

        static Mesh ExtractArmMesh(Mesh src, Transform[] bones)
        {
            if (!src || !src.isReadable)
                throw new InvalidOperationException("Mesh Infantry không đọc được.");
            if (bones == null || bones.Length == 0)
                throw new InvalidOperationException("Mesh Infantry không có xương.");

            var keepBone = new bool[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                var name = bones[i] ? bones[i].name : "";
                keepBone[i] = name.StartsWith("shoulder", StringComparison.Ordinal)
                    || name.StartsWith("upper_arm", StringComparison.Ordinal)
                    || name.StartsWith("lower_arm", StringComparison.Ordinal)
                    || name.StartsWith("hand.", StringComparison.Ordinal)
                    || name.StartsWith("thumb_", StringComparison.Ordinal)
                    || name.StartsWith("middle_", StringComparison.Ordinal);
            }

#pragma warning disable CS0618
            var weights = src.boneWeights;
#pragma warning restore CS0618
            if (weights == null || weights.Length != src.vertexCount)
                throw new InvalidOperationException("Mesh Infantry thiếu trọng số xương.");

            var remap = new int[src.vertexCount];
            int count = 0;
            for (int v = 0; v < src.vertexCount; v++)
            {
                int bone = DominantBone(weights[v]);
                bool keep = bone >= 0 && bone < keepBone.Length && keepBone[bone];
                remap[v] = keep ? count++ : -1;
            }

            var srcTris = src.triangles;
            var tris = new List<int>(srcTris.Length);
            for (int t = 0; t < srcTris.Length; t += 3)
            {
                int a = remap[srcTris[t]];
                int b = remap[srcTris[t + 1]];
                int c = remap[srcTris[t + 2]];
                if (a < 0 || b < 0 || c < 0) continue;
                tris.Add(a);
                tris.Add(b);
                tris.Add(c);
            }
            if (tris.Count < 30)
                throw new InvalidOperationException($"Mesh tay quá ít tam giác ({tris.Count / 3}).");

            var srcVerts = src.vertices;
            var srcNormals = src.normals;
            var srcUv = src.uv;
            var verts = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var kept = new BoneWeight[count];
            bool hasNormals = srcNormals != null && srcNormals.Length == src.vertexCount;
            bool hasUv = srcUv != null && srcUv.Length == src.vertexCount;
            for (int v = 0; v < src.vertexCount; v++)
            {
                int i = remap[v];
                if (i < 0) continue;
                verts[i] = srcVerts[v];
                if (hasNormals) normals[i] = srcNormals[v];
                if (hasUv) uv[i] = srcUv[v];
                kept[i] = weights[v];
            }

            var mesh = new Mesh { name = "InfantryArms" };
            mesh.vertices = verts;
            if (hasNormals) mesh.normals = normals;
            if (hasUv) mesh.uv = uv;
#pragma warning disable CS0618
            mesh.boneWeights = kept;
#pragma warning restore CS0618
            mesh.bindposes = src.bindposes;
            mesh.triangles = tris.ToArray();
            if (!hasNormals) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log($"[ViewModel] Giữ {count} đỉnh, {tris.Count / 3} tam giác tay Infantry.");
            return mesh;
        }

        static int DominantBone(BoneWeight weight)
        {
            float best = weight.weight0;
            int bone = weight.boneIndex0;
            if (weight.weight1 > best) { best = weight.weight1; bone = weight.boneIndex1; }
            if (weight.weight2 > best) { best = weight.weight2; bone = weight.boneIndex2; }
            if (weight.weight3 > best) bone = weight.boneIndex3;
            return bone;
        }

        static GameObject Spawn(GameObject asset)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            if (preview.IsValid())
                SceneManager.MoveGameObjectToScene(go, preview);
            return go;
        }

        static void Unpack(GameObject instance)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        static Transform FindBone(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            throw new InvalidOperationException($"Không thấy '{name}' trong {root.name}");
        }
    }
}
