using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Đoạn hướng dẫn Màn 1: đi, bắn, thay đạn.
    public class TutorialObjective : Objective
    {
        bool moved, fired, reloaded;

        public override string Progress =>
            $"{(moved ? "✔" : "·")} WASD đi  {(fired ? "✔" : "·")} Chuột trái bắn  {(reloaded ? "✔" : "·")} R thay đạn";

        void OnEnable()
        {
            FirstPersonController.Moved += OnMoved;
            WeaponController.Fired += OnFired;
            WeaponController.Reloaded += OnReloaded;
        }

        void OnDisable()
        {
            FirstPersonController.Moved -= OnMoved;
            WeaponController.Fired -= OnFired;
            WeaponController.Reloaded -= OnReloaded;
        }

        void OnMoved() { if (Active) moved = true; }
        void OnFired() { if (Active) fired = true; }
        void OnReloaded() { if (Active) reloaded = true; }

        protected override bool Check() => moved && fired && reloaded;
    }
}
