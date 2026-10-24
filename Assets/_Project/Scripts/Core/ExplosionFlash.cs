using UnityEngine;

namespace DBP.Core
{
    // Compatibility entry point for the field-phone strike.
    public class ExplosionFlash : MonoBehaviour
    {
        public static void Spawn(Vector3 position, float radius = 4f) => CombatEffects.Explosion(position, radius / 3f);
    }
}
