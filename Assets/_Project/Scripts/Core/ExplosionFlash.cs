using UnityEngine;

namespace DBP.Core
{
    // ponytail: quả cầu lửa phồng rồi tắt cho pháo/bộc phá. Long (T67) thay bằng hạt khói + âm thanh.
    public class ExplosionFlash : MonoBehaviour
    {
        float radius, age;
        const float Life = 0.6f;
        Material mat;

        public static void Spawn(Vector3 position, float radius = 4f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ExplosionFlash";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            var flash = go.AddComponent<ExplosionFlash>();
            flash.radius = radius;
            flash.mat = go.GetComponent<Renderer>().material;
            flash.mat.color = new Color(1f, 0.55f, 0.1f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = age / Life;
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, radius * 2f, k);
            mat.color = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(0.25f, 0.22f, 0.2f), k);
            if (age >= Life) Destroy(gameObject);
        }
    }
}
