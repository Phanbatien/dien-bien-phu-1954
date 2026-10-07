using System.Collections.Generic;
using DBP.Core;
using DBP.Player;
using DBP.Weapons;
using UnityEngine;
using UnityEngine.Events;

namespace DBP.Missions
{
    /// Một mục tiêu bắt buộc của màn. MissionController kích hoạt và kiểm tra theo thứ tự.
    /// onCompleted dùng để nối sự kiện kịch bản (vd. nổ A1 ở Màn 5 – T59).
    public abstract class Objective : MonoBehaviour
    {
        [SerializeField] string title = "Mục tiêu";
        public UnityEvent onCompleted = new UnityEvent();

        public string Title { get => title; set => title = value; }
        public bool Active { get; private set; }
        public bool Done { get; private set; }
        protected MissionController Mission { get; private set; }

        public virtual void Activate(MissionController mission)
        {
            Mission = mission;
            Active = true;
        }

        /// Trả true khi đã xong. Chỉ chuyển sang xong một lần.
        public bool Evaluate()
        {
            if (Done) return true;
            if (!Active || !Check()) return false;
            Done = true;
            onCompleted?.Invoke();
            return true;
        }

        public virtual string Progress => "";
        protected abstract bool Check();

        protected static bool AllNeutralized(List<Health> targets)
        {
            foreach (var t in targets)
                if (t != null && !t.Neutralized) return false;
            return true;
        }

        protected static int CountNeutralized(List<Health> targets)
        {
            int n = 0;
            foreach (var t in targets) if (t == null || t.Neutralized) n++;
            return n;
        }
    }
}
