using UnityEngine;
namespace DBP.AI
{
    /// Keeps the rifle's grip at the hand across retargeted two-handed poses.
    public class AlliedRifleGrip : MonoBehaviour
    {
        public Transform rifle, rightHand, leftHand;
        public Vector3 localGrip;
        ComradeFollower comrade;

        void Awake() => comrade = GetComponentInParent<ComradeFollower>();
        void LateUpdate() => Align();

        public void Align()
        {
            if (!rifle || !rightHand || !leftHand || Application.isPlaying && comrade && comrade.IsDead) return;
            var forward = leftHand.position - rightHand.position;
            if (forward.sqrMagnitude < .0001f) return;
            rifle.rotation = Quaternion.LookRotation(forward, transform.up);
            rifle.position += rightHand.position - rifle.TransformPoint(localGrip);
        }
    }
}
