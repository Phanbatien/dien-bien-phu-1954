using System.Collections.Generic;
using UnityEngine;

namespace DBP.Player
{
    /// Vùng trèo thang: người chơi đứng trong vùng này thì W/S trèo lên/xuống (FirstPersonController).
    /// Vùng là BoxCollider trigger bao thân thang; lên tới đỉnh thì được đẩy nhẹ ra phía trước để bước lên bục.
    [RequireComponent(typeof(BoxCollider))]
    public class Ladder : MonoBehaviour
    {
        public static readonly List<Ladder> All = new List<Ladder>();
        BoxCollider area;

        void Awake() { area = GetComponent<BoxCollider>(); area.isTrigger = true; }
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// Hướng từ chân thang vào bục (mặt sau của thang).
        public Vector3 Into => transform.forward;
        public float TopY => area.bounds.max.y;

        public bool Contains(Vector3 feet) =>
            area.bounds.Contains(feet + Vector3.up * 0.3f) || area.bounds.Contains(feet + Vector3.up * 1.2f);

        public static Ladder At(Vector3 feet)
        {
            foreach (var l in All) if (l && l.Contains(feet)) return l;
            return null;
        }
    }
}
