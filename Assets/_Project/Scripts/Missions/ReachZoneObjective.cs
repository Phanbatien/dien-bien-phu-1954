using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Tới một khu vực: vượt rào, điểm tập kết, điểm rút, qua cầu, vào hầm chỉ huy.
    public class ReachZoneObjective : Objective
    {
        public Collider zone;
        protected override bool Check() => zone && Mission.PlayerInside(zone);
    }
}
