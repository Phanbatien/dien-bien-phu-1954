using DBP.Core;
using UnityEngine;

namespace DBP.AI
{
    /// Mìn do chỉ huy địch đặt: đĩa sắt nằm sát đất, người chơi hoặc đồng đội bước vào 1,3 m thì nổ.
    public sealed class Landmine : MonoBehaviour
    {
        const float TriggerRadius = 1.3f, BlastRadius = 4f, PlayerDamage = 40f, AllyDamage = 80f;
        static Material metal, plate;
        Transform player;

        public static Landmine Place(Vector3 at)
        {
            if (!metal) { metal = Lit(new Color(0.22f, 0.24f, 0.18f)); plate = Lit(new Color(0.45f, 0.42f, 0.3f)); }
            var root = new GameObject("Min");
            root.transform.position = at;
            Part(root.transform, new Vector3(0.42f, 0.05f, 0.42f), 0.04f, metal);
            Part(root.transform, new Vector3(0.14f, 0.03f, 0.14f), 0.1f, plate);
            return root.AddComponent<Landmine>();
        }

        static void Part(Transform parent, Vector3 scale, float y, Material m)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(parent, false);
            c.transform.localPosition = Vector3.up * y;
            c.transform.localScale = scale;
            c.GetComponent<Renderer>().sharedMaterial = m;
        }

        static Material Lit(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            return m;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        void Update()
        {
            if (player && Flat(player.position) < TriggerRadius) { Explode(); return; }
            foreach (var c in ComradeFollower.All)
                if (c && !c.IsDead && Flat(c.transform.position) < TriggerRadius) { Explode(); return; }
        }

        float Flat(Vector3 p) { var d = p - transform.position; d.y = 0; return d.magnitude; }

        void Explode()
        {
            var at = transform.position;
            CombatEffects.Explosion(at, 1.3f);
            CampaignAudio.Effect("explosion_02", at);
            if (player && Flat(player.position) < BlastRadius)
                player.GetComponent<IDamageable>()?.TakeDamage(PlayerDamage * (1f - Flat(player.position) / BlastRadius * 0.6f), DamageType.Explosive, at);
            foreach (var c in ComradeFollower.All.ToArray())
                if (c && !c.IsDead && Flat(c.transform.position) < BlastRadius) c.TakeHit(AllyDamage);
            Destroy(gameObject);
        }
    }
}
