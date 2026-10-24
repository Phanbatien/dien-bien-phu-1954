using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DBP.EditorTools
{
    /// Chụp ảnh xem trước model (nhìn ngang + chéo) để chọn và kiểm tra model mà không cần mở Editor.
    /// -executeMethod DBP.EditorTools.ModelPreview.RenderCI -previewOut <thư mục> -previewModels a.fbx;b.prefab
    public static class ModelPreview
    {
        public static void RenderCI()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string outDir = Arg(args, "-previewOut"), list = Arg(args, "-previewModels");
                Directory.CreateDirectory(outDir);
                foreach (var path in list.Split(';'))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Trim());
                    if (!prefab) { Debug.LogError("[Preview] không thấy " + path); continue; }
                    Render(prefab, Path.Combine(outDir, Path.GetFileNameWithoutExtension(path) + ".png"));
                }
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
        }

        public static void Render(GameObject prefab, string file)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.75f, 0.78f, 0.8f);
            cam.fieldOfView = 30; cam.nearClipPlane = 0.01f;
            var rt = new RenderTexture(1600, 800, 24); var tex = new Texture2D(1600, 800, TextureFormat.RGB24, false);
            float dist = b.extents.magnitude * 4.2f;
            var shots = new[] { new Vector3(1, 0.15f, 0), new Vector3(0, 0.15f, 1), new Vector3(0.7f, 0.5f, 0.7f) };
            var sheet = new Texture2D(1600, 800 * shots.Length, TextureFormat.RGB24, false);
            for (int i = 0; i < shots.Length; i++)
            {
                cam.transform.position = b.center + shots[i].normalized * dist;
                cam.transform.LookAt(b.center);
                cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1600, 800), 0, 0); tex.Apply();
                sheet.SetPixels(0, 800 * (shots.Length - 1 - i), 1600, 800, tex.GetPixels());
            }
            RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes(file, sheet.EncodeToPNG());
            Debug.Log($"[Preview] {file} kích thước {b.size} tâm {b.center}");
        }
    }
}
