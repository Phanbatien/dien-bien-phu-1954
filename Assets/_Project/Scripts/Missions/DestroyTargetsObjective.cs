using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Phá/hạ toàn bộ mục tiêu: lô cốt (M1), lính gác + tổ súng máy (M3), ổ đề kháng (M5).
    public class DestroyTargetsObjective : Objective
    {
        public List<Health> targets = new List<Health>();
        public override string Progress => $"{CountNeutralized(targets)}/{targets.Count}";
        protected override bool Check() => AllNeutralized(targets);
    }
}
