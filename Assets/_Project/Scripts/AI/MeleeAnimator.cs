using System.Collections;
using UnityEngine;

namespace DBP.AI
{
    /// Động tác giáp lá cà cho model lính (không có clip hoạt ảnh riêng): đâm lưỡi lê = cúi người lao tới,
    /// lưỡi dao lóe ra trước ngực; đấm báng súng = xoay vai hất ngang. Gắn tự động khi cần.
    public class MeleeAnimator : MonoBehaviour
    {
        Transform model, blade;
        Coroutine running;

        public static void Play(Component soldier, bool stab)
        {
            if (!soldier) return;
            EnemyAnimatorDriver.Play(soldier, stab ? "Stab" : "Punch");
            if (!soldier.TryGetComponent<MeleeAnimator>(out var anim)) anim = soldier.gameObject.AddComponent<MeleeAnimator>();
            anim.Run(stab);
        }

        void Run(bool stab)
        {
            if (!model) model = transform.Find("Model");
            if (!model) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(stab ? Stab() : Punch());
        }

        Transform Blade()
        {
            if (blade) return blade;
            // Súng đã có lưỡi lê thật (LuoiLe_Model) thì không dựng lưỡi khối
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "LuoiLe_Model") return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "LuoiLe";
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.025f, 0.05f, 0.35f);
            var r = go.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", new Color(0.75f, 0.77f, 0.8f));
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return blade = go.transform;
        }

        IEnumerator Stab()
        {
            var b = Blade();
            var rest = model.localRotation; var restPos = model.localPosition;
            if (b) b.gameObject.SetActive(true);
            for (float t = 0; t < 0.32f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.32f) * Mathf.PI); // vào rồi ra
                model.localRotation = rest * Quaternion.Euler(18f * k, 0, 0);
                model.localPosition = restPos + Vector3.forward * 0.35f * k;
                if (b) b.localPosition = new Vector3(0.15f, 1.25f, 0.45f + 0.55f * k);
                yield return null;
            }
            model.localRotation = rest; model.localPosition = restPos;
            if (b) b.gameObject.SetActive(false);
            running = null;
        }

        IEnumerator Punch()
        {
            var rest = model.localRotation;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.3f) * Mathf.PI);
                model.localRotation = rest * Quaternion.Euler(8f * k, -35f * k, 0);
                yield return null;
            }
            model.localRotation = rest;
            running = null;
        }
    }
}
