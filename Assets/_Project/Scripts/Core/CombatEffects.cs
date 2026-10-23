using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DBP.Core
{
    /// Scene-owned, bounded presentation. Particle seeds never use gameplay Random.
    public sealed class CombatEffects : MonoBehaviour
    {
        public static CombatEffects Current { get; private set; }
        readonly ParticleSystem[][] pools = new ParticleSystem[5][];
        readonly int[] next = new int[5];
        Material material;
        Texture2D texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            Current = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initial() => Ensure();
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Ensure(); }
        static CombatEffects Ensure()
        {
            if (!Application.isPlaying) return null;
            if (!Current) Current = new GameObject("CombatEffects").AddComponent<CombatEffects>();
            return Current;
        }
        void Awake()
        {
            if (Current && Current != this) { Destroy(gameObject); return; }
            Current = this;
            texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "CombatSoftParticle", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float r = new Vector2((x - 15.5f) / 15.5f, (y - 15.5f) / 15.5f).magnitude;
                pixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - r), 1.5f));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            material = new Material(Resources.Load<Shader>("CombatParticles")) { name = "CombatParticles" };
            material.mainTexture = texture;
            string[] names = { "Muzzle", "Dust", "Sparks", "Blast", "Smoke" };
            int[] sizes = { 12, 8, 8, 4, 4 };
            for (int kind = 0; kind < pools.Length; kind++)
            {
                pools[kind] = new ParticleSystem[sizes[kind]];
                for (int i = 0; i < sizes[kind]; i++)
                {
                    var go = new GameObject(names[kind] + i); go.transform.SetParent(transform, false);
                    var p = go.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    p.useAutoRandomSeed = false; p.randomSeed = (uint)(100 + kind * 16 + i);
                    var main = p.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 24;
                    main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = false;
                    main.duration = 3; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                    var emission = p.emission; emission.enabled = false;
                    var shape = p.shape; shape.enabled = false;
                    var color = p.colorOverLifetime; color.enabled = true;
                    var gradient = new Gradient();
                    gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                        new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.7f, .25f), new GradientAlphaKey(0, 1) });
                    color.color = gradient;
                    var size = p.sizeOverLifetime; size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, kind == 4 ? .5f : 1, 1, kind == 4 ? 2 : .15f));
                    var renderer = p.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    pools[kind][i] = p;
                }
            }
        }
        void Burst(int kind, Vector3 position, Quaternion rotation, int count, float size, float life, float speed, Color tint)
        {
            var p = pools[kind][next[kind]++ % pools[kind].Length];
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.transform.SetPositionAndRotation(position, rotation);
            var main = p.main; main.startLifetime = new ParticleSystem.MinMaxCurve(life * .6f, life);
            main.startSize = new ParticleSystem.MinMaxCurve(size * .65f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .4f, speed);
            main.startColor = tint; main.gravityModifier = kind == 2 ? .3f : 0;
            var shape = p.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = kind == 4 ? 30 : 65; shape.radius = kind == 4 ? .2f * size : .02f;
            p.Play(); p.Emit(count);
        }
        public static void Muzzle(Vector3 position, Quaternion rotation, float scale = 1)
        {
            var fx = Ensure(); if (!fx || !fx.enabled) return;
            fx.Burst(0, position, rotation, 3, .22f * scale, .075f, .5f, new Color(1, .72f, .22f, 1));
        }
        public static void Impact(Vector3 position, Vector3 normal, bool metal = false)
        {
            var fx = Ensure(); if (!fx || !fx.enabled) return;
            var rotation = Quaternion.LookRotation(normal.sqrMagnitude > .001f ? normal : Vector3.up);
            fx.Burst(metal ? 2 : 1, position + normal * .025f, rotation, metal ? 8 : 5,
                metal ? .055f : .25f, metal ? .25f : .55f, metal ? 3 : .6f,
                metal ? new Color(1, .75f, .3f, 1) : new Color(.48f, .4f, .3f, .65f));
        }
        public static bool IsMetal(Collider target)
        {
            if (!target) return false;
            if (target.GetComponentInParent<DBP.AI.ChaffeeTank>()) return true;
            string label = target.sharedMaterial ? target.sharedMaterial.name : target.name;
            return label.IndexOf("metal", System.StringComparison.OrdinalIgnoreCase) >= 0
                || label.IndexOf("steel", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public static void Explosion(Vector3 position, float scale = 1)
        {
            var fx = Ensure(); if (!fx || !fx.enabled) return;
            scale = Mathf.Clamp(scale, .3f, 3);
            fx.Burst(3, position + Vector3.up * .15f, Quaternion.Euler(-90, 0, 0), 12, 1.3f * scale, .35f, 2 * scale, new Color(1, .47f, .12f, .95f));
            fx.Burst(4, position + Vector3.up * .35f, Quaternion.Euler(-90, 0, 0), 10, 1.2f * scale, 2.5f, .65f * scale, new Color(.23f, .21f, .18f, .65f));
        }
        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (material) Destroy(material);
            if (texture) Destroy(texture);
        }
    }
}
