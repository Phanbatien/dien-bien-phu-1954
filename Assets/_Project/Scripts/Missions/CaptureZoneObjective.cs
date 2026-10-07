using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Chiếm một đoạn hào (M4; BR-32): người chơi còn sống trong vùng và không còn địch giữ đoạn đó.
    /// Đặt nhiều cái liên tiếp trong MissionController (sequential) để bắt buộc chiếm theo thứ tự.
    public class CaptureZoneObjective : Objective
    {
        public Collider zone;
        public List<Health> holders = new List<Health>();
        public override string Progress => $"Địch còn {holders.Count - CountNeutralized(holders)}";
        protected override bool Check() => zone && AllNeutralized(holders) && Mission.PlayerInside(zone) && !Mission.PlayerDead;
    }
}
