using DBP.Core;
using DBP.Missions;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Interaction
{
    /// Địch bị hạ có thể rơi súng (T23; BR-14). Súng rơi phải qua câu hỏi, cấp theo độ hiếm của chính vũ khí.
    [RequireComponent(typeof(Health))]
    public class WeaponDrop : MonoBehaviour
    {
        public string weaponId = "W-MAT49";
        public int ammo = 32;

        void Awake() => GetComponent<Health>().Died += OnDied;

        void OnDied()
        {
            var mission = MissionController.Current;
            if (!mission || !mission.TryRollDrop()) return;
            var def = WeaponCatalog.Get(weaponId);
            if (def == null) return;
            var reward = new CrateReward { rarity = RarityRules.Parse(def.rarity), weaponId = weaponId, ammoType = def.ammoType, ammo = ammo };
            // Tìm mặt đất dưới xác, bỏ qua collider của chính lính này.
            var ground = new Vector3(transform.position.x, 0f, transform.position.z);
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(transform.position + Vector3.up, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && hit.distance < best) { best = hit.distance; ground = hit.point; }
            QuizCrate.Spawn(ground + transform.right * 0.6f, Quaternion.Euler(0, Random.Range(0f, 360f), 0), reward, false, null);
        }
    }
}
