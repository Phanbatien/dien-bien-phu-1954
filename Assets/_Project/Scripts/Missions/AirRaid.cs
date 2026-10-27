using System.Collections.Generic;
using DBP.AI;
using DBP.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DBP.Missions
{
    /// Máy bay Pháp ném bom ngẫu nhiên trong mỗi màn: lần đầu sau 25–45 s, sau đó 50–90 s một lần.
    /// Máy bay (dáng F8F Bearcat, phù hiệu tròn ba màu) bay ngang ở độ cao 80 m, thả 5 quả bom rơi theo
    /// quán tính xuống một điểm gần người chơi (12–30 m, không trúng ngay chỗ đứng). Bom nổ sát thương mọi bên
    /// trong 7 m (địch chết vì bom không tính công người chơi). Có báo trước để kịp tìm chỗ nấp.
    /// Thời gian đi theo timeScale nên dừng khi trả lời câu hỏi.
    public sealed class AirRaid : MonoBehaviour
    {
        const float Altitude = 80f, Speed = 70f, BlastRadius = 7f, MaxDamage = 75f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && MissionController.Current && s.name.StartsWith("M") && !FindAnyObjectByType<AirRaid>())
                new GameObject("AirRaid").AddComponent<AirRaid>();
        }

        /// Gọi một đợt ném bom ngay (trailer, test).
        public static void Now() { var r = FindAnyObjectByType<AirRaid>(); if (r) r.nextRaid = Time.time; }

        /// Điểm ném bom của đợt gần nhất (camera trailer nhìn vào đây).
        public static Vector3 LastTarget { get; private set; }

        struct Bomb { public Transform t; public Vector3 v; }
        readonly List<Bomb> bombs = new List<Bomb>();
        Transform player, plane, propeller;
        Vector3 dir, target, releasePoint;
        float nextRaid, nextDrop;
        int toDrop;
        static Material body, dark, white, blue, red;

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            nextRaid = Time.time + Random.Range(25f, 45f);
        }

        void Update()
        {
            if (!player || Time.deltaTime <= 0f) return;
            if (!plane && Time.time >= nextRaid) Launch();
            if (plane) Fly();
            for (int i = bombs.Count - 1; i >= 0; i--) if (Fall(i)) bombs.RemoveAt(i);
        }

        void Launch()
        {
            nextRaid = Time.time + Random.Range(50f, 90f);
            float a = Random.value * Mathf.PI * 2f;
            dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var side = Vector3.Cross(Vector3.up, dir) * Random.Range(-1f, 1f);
            target = player.position + (side.normalized * Random.Range(12f, 30f));
            target.y = Ground(target);
            LastTarget = target;
            float fall = Mathf.Sqrt(2f * Altitude / 9.81f);
            releasePoint = target - dir * (Speed * fall);
            plane = BuildPlane().transform;
            plane.position = releasePoint - dir * 300f + Vector3.up * (target.y + Altitude);
            plane.rotation = Quaternion.LookRotation(dir);
            toDrop = 5;
            var engine = CampaignAudio.AttachLoop("DongCoMayBay", "airplane_loop", plane, 1f);
            if (engine) { engine.minDistance = 40f; engine.maxDistance = 500f; }
            DBP.UI.CampaignNotices.Show("Máy bay địch! Tìm chỗ nấp!");
        }

        void Fly()
        {
            plane.position += dir * Speed * Time.deltaTime;
            if (propeller) propeller.Rotate(0, 0, 1800f * Time.deltaTime, Space.Self);
            float along = Vector3.Dot(new Vector3(plane.position.x, 0, plane.position.z) - new Vector3(releasePoint.x, 0, releasePoint.z), dir);
            if (toDrop > 0 && along >= -Speed * 0.5f && Time.time >= nextDrop)
            {
                nextDrop = Time.time + 0.18f;
                toDrop--;
                var b = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(b.GetComponent<Collider>());
                b.name = "Bom";
                b.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
                b.transform.position = plane.position - Vector3.up * 0.8f;
                b.GetComponent<Renderer>().sharedMaterial = dark;
                bombs.Add(new Bomb { t = b.transform, v = dir * Speed });
            }
            if (along > 600f) { Destroy(plane.gameObject); plane = null; }
        }

        bool Fall(int i)
        {
            var b = bombs[i];
            if (!b.t) return true;
            b.v += Vector3.down * 9.81f * Time.deltaTime;
            var step = b.v * Time.deltaTime;
            b.t.rotation = Quaternion.LookRotation(b.v) * Quaternion.Euler(90, 0, 0);
            if (Physics.Raycast(b.t.position, step.normalized, out var hit, step.magnitude + 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                Explode(hit.point);
                Destroy(b.t.gameObject);
                return true;
            }
            b.t.position += step;
            bombs[i] = b;
            if (b.t.position.y < target.y - 50f) { Destroy(b.t.gameObject); return true; }
            return false;
        }

        void Explode(Vector3 at)
        {
            CombatEffects.Explosion(at, 2.4f);
            CampaignAudio.Effect($"explosion_0{Random.Range(1, 5)}", at, true, 1f);
            var hurt = new HashSet<Health>();
            foreach (var c in Physics.OverlapSphere(at, BlastRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                var h = c.GetComponentInParent<Health>();
                if (!h || !hurt.Add(h)) continue;
                float k = 1f - Vector3.Distance(at, c.ClosestPoint(at)) / BlastRadius;
                if (k <= 0f) continue;
                if (h.Kind == TargetKind.Player) h.TakeDamage(MaxDamage * k, DamageType.Explosive, at);
                else h.TakeAllyDamage(MaxDamage * k, DamageType.Explosive, at); // không phải công người chơi
            }
            foreach (var c in ComradeFollower.All.ToArray())
            {
                if (!c || c.IsDead) continue;
                float k = 1f - Vector3.Distance(at, c.transform.position) / BlastRadius;
                if (k > 0f) c.TakeHit(MaxDamage * k * 1.2f);
            }
        }

        static float Ground(Vector3 p) =>
            Physics.Raycast(p + Vector3.up * 200f, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : p.y;

        // ───────── Máy bay F8F Bearcat dựng bằng khối ─────────
        GameObject BuildPlane()
        {
            if (!body)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                Material M(Color c) { var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", 0.35f); return m; }
                body = M(new Color(0.27f, 0.32f, 0.38f)); dark = M(new Color(0.1f, 0.1f, 0.1f));
                white = M(Color.white); blue = M(new Color(0.1f, 0.2f, 0.55f)); red = M(new Color(0.7f, 0.1f, 0.1f));
            }
            var root = new GameObject("MayBayPhap");
            Part(root, PrimitiveType.Capsule, new Vector3(0, 0, 0), new Vector3(90, 0, 0), new Vector3(1.3f, 4.2f, 1.3f), body);         // thân
            Part(root, PrimitiveType.Cube, new Vector3(0, -0.2f, 0.6f), Vector3.zero, new Vector3(11f, 0.18f, 2.1f), body);             // cánh chính
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.15f, -3.6f), Vector3.zero, new Vector3(4.2f, 0.12f, 1.2f), body);            // cánh đuôi ngang
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.9f, -3.7f), Vector3.zero, new Vector3(0.14f, 1.7f, 1.4f), body);             // đuôi đứng
            Part(root, PrimitiveType.Sphere, new Vector3(0, 0.6f, 0.2f), Vector3.zero, new Vector3(0.75f, 0.6f, 1.4f), dark);           // buồng lái
            Part(root, PrimitiveType.Cylinder, new Vector3(0, 0, 4.15f), new Vector3(90, 0, 0), new Vector3(1.25f, 0.25f, 1.25f), dark); // nắp động cơ
            propeller = Part(root, PrimitiveType.Cube, new Vector3(0, 0, 4.45f), Vector3.zero, new Vector3(3.6f, 0.22f, 0.06f), dark).transform;
            for (int s = -1; s <= 1; s += 2) // phù hiệu tròn ba màu dưới cánh (nhìn từ mặt đất)
            {
                Part(root, PrimitiveType.Cylinder, new Vector3(s * 3.8f, -0.3f, 0.6f), Vector3.zero, new Vector3(1.5f, 0.01f, 1.5f), red);
                Part(root, PrimitiveType.Cylinder, new Vector3(s * 3.8f, -0.31f, 0.6f), Vector3.zero, new Vector3(1.05f, 0.01f, 1.05f), white);
                Part(root, PrimitiveType.Cylinder, new Vector3(s * 3.8f, -0.32f, 0.6f), Vector3.zero, new Vector3(0.55f, 0.01f, 0.55f), blue);
            }
            return root;
        }

        static GameObject Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
