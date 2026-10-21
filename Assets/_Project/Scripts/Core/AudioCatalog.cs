using UnityEngine;

namespace DBP.Core
{
    /// Approved audio choices, separate from weapon balance and gameplay randomness.
    public static class AudioCatalog
    {
        public static AudioClip Clip(string name) => Resources.Load<AudioClip>("Audio/" + name);

        public static AudioClip Weapon(string id, int variation = 0)
        {
            switch (id)
            {
                case "W-MAT49": return Clip("mat49_A");
                case "W-TYPE50": return Clip("type50_B");
                case "W-MOSIN":
                case "W-MOSIN-SCOPE": return Clip($"mosin_shot_{(variation & int.MaxValue) % 5 + 1:00}");
                default: return null;
            }
        }

        public static AudioClip Music(string scene)
        {
            switch (scene)
            {
                case "M1_HimLam": return Clip("mission_01");
                case "M2_GiuDocLap": return Clip("mission_02");
                case "M3_DoiED": return Clip("mission_03");
                case "M4_LongHao": return Clip("mission_04");
                case "M5_ChieuMungBay": return Clip("mission_05");
                default: return null;
            }
        }

        public static AudioClip Step(string surface, int variation)
        {
            surface = (surface ?? "").ToLowerInvariant();
            if (surface.Contains("wood") || surface.Contains("plank")) return Clip($"step_wood{(variation & int.MaxValue) % 3 + 1:00}");
            if (surface.Contains("stone") || surface.Contains("concrete")) return Clip("step_stone01");
            if (surface.Contains("gravel")) return Clip("step_gravel");
            if (surface.Contains("grass") || surface.Contains("leaves")) return Clip($"step_leaves{(variation & int.MaxValue) % 2 + 1:00}");
            return Clip("step_mud02");
        }
    }
}
