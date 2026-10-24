using UnityEngine;

namespace DBP.Core
{
    /// Ánh sáng pháo xa (T24): loé lên rồi tắt, không đổ bóng nên không kéo FPS.
    [RequireComponent(typeof(Light))]
    public class ArtilleryFlash : MonoBehaviour
    {
        public float peak = 3.5f;
        public float gap = 6f;
        public float fade = 8f;

        Light flash;
        float next;

        void Awake()
        {
            flash = GetComponent<Light>();
            flash.shadows = LightShadows.None;
            next = gap;
        }

        void Update()
        {
            if (!flash) return;
            if (Time.time >= next)
            {
                flash.intensity = peak;
                CampaignAudio.Effect("distant_explosion", transform.position, false, .65f);
                next = Time.time + gap + Random.Range(0f, 3f);
            }
            else
            {
                flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, fade * Time.deltaTime);
            }
        }
    }
}
