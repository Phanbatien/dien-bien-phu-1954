using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DBP.Weapons
{
    /// Một khẩu súng trong StreamingAssets/content/weapons.json (GDD §8).
    [Serializable]
    public class WeaponDef
    {
        public string id;
        public string name;
        public string origin;
        [Tooltip("Loại đạn: chỉ súng cùng ammoType mới dùng chung đạn (BR-36).")]
        public string ammoType;
        public int availableFrom;
        public string rarity;
        [Tooltip("single = bắn phát một/khóa nòng, auto = liên thanh")]
        public string fireMode = "single";
        public WeaponStats stats = new WeaponStats();
        public string sourceId;

        public bool Automatic => fireMode == "auto";
    }

    [Serializable]
    public class WeaponStats
    {
        public float damage = 50f;
        public float rpm = 40f;
        public int mag = 5;
        public float reloadSec = 3f;
        public int reserve = 30;
        public float range = 150f;
        public float hipSpread = 2f;
        public float adsSpread = 0.2f;
        public float recoilPitch = 2.5f;
        public float recoilYaw = 0.6f;
        public float adsFov = 50f;
        [Tooltip("> 1 là súng có ống ngắm: FOV = adsFov / scopeZoom.")]
        public float scopeZoom = 1f;
    }

    /// Đọc weapons.json một lần. Thiếu file thì báo lỗi rõ ràng (BR-27).
    public static class WeaponCatalog
    {
        [Serializable]
        class WeaponFile { public WeaponDef[] weapons; }

        static Dictionary<string, WeaponDef> byId;

        public static string FilePath => Path.Combine(Application.streamingAssetsPath, "content", "weapons.json");

        public static WeaponDef Get(string id)
        {
            Ensure();
            if (id != null && byId.TryGetValue(id, out var def)) return def;
            Debug.LogError($"[WeaponCatalog] Không có vũ khí '{id}' trong {FilePath}");
            return null;
        }

        public static IEnumerable<WeaponDef> All
        {
            get { Ensure(); return byId.Values; }
        }

        public static void LoadJson(string json)
        {
            byId = new Dictionary<string, WeaponDef>();
            var file = JsonUtility.FromJson<WeaponFile>(json);
            if (file?.weapons == null) return;
            foreach (var w in file.weapons) byId[w.id] = w;
        }

        static void Ensure()
        {
            if (byId != null) return;
            if (!File.Exists(FilePath))
            {
                Debug.LogError($"[WeaponCatalog] Thiếu file {FilePath}");
                byId = new Dictionary<string, WeaponDef>();
                return;
            }
            LoadJson(File.ReadAllText(FilePath));
        }
    }
}
