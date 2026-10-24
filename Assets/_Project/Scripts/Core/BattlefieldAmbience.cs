using UnityEngine;
using UnityEngine.Rendering;

namespace DBP.Core
{
    /// Chiến trường luôn sống (T44, T60): pháo nổ ở xa, đạn vạch phòng không trên trời, pháo sáng đêm,
    /// cột khói và đám cháy trên xác xe pháo. Chỉ là hình/tiếng, không gây sát thương, không dùng Random
    /// của gameplay. Chạy theo timeScale nên đứng yên khi trả lời câu hỏi.
    public class BattlefieldAmbience : MonoBehaviour
    {
        [Tooltip("Tâm và bán kính vùng chơi; nổ xa rơi ngoài vùng này.")]
        public Vector3 center;
        public float radius = 60f;
        [Header("Pháo nổ xa")]
        public Vector2 shellGap = new Vector2(3f, 8f);
        [Header("Đạn vạch phòng không / đại liên xa")]
        public Vector2 tracerGap = new Vector2(2f, 5f);
        [Header("Pháo sáng (đêm)")]
        public bool flares;
        public Vector2 flareGap = new Vector2(12f, 20f);
        [Header("Khói & lửa")]
        public Transform[] smokePoints;
        public Transform[] firePoints;

        System.Random rng;
        float nextShell, nextTracer, nextFlare;
        Light flash;
        Transform player;
        // Loạt đạn vạch đang bắn
        Vector3 burstFrom, burstTo;
        int burstLeft;
        float burstNext;
        // Pháo sáng: tối đa 2 quả cùng lúc
        readonly Light[] flareLights = new Light[2];
        readonly LineRenderer[] flareGlows = new LineRenderer[2];
        readonly float[] flareAge = { -1f, -1f };
        Light[] fireLights;

        void Start()
        {
            rng = new System.Random(gameObject.scene.name.GetHashCode());
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            flash = new GameObject("DistantFlash").AddComponent<Light>();
            flash.transform.SetParent(transform, false);
            flash.type = LightType.Point; flash.range = 160f; flash.intensity = 0f; flash.shadows = LightShadows.None;
            flash.color = new Color(1f, .62f, .3f);
            nextShell = Time.time + 1.5f; nextTracer = Time.time + 1f; nextFlare = Time.time + 3f;

            var mat = CombatEffects.SoftMaterial;
            if (smokePoints != null) foreach (var t in smokePoints) if (t) Plume(t, mat, false);
            if (firePoints != null)
            {
                fireLights = new Light[firePoints.Length];
                for (int i = 0; i < firePoints.Length; i++)
                {
                    if (!firePoints[i]) continue;
                    Plume(firePoints[i], mat, true);
                    var l = new GameObject("FireLight").AddComponent<Light>();
                    l.transform.SetParent(firePoints[i], false); l.transform.localPosition = Vector3.up * .8f;
                    l.type = LightType.Point; l.range = 9f; l.color = new Color(1f, .5f, .18f); l.shadows = LightShadows.None;
                    fireLights[i] = l;
                }
            }
            if (flares)
                for (int i = 0; i < flareLights.Length; i++)
                {
                    var go = new GameObject("Flare" + i); go.transform.SetParent(transform, false);
                    var l = go.AddComponent<Light>();
                    l.type = LightType.Point; l.range = 70f; l.color = new Color(1f, .93f, .78f); l.shadows = LightShadows.None; l.enabled = false;
                    var glow = go.AddComponent<LineRenderer>();
                    glow.sharedMaterial = mat; glow.positionCount = 2; glow.widthMultiplier = 1.4f; glow.useWorldSpace = false;
                    glow.SetPosition(0, Vector3.up * -.5f); glow.SetPosition(1, Vector3.up * .5f);
                    glow.startColor = glow.endColor = new Color(1f, .95f, .8f, 1f);
                    glow.shadowCastingMode = ShadowCastingMode.Off; glow.enabled = false;
                    flareLights[i] = l; flareGlows[i] = glow;
                }
        }

        float Range(Vector2 r) => Mathf.Lerp(r.x, r.y, (float)rng.NextDouble());

        /// Điểm ngoài vùng chơi nhưng vẫn trong tầm nhìn của người chơi (sương mù nuốt mất nếu quá xa).
        Vector3 DistantPoint(float min, float max)
        {
            Vector3 origin = player ? player.position : center;
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            var p = origin + dir * Range(new Vector2(min, max));
            var fromCenter = p - center; fromCenter.y = 0;
            if (fromCenter.magnitude < radius) p = center + (fromCenter.sqrMagnitude > .01f ? fromCenter.normalized : dir) * (radius + 10f);
            p.y = GroundHeight(p);
            return p;
        }

