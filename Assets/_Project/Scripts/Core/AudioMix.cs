namespace DBP.Core
{
    /// Relative layer gains; the profile's AudioListener volume remains the master.
    public static class AudioMix
    {
        public static float Gain(string clip)
        {
            if (string.IsNullOrEmpty(clip)) return .55f;
            if (clip.StartsWith("mission_", System.StringComparison.Ordinal)) return .22f;
            if (clip == "airplane_loop") return .12f;
            if (clip == "tank_engine_loop") return .28f;
            if (clip.StartsWith("step_", System.StringComparison.Ordinal)) return .25f;
            if (clip == "ui_click" || clip == "ui_saved") return .4f;
            if (clip.StartsWith("ui_", System.StringComparison.Ordinal)) return .55f;
            if (clip.StartsWith("mechanic_", System.StringComparison.Ordinal)) return .35f;
            if (IsWeapon(clip)) return .62f;
            return .55f;
        }
        static bool IsWeapon(string clip) => clip.StartsWith("mosin_shot_",System.StringComparison.Ordinal) || clip == "mat49_A" || clip == "type50_B";
        public static bool IsCombat(string clip) => !string.IsNullOrEmpty(clip) &&
            (IsWeapon(clip) || clip.StartsWith("explosion_",System.StringComparison.Ordinal) || clip == "cannon_generic");
    }
}
