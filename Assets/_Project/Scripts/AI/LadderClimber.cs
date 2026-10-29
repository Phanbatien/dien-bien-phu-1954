using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace DBP.AI
{
    /// Lính AI (cả đồng đội lẫn địch) đi qua liên kết NavMesh của thang (NavMeshLink đặt ở mỗi thang):
    /// lên thì trèo thẳng đứng rồi bước vào bục, xuống thì bước ra mép rồi tụt xuống — thay vì trượt chéo.
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class LadderClimber : MonoBehaviour
    {
        const float ClimbSpeed = 2.2f, StepSpeed = 3f;
        NavMeshAgent agent;
        bool climbing;
        public bool IsClimbing => climbing;

        public static void Ensure(Component c) { if (!c.TryGetComponent<LadderClimber>(out _)) c.gameObject.AddComponent<LadderClimber>(); }

        void Awake() { agent = GetComponent<NavMeshAgent>(); agent.autoTraverseOffMeshLink = false; }

        void Update()
        {
            if (!climbing && agent.enabled && agent.isOnOffMeshLink) StartCoroutine(Climb(agent.currentOffMeshLinkData));
        }

        IEnumerator Climb(OffMeshLinkData link)
        {
            climbing = true;
            var start = transform.position;
            var end = link.endPos + Vector3.up * agent.baseOffset;
            // Lên: trèo đứng tới cao độ đích rồi bước vào; xuống: bước ra mép rồi tụt xuống
            var corner = end.y > start.y ? new Vector3(start.x, end.y, start.z) : new Vector3(end.x, start.y, end.z);
            float cornerSpeed = end.y > start.y ? ClimbSpeed : StepSpeed, endSpeed = end.y > start.y ? StepSpeed : ClimbSpeed;
            var look = end - start; look.y = 0;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
            yield return MoveTo(corner, cornerSpeed);
            yield return MoveTo(end, endSpeed);
            if (agent.enabled && agent.isOnOffMeshLink) agent.CompleteOffMeshLink();
            climbing = false;
        }

        IEnumerator MoveTo(Vector3 to, float speed)
        {
            while (agent.enabled && (transform.position - to).sqrMagnitude > 0.0025f)
            {
                transform.position = Vector3.MoveTowards(transform.position, to, speed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
