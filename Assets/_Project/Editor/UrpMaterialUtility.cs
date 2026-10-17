using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DBP.EditorTools
{
    /// Converts Built-in / Amplify materials on spawned prefabs to URP Lit (cached under UrpFallback).
    /// Vendor .mat files are not modified. For higher-fidelity weapons, import
    /// Low Poly Weapon Series V 4_URP.unitypackage manually.
    public static class UrpMaterialUtility
    {
        const string LitGuid = "933532a4fcc9baf4fa0491de14d08ed7";
        const string TerrainLitShader = "Universal Render Pipeline/Terrain/Lit";
        const string TrenchScenePath = "Assets/_Project/Scenes/Trench Map.unity";
        const string OutDir = "Assets/_Project/Art/Materials/UrpFallback";
        const string AuditLogPath = "Logs/urp-material-audit.log";

        static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>();
        static Shader lit;

        [MenuItem("DBP/Trench Map/Sửa shader URP cho scene")]
        public static void FixOpenScene()
        {
            Cache.Clear();
            int slots = FixSceneRenderers();
            ApplyTerrains();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[UrpMaterial] Đã gán lại {slots} material slot trong scene.");
        }

        /// Batch: audit Trench Map, fix materials, save scene, write Logs/urp-material-audit.log
        public static void FixTrenchMapCI()
        {
            try
            {
                Cache.Clear();
                OpenTrenchScene();
                var report = new StringBuilder();
                int before = AppendAudit(report, "BEFORE");
                int fixedSlots = FixSceneRenderers();
                ApplyTerrains();
                int after = AppendAudit(report, "AFTER");
                report.AppendLine($"FixedSlots={fixedSlots} Before={before} After={after}");
                WriteAuditLog(report.ToString());
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Debug.Log(report.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(after > 0 ? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// Batch: audit only (no changes). Exit 1 if any incompatible material slot remains.
        public static void AuditTrenchMapCI()
        {
            try
            {
                OpenTrenchScene();
                var report = new StringBuilder();
                int count = AppendAudit(report, "AUDIT");
                WriteAuditLog(report.ToString());
                Debug.Log(report.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(count > 0 ? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void OpenTrenchScene()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == TrenchScenePath) return;
            if (active.isDirty && !Application.isBatchMode)
                throw new InvalidOperationException($"Scene '{active.name}' chưa lưu. Lưu lại rồi mở {TrenchScenePath}.");
            EditorSceneManager.OpenScene(TrenchScenePath);
        }

        static int FixSceneRenderers()
        {
            int slots = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                slots += ApplyRenderer(renderer);
            return slots;
        }

        public static void Apply(GameObject root)
        {
            if (!root) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                ApplyRenderer(renderer);
        }

        static int ApplyRenderer(Renderer renderer)
        {
            var slots = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (IsUrpCompatible(slots[i])) continue;
                slots[i] = Convert(slots[i], renderer.name);
                changed = true;
            }
            if (!changed) return 0;
            renderer.sharedMaterials = slots;
            CommitRendererMaterials(renderer);
            return slots.Length;
        }

        static void CommitRendererMaterials(Renderer renderer)
        {
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(renderer.gameObject);
            if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        [MenuItem("DBP/Trench Map/Kiểm tra shader (ghi log)")]
        public static void AuditOpenSceneMenu()
        {
            var report = new StringBuilder();
            int count = AppendAudit(report, "AUDIT");
            WriteAuditLog(report.ToString());
            Debug.Log(report.ToString());
            if (count > 0)
                Debug.LogWarning($"[UrpMaterial] Còn {count} slot không tương thích URP. Xem {AuditLogPath}");
        }

        public static bool IsUrpCompatible(Material mat)
        {
            if (!mat || !mat.shader) return false;
            var name = mat.shader.name;
            if (name == "Hidden/InternalErrorShader") return false;
            if (!mat.shader.isSupported) return false;
            if (name.StartsWith("Universal Render Pipeline/")) return true;
            if (name.StartsWith("Shader Graphs/"))
            {
                for (int i = 0; i < mat.shader.passCount; i++)
                {
                    var tag = mat.shader.FindPassTagValue(i, new ShaderTagId("RenderPipeline"));
                    if (tag.name == "UniversalPipeline") return true;
                }
            }
            return false;
        }

        public static Material EnsureUrpLitMaterial(string assetPath, Action<Material> configure)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (!mat)
            {
                mat = new Material(UrpLit());
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else if (!IsUrpCompatible(mat))
                mat.shader = UrpLit();

            configure?.Invoke(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Convert(Material source, string contextName)
        {
            if (source && Cache.TryGetValue(source.GetInstanceID(), out var cached))
                return cached;

            var existingPath = source ? FallbackAssetPath(source) : null;
            if (!string.IsNullOrEmpty(existingPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<Material>(existingPath);
                if (existing && IsUrpCompatible(existing))
                {
                    if (source) Cache[source.GetInstanceID()] = existing;
                    return existing;
                }
                if (existing)
                    AssetDatabase.DeleteAsset(existingPath);
            }

            var mat = new Material(UrpLit()) { name = source ? source.name + "_urp" : Sanitize(contextName) + "_Fallback" };
            if (source) CopySurface(source, mat);
            else mat.SetColor("_BaseColor", new Color(0.45f, 0.42f, 0.36f));

            string path = string.IsNullOrEmpty(existingPath)
                ? AssetDatabase.GenerateUniqueAssetPath($"{OutDir}/{Sanitize(mat.name)}.mat")
                : existingPath;
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);
            AssetDatabase.CreateAsset(mat, path);

            if (source) Cache[source.GetInstanceID()] = mat;
            return mat;
        }

        static string FallbackAssetPath(Material source)
        {
            var srcPath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(srcPath)) return null;
            var key = srcPath.StartsWith("Assets/") ? srcPath.Substring(7) : srcPath;
            key = key.Replace('/', '_').Replace('\\', '_');
            foreach (var c in Path.GetInvalidFileNameChars())
                key = key.Replace(c, '_');
            return $"{OutDir}/{key}";
        }

        static void CopySurface(Material source, Material mat)
        {
            var shaderName = source.shader ? source.shader.name : "";
            bool foliage = shaderName.IndexOf("Foliage", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("Leaf", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0;
            bool transparent = !foliage && (shaderName.IndexOf("Lens", StringComparison.OrdinalIgnoreCase) >= 0
                || shaderName.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0
                || (source.HasProperty("_Mode") && source.GetFloat("_Mode") >= 2f));
            bool cutout = foliage || (source.HasProperty("_Mode") && Mathf.Approximately(source.GetFloat("_Mode"), 1f));

            var albedo = FirstTexture(source, "_BaseMap", "_MainTex", "_BaseTexture", "_BaseColorMap", "_TextureSample2");
            if (albedo.texture)
            {
                mat.SetTexture("_BaseMap", albedo.texture);
                mat.SetTextureScale("_BaseMap", albedo.scale);
                mat.SetTextureOffset("_BaseMap", albedo.offset);
            }

            var normal = FirstTexture(source, "_BumpMap", "_NormalMap");
            if (normal.texture)
            {
                mat.SetTexture("_BumpMap", normal.texture);
                mat.EnableKeyword("_NORMALMAP");
            }

            Color color = Color.white;
            if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color")) color = source.GetColor("_Color");
            else if (shaderName.IndexOf("Buildings", StringComparison.OrdinalIgnoreCase) >= 0 && source.HasProperty("_Wood1color"))
                color = source.GetColor("_Wood1color");
            mat.SetColor("_BaseColor", color);

            float smoothness = source.HasProperty("_Smoothness") ? source.GetFloat("_Smoothness")
                : source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0.2f;
            mat.SetFloat("_Smoothness", smoothness);
            if (source.HasProperty("_Metallic")) mat.SetFloat("_Metallic", source.GetFloat("_Metallic"));

            if (cutout)
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                mat.SetFloat("_Cull", 0f);
            }
            else if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetFloat("_Cull", 0f);
            }
        }

        struct TexSlot
        {
            public Texture texture;
            public Vector2 scale, offset;
        }

        static TexSlot FirstTexture(Material mat, params string[] names)
        {
            foreach (var name in names)
            {
                if (!mat.HasProperty(name)) continue;
                var texture = mat.GetTexture(name);
                if (!texture) continue;
                return new TexSlot { texture = texture, scale = mat.GetTextureScale(name), offset = mat.GetTextureOffset(name) };
            }
            return default;
        }

        static Shader UrpLit()
        {
            if (lit && lit.name != "Hidden/InternalErrorShader") return lit;
            var path = AssetDatabase.GUIDToAssetPath(LitGuid);
            if (!string.IsNullOrEmpty(path)) lit = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (!lit) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!lit || lit.name == "Hidden/InternalErrorShader")
                throw new InvalidOperationException("Không thấy Universal Render Pipeline/Lit. Kiểm tra package URP.");
            return lit;
        }

        static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Material";
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }

        static void ApplyTerrains()
        {
            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mat = terrain.materialTemplate;
                if (mat && IsUrpCompatible(mat)) continue;
                terrain.materialTemplate = UrpTerrainMaterial();
                EditorUtility.SetDirty(terrain);
                if (PrefabUtility.IsPartOfPrefabInstance(terrain))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(terrain);
            }
        }

        static Material UrpTerrainMaterial()
        {
            const string path = OutDir + "/Terrain_Lit_URP.mat";
            return EnsureUrpLitMaterial(path, mat =>
            {
                var shader = Shader.Find(TerrainLitShader);
                if (shader && shader.isSupported) mat.shader = shader;
            });
        }

        static int AppendAudit(StringBuilder report, string label)
        {
            report.AppendLine($"=== {label} ===");
            int count = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    var mat = slots[i];
                    if (IsUrpCompatible(mat)) continue;
                    count++;
                    string matPath = mat ? AssetDatabase.GetAssetPath(mat) : "(null)";
                    string shader = mat && mat.shader ? mat.shader.name : "(null)";
                    bool prefab = PrefabUtility.IsPartOfPrefabInstance(renderer);
                    report.AppendLine(
                        $"[{count}] {HierarchyPath(renderer.transform)} | slot {i} | shader={shader} | mat={matPath} | prefabInstance={prefab}");
                }
            }
            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var mat = terrain.materialTemplate;
                if (IsUrpCompatible(mat)) continue;
                count++;
                report.AppendLine(
                    $"[{count}] {HierarchyPath(terrain.transform)} | terrain materialTemplate | shader={(mat && mat.shader ? mat.shader.name : "(null)")} | mat={(mat ? AssetDatabase.GetAssetPath(mat) : "(null)")}");
            }
            report.AppendLine($"Total incompatible: {count}");
            return count;
        }

        static string HierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        static void WriteAuditLog(string text)
        {
            var dir = Path.GetDirectoryName(AuditLogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(AuditLogPath, text);
        }
    }
}