        static float GroundHeight(Vector3 p)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain) return terrain.SampleHeight(p) + terrain.transform.position.y;
            return Physics.Raycast(p + Vector3.up * 200f, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : p.y;
        }

        void Update()
        {
            if (Time.deltaTime <= 0f) return;
            float now = Time.time;

            if (now >= nextShell)
            {
                var p = DistantPoint(55f, 110f);
                CombatEffects.Explosion(p, Range(new Vector2(2f, 3f)));
                flash.transform.position = p + Vector3.up * 6f;
                flash.intensity = 6f;
                CampaignAudio.Effect("distant_explosion", p, false, .45f);
                nextShell = now + Range(shellGap);
            }
            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, 14f * Time.deltaTime);

            if (now >= nextTracer && burstLeft == 0)
            {
                burstFrom = DistantPoint(60f, 95f);
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                burstTo = burstFrom + new Vector3(Mathf.Cos(a) * 45f, Range(new Vector2(45f, 90f)), Mathf.Sin(a) * 45f);
                burstLeft = rng.Next(5, 12);
                nextTracer = now + Range(tracerGap);
            }
            if (burstLeft > 0 && now >= burstNext)
            {
                var jitter = new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * 6f;
                CombatEffects.Tracer(burstFrom, burstTo + jitter, rng.NextDouble() < .5);
                burstLeft--; burstNext = now + .09f;
            }

            if (flares) UpdateFlares(now);

            if (fireLights != null)
                for (int i = 0; i < fireLights.Length; i++)
                    if (fireLights[i]) fireLights[i].intensity = 1.6f + Mathf.PerlinNoise(now * 7f, i * 3.1f) * 1.8f;
        }

        void UpdateFlares(float now)
        {
            if (now >= nextFlare)
                for (int i = 0; i < flareLights.Length; i++)
                {
                    if (flareAge[i] >= 0f) continue;
                    var p = DistantPoint(25f, 60f); p.y += Range(new Vector2(55f, 75f));
                    flareLights[i].transform.position = p;
                    flareAge[i] = 0f; flareLights[i].enabled = flareGlows[i].enabled = true;
                    nextFlare = now + Range(flareGap);
                    break;
                }
            for (int i = 0; i < flareLights.Length; i++)
            {
                if (flareAge[i] < 0f) continue;
                flareAge[i] += Time.deltaTime;
                // Rơi chậm dưới dù, lắc nhẹ theo gió, cháy chập chờn rồi tắt
                flareLights[i].transform.position += new Vector3(Mathf.Sin(flareAge[i] * .7f) * .6f, -1.6f, .3f) * Time.deltaTime;
                float life = 1f - flareAge[i] / 26f;
                flareLights[i].intensity = Mathf.Max(0f, life) * (5f + Mathf.PerlinNoise(flareAge[i] * 9f, i) * 2f);
                if (life <= 0f) { flareAge[i] = -1f; flareLights[i].enabled = flareGlows[i].enabled = false; }
            }
        }

        static void Plume(Transform at, Material mat, bool fire)
        {
            var go = new GameObject(fire ? "Fire" : "SmokeColumn");
            go.transform.SetParent(at, false);
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var p = go.AddComponent<ParticleSystem>();
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.useAutoRandomSeed = false; p.randomSeed = (uint)Mathf.Abs(at.position.GetHashCode());
            var main = p.main;
            main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = fire ? new ParticleSystem.MinMaxCurve(.5f, 1.1f) : new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSpeed = fire ? new ParticleSystem.MinMaxCurve(1.2f, 2.4f) : new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSize = fire ? new ParticleSystem.MinMaxCurve(.6f, 1.3f) : new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startColor = fire ? new Color(1f, .5f, .15f, .9f) : new Color(.16f, .15f, .14f, .55f);
            main.maxParticles = fire ? 40 : 60;
            main.gravityModifier = fire ? -.1f : -.02f;
            var emission = p.emission; emission.rateOverTime = fire ? 26f : 4.5f;
            var shape = p.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = fire ? 12f : 8f; shape.radius = fire ? .5f : 1.2f;
            var size = p.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, fire ? 1f : .6f, 1, fire ? .2f : 3.5f));
            var color = p.colorOverLifetime; color.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(fire ? new Color(.4f, .3f, .25f) : Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f), new GradientAlphaKey(0, 1) });
            color.color = g;
            // Gió nhẹ thổi khói nghiêng về một phía
            var velocity = p.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(fire ? 0f : 1.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(fire ? 0f : .4f);
            var r = p.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            // Bắt đầu như đã cháy từ lâu, không phải vừa nhóm lửa
            p.Simulate(fire ? 1f : 12f, true, true, true);
            p.Play();
        }
    }
}
