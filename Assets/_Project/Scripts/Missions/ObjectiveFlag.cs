using DBP.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.Missions
{
    /// Cờ đỏ sao vàng trên cột tre đánh dấu vùng mục tiêu (thay cột xanh của bản dựng thô).
    /// Chưa tới: cờ treo lưng chừng, hơi rủ, có vệt sáng vàng mờ hắt lên trời để thấy từ xa.
    /// Hoàn thành vùng: cờ kéo lên đỉnh, bay phấp phới, vệt sáng tắt. Tự gắn khi vào màn có mục tiêu theo vùng.
    public sealed class ObjectiveFlag : MonoBehaviour
    {
        const float PoleHeight = 5.5f, FlagW = 1.5f, FlagH = 1.0f;
        const int Cols = 12, Rows = 6;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            var mission = MissionController.Current;
            if (mode != LoadSceneMode.Single || !mission || FindAnyObjectByType<ObjectiveFlag>()) return;
            foreach (var o in mission.Objectives)
            {
                Collider zone = o is ReachZoneObjective r ? r.zone : o is CaptureZoneObjective c ? c.zone : null;
                if (!zone) continue;
                foreach (var t in zone.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Marker" && t.TryGetComponent<Renderer>(out var old)) old.enabled = false; // cột xanh cũ
                var go = new GameObject("CoMucTieu");
                go.transform.position = Ground(zone.bounds.center);
                go.AddComponent<ObjectiveFlag>().objective = o;
            }
        }

        static Vector3 Ground(Vector3 p)
        {
            foreach (var hit in Physics.RaycastAll(p + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.isTrigger) return hit.point;
            return p;
        }

        public Objective objective;
        Transform cloth;
        Mesh mesh;
        Vector3[] baseVerts, verts;
        LineRenderer beam;
        float raise; // 0 = lưng chừng, 1 = đỉnh cột
        static Material bamboo, flagMat;

        void Start()
        {
            if (!bamboo)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                bamboo = new Material(lit); bamboo.SetColor("_BaseColor", new Color(0.62f, 0.55f, 0.32f)); bamboo.SetFloat("_Smoothness", 0.2f);
                flagMat = new Material(lit) { mainTexture = FlagTexture() };
                flagMat.SetTexture("_BaseMap", flagMat.mainTexture);
                flagMat.SetFloat("_Cull", 0f); // vẽ cả hai mặt cờ
                flagMat.SetFloat("_Smoothness", 0.1f);
            }
            // Cột tre: các đốt nối nhau, có mấu
            for (int i = 0; i < 5; i++)
            {
                float y0 = i * PoleHeight / 5f, y1 = (i + 1) * PoleHeight / 5f;
                Prim(PrimitiveType.Cylinder, new Vector3(0, (y0 + y1) / 2f, 0), new Vector3(0.07f, (y1 - y0) / 2f, 0.07f), bamboo);
                Prim(PrimitiveType.Cylinder, new Vector3(0, y1, 0), new Vector3(0.09f, 0.02f, 0.09f), bamboo);
            }
            // Mảnh cờ: lưới đỉnh để làm sóng vải
            cloth = new GameObject("Co").transform;
            cloth.SetParent(transform, false);
            mesh = new Mesh { name = "CoDoSaoVang" };
            baseVerts = new Vector3[(Cols + 1) * (Rows + 1)];
            var uv = new Vector2[baseVerts.Length];
            for (int y = 0; y <= Rows; y++)
                for (int x = 0; x <= Cols; x++)
                {
                    int i = y * (Cols + 1) + x;
                    baseVerts[i] = new Vector3(x / (float)Cols * FlagW, -y / (float)Rows * FlagH, 0);
                    uv[i] = new Vector2(x / (float)Cols, 1f - y / (float)Rows);
                }
            var tris = new int[Cols * Rows * 6];
            for (int y = 0, k = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++, k += 6)
                {
                    int a = y * (Cols + 1) + x, b = a + 1, c = a + Cols + 1, d = c + 1;
                    tris[k] = a; tris[k + 1] = b; tris[k + 2] = c; tris[k + 3] = b; tris[k + 4] = d; tris[k + 5] = c;
                }
            verts = (Vector3[])baseVerts.Clone();
            mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateNormals();
            cloth.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            cloth.gameObject.AddComponent<MeshRenderer>().sharedMaterial = flagMat;

            // Vệt sáng vàng mờ hắt lên trời
            beam = new GameObject("VetSang").AddComponent<LineRenderer>();
            beam.transform.SetParent(transform, false);
            beam.useWorldSpace = false; beam.positionCount = 2;
            beam.SetPosition(0, Vector3.zero); beam.SetPosition(1, Vector3.up * 40f);
            beam.widthCurve = new AnimationCurve(new Keyframe(0, 0.9f), new Keyframe(1, 0.25f));
            beam.sharedMaterial = CombatEffects.SoftMaterial;
            beam.startColor = new Color(1f, 0.82f, 0.35f, 0.45f); beam.endColor = new Color(1f, 0.82f, 0.35f, 0f);
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Prim(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            if (!cloth) return;
            bool done = objective && objective.Done;
            raise = Mathf.MoveTowards(raise, done ? 1f : 0f, Time.deltaTime * 0.6f);
            cloth.localPosition = new Vector3(0.06f, Mathf.Lerp(PoleHeight * 0.6f, PoleHeight - 0.05f, raise), 0);
            if (beam) beam.enabled = !done;

            // Sóng vải: chưa xong thì rủ, gió nhẹ; xong thì căng và phấp phới mạnh
            float wind = Mathf.Lerp(0.12f, 0.32f, raise), droop = Mathf.Lerp(0.35f, 0.05f, raise), t = Time.time;
            for (int i = 0; i < verts.Length; i++)
            {
                var b = baseVerts[i];
                float u = b.x / FlagW;
                float wave = Mathf.Sin(t * (3f + 3f * raise) - u * 6f) * wind * u + Mathf.Sin(t * 5.3f - u * 11f) * 0.04f * u;
                verts[i] = new Vector3(b.x * (1f - droop * 0.4f * u), b.y - droop * u * u * FlagH * 0.6f, wave);
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
        }

        /// Cờ đỏ sao vàng năm cánh, vẽ bằng code.
        static Texture2D FlagTexture()
        {
            const int w = 192, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "CoDoSaoVang", wrapMode = TextureWrapMode.Clamp };
            var red = new Color(0.85f, 0.1f, 0.08f); var gold = new Color(1f, 0.87f, 0.0f);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sx = (x + 0.5f - w / 2f) / (h * 0.3f), sy = (y + 0.5f - h / 2f) / (h * 0.3f);
                    px[y * w + x] = InStar(sx, sy) ? gold : red;
                }
            tex.SetPixels(px); tex.Apply(true);
            return tex;
        }

        /// Điểm (x, y) nằm trong ngôi sao năm cánh bán kính 1, một cánh chĩa lên.
        static bool InStar(float x, float y)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 1f : 0.382f;
                pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > y) != (pts[j].y > y) && x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            return inside;
        }
    }
}
