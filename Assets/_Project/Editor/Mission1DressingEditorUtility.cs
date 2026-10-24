using System;
using System.Collections.Generic;
using DBP.Core;
using DBP.Interaction;
using DBP.Missions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.EditorTools
{
    /// Đưa dressing Trench Map vào Environment của Màn 1, giữ Mission / GameSystems / Zone.
    /// Hào chữ chi được dịch để đáy hào nằm y = 0 và miệng hào trùng điểm xuất phát greybox (z = 2).
    /// Dòng lệnh: -executeMethod DBP.EditorTools.Mission1DressingEditorUtility.BuildCI
    public static class Mission1DressingEditorUtility
    {
        const string M1Path = "Assets/_Project/Scenes/M1_HimLam.unity";
        const string TrenchPath = "Assets/_Project/Scenes/Trench Map.unity";
        const int CrateTarget = 12;

        static readonly string[] DressingRoots =
        {
            "Terrain_TrenchMap", "Trench_Ground", "Trench_Structure", "Trench_Fortifications", "Trench_Storytelling",
        };

        [MenuItem("DBP/Màn 1/Đưa dressing Trench Map vào Him Lam")]
        public static void ApplyMenu() => Apply();

        public static void BuildCI() => Apply();

        public static void Apply()
        {
            var m1 = EditorSceneManager.OpenScene(M1Path, OpenSceneMode.Single);
            var trench = EditorSceneManager.OpenScene(TrenchPath, OpenSceneMode.Additive);
            try
            {
                var env = FindRoot(m1, "Environment");
                if (!env) throw new InvalidOperationException("M1 thiếu Environment.");
                if (!FindRoot(m1, "Mission")) throw new InvalidOperationException("M1 thiếu Mission.");
                if (!FindRoot(m1, "GameSystems")) throw new InvalidOperationException("M1 thiếu GameSystems.");

                foreach (var name in DressingRoots)
                {
                    var stale = FindChild(env.transform, name);
                    if (stale) UnityEngine.Object.DestroyImmediate(stale.gameObject);
                }

                float zShift = 2f - TrenchTerrainEditorUtility.Route[0].y;
                var offset = new Vector3(0f, -TrenchTerrainEditorUtility.Floor, zShift);
                Terrain terrain = null;

                foreach (var name in DressingRoots)
                {
                    var src = FindRoot(trench, name);
                    if (!src) throw new InvalidOperationException($"Trench Map thiếu root '{name}'.");
                    var clone = UnityEngine.Object.Instantiate(src);
                    clone.name = name;
                    EditorSceneManager.MoveGameObjectToScene(clone, m1);
                    clone.transform.SetParent(env.transform, true);
                    clone.transform.position += offset;

                    var srcTerrain = src.GetComponent<Terrain>();
                    if (srcTerrain)
                    {
                        terrain = clone.GetComponent<Terrain>();
                        var col = clone.GetComponent<TerrainCollider>();
                        terrain.terrainData = srcTerrain.terrainData;
                        if (col) col.terrainData = srcTerrain.terrainData;
                    }

                    UrpMaterialUtility.Apply(clone);
                    foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                }

                if (!terrain) throw new InvalidOperationException("Không copy được Terrain_TrenchMap.");

                RemoveGreybox(env.transform);
                DisableGround(m1);
                SeatActors(m1, terrain);
                PlaceCratePoints(m1, terrain, zShift);
                PlacePlayer(m1, terrain, zShift);
                AlignZones(m1, terrain);
                DressAtmosphere(m1, env.transform, terrain, zShift);

                EditorSceneManager.MarkSceneDirty(m1);
                EditorSceneManager.SaveScene(m1);
                Debug.Log($"[M1] Dressing đã vào Him Lam. Offset {offset}. Player và Zone giữ trong Mission.");
            }
            finally
            {
                if (trench.IsValid())
                    EditorSceneManager.CloseScene(trench, true);
            }
        }

        static void RemoveGreybox(Transform env)
        {
            var kill = new List<GameObject>();
            for (int i = 0; i < env.childCount; i++)
            {
                var child = env.GetChild(i);
                var n = child.name;
                if (n.StartsWith("Hao_") || n.StartsWith("RaoKemGai_") || n.StartsWith("HoBom_"))
                    kill.Add(child.gameObject);
            }
            foreach (var go in kill) UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[M1] Gỡ {kill.Count} khối greybox (hào, rào kẽm, hố bom).");
        }

        static void DisableGround(Scene scene)
        {
            var ground = FindRoot(scene, "Ground");
            if (!ground) return;
            foreach (var col in ground.GetComponents<Collider>()) col.enabled = false;
            foreach (var renderer in ground.GetComponents<Renderer>()) renderer.enabled = false;
        }

        static void SeatActors(Scene scene, Terrain terrain)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Enemies" && root.name != "Crates") continue;
                foreach (var col in root.GetComponentsInChildren<Collider>(true))
                {
                    var go = col.gameObject;
                    float above = go.transform.position.y - col.bounds.min.y;
                    var p = go.transform.position;
                    p.y = Surface(terrain, p) + above;
                    go.transform.position = p;
                }
            }
        }

        static void PlacePlayer(Scene scene, Terrain terrain, float zShift)
        {
            var start = TrenchTerrainEditorUtility.Route[0];
            var pos = new Vector3(start.x, 0f, start.y + zShift + 1f);
            pos.y = Surface(terrain, pos) + 0.05f;
            var player = FindPlayer(scene);
            if (!player) throw new InvalidOperationException("M1 thiếu Player.");
            player.transform.SetPositionAndRotation(pos, Quaternion.identity);
            Debug.Log($"[M1] Player tại {pos}.");
        }

        static void AlignZones(Scene scene, Terrain terrain)
        {
            var mission = FindRoot(scene, "Mission");
            if (!mission) return;
            float endX = TrenchTerrainEditorUtility.Route[TrenchTerrainEditorUtility.Route.Length - 1].x;
            foreach (var zone in mission.GetComponentsInChildren<ReachZoneObjective>(true))
            {
                if (!zone.zone) continue;
                var t = zone.zone.transform;
                var p = t.position;
                if (zone.Title != null && zone.Title.IndexOf("tập kết", StringComparison.Ordinal) >= 0)
                    p.x = endX;
                p.y = Surface(terrain, p) + 1f;
                t.position = p;
                Debug.Log($"[M1] Zone '{zone.Title}' tại {t.position}.");
            }
        }

        static void PlaceCratePoints(Scene scene, Terrain terrain, float zShift)
        {
            var poly = RouteWorld(zShift);
            float length = 0f;
            for (int i = 0; i < poly.Count - 1; i++) length += Vector3.Distance(poly[i], poly[i + 1]);

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var spawner in root.GetComponentsInChildren<CrateSpawner>(true))
                {
                    int count = spawner.points.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var point = spawner.points[i];
                        if (!point) continue;
                        float dist = length * (i + 1) / (count + 1);
                        var at = PointAlong(poly, dist, out var forward);
                        var side = Vector3.Cross(Vector3.up, forward) * (i % 2 == 0 ? 0.55f : -0.55f);
                        var pos = at + side;
                        pos.y = Surface(terrain, pos) + 0.2f;
                        point.position = pos;
                    }
                    Debug.Log($"[M1] {count} điểm hòm nằm dọc hào.");
                }

                var phone = FindNamed(root.transform, "DienThoai_GoiPhao");
                if (!phone) continue;
                var spot = PointAlong(poly, 18f, out var phoneForward);
                spot += Vector3.Cross(Vector3.up, phoneForward) * 0.7f;
                spot.y = Surface(terrain, spot) + 0.15f;
                phone.position = spot;
            }
        }

        static Transform FindNamed(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        static void DressAtmosphere(Scene scene, Transform env, Terrain terrain, float zShift)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.55f, 0.52f, 0.45f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.4f, 0.36f);

            var existing = FindChild(env, "AnhSangPhao");
            if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var lightGo = new GameObject("AnhSangPhao");
            lightGo.transform.SetParent(env, false);
            lightGo.transform.position = new Vector3(0f, 18f, 40f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.35f);
            light.range = 90f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<ArtilleryFlash>();

            var cratesRoot = FindRoot(scene, "Crates");
            if (!cratesRoot) return;
            var old = new List<GameObject>();
            for (int i = 0; i < cratesRoot.transform.childCount; i++)
            {
                var child = cratesRoot.transform.GetChild(i);
                if (child.name.StartsWith("Hom_Dress_")) old.Add(child.gameObject);
            }
            foreach (var go in old) UnityEngine.Object.DestroyImmediate(go);

            var crates = cratesRoot.GetComponentsInChildren<QuizCrate>(true);
            if (crates.Length == 0) return;
            var template = crates[0].gameObject;
            var poly = RouteWorld(zShift);
            int made = 0;
            int guard = 0;
            for (float dist = 8f; cratesRoot.GetComponentsInChildren<QuizCrate>(true).Length < CrateTarget && guard < 40; dist += 7f, guard++)
            {
                var at = PointAlong(poly, dist, out var forward);
                if (forward.sqrMagnitude < 0.01f) break;
                bool near = false;
                foreach (var crate in cratesRoot.GetComponentsInChildren<QuizCrate>(true))
                {
                    var c = crate.transform.position;
                    if (new Vector2(c.x - at.x, c.z - at.z).sqrMagnitude < 16f) { near = true; break; }
                }
                if (near) continue;

                var side = Vector3.Cross(Vector3.up, forward).normalized * (made % 2 == 0 ? 0.55f : -0.55f);
                var pos = at + side;
                pos.y = Surface(terrain, pos) + 0.3f;
                var copy = UnityEngine.Object.Instantiate(template, cratesRoot.transform);
                copy.name = $"Hom_Dress_{made}";
                copy.transform.position = pos;
                copy.transform.rotation = Quaternion.identity;
                var quiz = copy.GetComponent<QuizCrate>();
                quiz.rarity = made % 3 == 2 ? Rarity.Uncommon : Rarity.Common;
                quiz.rewardWeaponId = "";
                quiz.rewardAmmo = 15;
                quiz.rewardGrenades = 0;
                made++;
            }

            int total = cratesRoot.GetComponentsInChildren<QuizCrate>(true).Length;
            if (total < 10 || total > 15)
                throw new InvalidOperationException($"M1 cần 10–15 điểm hòm, đang có {total}.");
            Debug.Log($"[M1] {total} điểm hòm, sương và ánh sáng pháo.");
        }

        static List<Vector3> RouteWorld(float zShift)
        {
            var list = new List<Vector3>(TrenchTerrainEditorUtility.Route.Length);
            foreach (var p in TrenchTerrainEditorUtility.Route)
                list.Add(new Vector3(p.x, 0f, p.y + zShift));
            return list;
        }

        static Vector3 PointAlong(List<Vector3> poly, float distance, out Vector3 forward)
        {
            float left = distance;
            for (int i = 0; i < poly.Count - 1; i++)
            {
                var delta = poly[i + 1] - poly[i];
                float len = delta.magnitude;
                if (left <= len || i == poly.Count - 2)
                {
                    forward = len > 0.01f ? delta / len : Vector3.forward;
                    float t = len > 0.01f ? Mathf.Clamp01(left / len) : 0f;
                    return Vector3.Lerp(poly[i], poly[i + 1], t);
                }
                left -= len;
            }
            forward = Vector3.zero;
            return poly.Count > 0 ? poly[poly.Count - 1] : Vector3.zero;
        }

        static float Surface(Terrain terrain, Vector3 world)
        {
            return terrain.transform.position.y + terrain.SampleHeight(world);
        }

        static GameObject FindPlayer(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.CompareTag("Player")) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.CompareTag("Player")) return t.gameObject;
            }
            return null;
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name) return go;
            return null;
        }

        static Transform FindChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }
    }
}
