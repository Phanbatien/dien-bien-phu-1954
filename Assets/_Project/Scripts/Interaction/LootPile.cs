using System.Collections.Generic;
using DBP.Weapons;
using UnityEngine;

namespace DBP.Interaction
{
    /// Đồ để lại tại điểm nhận: súng bị thay ra, đạn/lựu đạn dư (BR-36).
    /// Đã thuộc sở hữu trong lượt nên nhặt lại bằng E, không qua câu hỏi, không sinh thêm đồ.
    public class LootPile : MonoBehaviour, IInteractable
    {
        Weapon weapon;
        readonly Dictionary<string, int> ammo = new Dictionary<string, int>();
        int grenades;

        public string Prompt => weapon != null ? $"E: nhặt lại {weapon.Def.name}" : "E: nhặt đồ để lại";
        public float HoldSeconds => 0f;
        public bool IsEmpty => weapon == null && grenades <= 0 && ammo.Count == 0;

        public bool CanInteract(Interactor who) => !IsEmpty && who.Weapons != null;

        /// Lấy đống đồ đang có tại vị trí, hoặc tạo mới (khối nhỏ màu nâu, Khôi thay model sau).
        public static LootPile At(Transform anchor)
        {
            var existing = anchor.GetComponentInChildren<LootPile>();
            if (existing) return existing;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "LootPile";
            // Con của điểm nhận nhưng giữ kích thước thật, kể cả khi điểm nhận bị co giãn (súng rơi dẹt).
            go.transform.SetParent(anchor, true);
            go.transform.position = anchor.position + anchor.forward * 0.9f + Vector3.up * 0.15f;
            var s = anchor.lossyScale;
            go.transform.localScale = new Vector3(0.6f / s.x, 0.3f / s.y, 0.4f / s.z);
            go.GetComponent<Renderer>().material.color = new Color(0.4f, 0.3f, 0.2f);
            return go.AddComponent<LootPile>();
        }

        public void Add(Weapon droppedWeapon, string ammoType, int ammoCount, int grenadeCount)
        {
            if (droppedWeapon != null) weapon = droppedWeapon;
            if (ammoCount > 0 && !string.IsNullOrEmpty(ammoType))
                ammo[ammoType] = (ammo.TryGetValue(ammoType, out var n) ? n : 0) + ammoCount;
            grenades += Mathf.Max(0, grenadeCount);
            RemoveIfEmpty();
        }

        public void Interact(Interactor who)
        {
            var inventory = who.Weapons.Inventory;
            if (weapon != null)
            {
                var taken = weapon;
                weapon = null;
                var result = inventory.TakeBack(taken);
                weapon = result.Dropped; // đổi chỗ: khẩu đang cầm nằm lại đây
                if (result.LeftoverAmmo > 0) AddAmmo(taken.Def.ammoType, result.LeftoverAmmo);
            }

            foreach (var type in new List<string>(ammo.Keys))
            {
                int left = inventory.AddAmmo(type, ammo[type]);
                if (left > 0) ammo[type] = left; else ammo.Remove(type);
            }
            grenades = inventory.AddGrenades(grenades);
            RemoveIfEmpty();
        }

        void AddAmmo(string type, int count) => ammo[type] = (ammo.TryGetValue(type, out var n) ? n : 0) + count;

        void RemoveIfEmpty()
        {
            if (IsEmpty) Destroy(gameObject);
        }
    }
}
