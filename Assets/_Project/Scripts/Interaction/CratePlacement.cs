using System.Collections.Generic;
using DBP.Core;
using UnityEngine;

namespace DBP.Interaction
{
    /// Tìm chỗ đặt hòm hợp lệ quanh một điểm khai báo: nằm trên mặt đất thật, đủ phẳng,
    /// không dính vật cản (vách hào, bao cát, cây, lô cốt, hòm khác). Dò vòng quanh tối đa 3 m.
    public static class CratePlacement
    {
        /// Nửa kích thước hòm (0,9 × 0,6 × 0,6 m).
        public static readonly Vector3 HalfExtents = new Vector3(0.45f, 0.3f, 0.3f);
        const float Step = 0.75f, MaxSearch = 3f, MinFlatness = 0.8f, Padding = 1.15f;

        /// ground: điểm trên mặt đất để đặt đáy hòm. false = không có chỗ trống quanh điểm này.
        public static bool TryFindSpot(Vector3 point, Quaternion rotation, out Vector3 ground)
        {
            Physics.SyncTransforms(); // hòm vừa sinh trong cùng khung hình cũng phải tính là vật cản
            foreach (var candidate in Candidates(point))
            {
                if (FindGround(candidate, out ground, out var groundCollider) && IsClear(ref ground, rotation, groundCollider))
                    return true;
            }
            ground = point;
            return false;
        }

        static IEnumerable<Vector3> Candidates(Vector3 p)
        {
            yield return p;
            for (float r = Step; r <= MaxSearch + 0.01f; r += Step)
                for (int a = 0; a < 8; a++)
                {
                    float ang = a * 45f * Mathf.Deg2Rad;
                    yield return p + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                }
        }

        /// Dò mặt đất từ ngay trên điểm khai báo; nếu điểm bị chôn dưới địa hình thì dò lại từ trên cao.
        static bool FindGround(Vector3 p, out Vector3 point, out Collider collider)
        {
            foreach (float from in new[] { 3f, 40f })
            {
                if (Physics.Raycast(p + Vector3.up * from, Vector3.down, out var hit, from + 20f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.normal.y >= MinFlatness
                    && !hit.collider.GetComponentInParent<Health>()      // không đặt lên nóc lô cốt, xe tăng, lính
                    && !hit.collider.GetComponentInParent<QuizCrate>()  // không chồng lên hòm khác
                    && !IsUnderTerrain(hit.point))                       // không lấy nền cũ bị chôn dưới địa hình
                {
                    point = hit.point;
                    collider = hit.collider;
                    return true;
                }
            }
            point = p;
            collider = null;
            return false;
        }

        /// Terrain chỉ va chạm từ phía trên, nên tia dò bắt đầu dưới mặt địa hình sẽ xuyên qua và chạm nền cũ bên dưới.
        static bool IsUnderTerrain(Vector3 point)
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                var origin = terrain.transform.position;
                var size = terrain.terrainData.size;
                if (point.x < origin.x || point.z < origin.z || point.x > origin.x + size.x || point.z > origin.z + size.z) continue;
                if (terrain.SampleHeight(point) + origin.y > point.y + 0.2f) return true;
            }
            return false;
        }

        const float MaxStep = 0.12f;

        /// Cả 4 góc đáy hòm phải chạm đất ở gần cùng độ cao: loại chỗ dốc, mép hố, chân vách hào
        /// (vách hào trong Màn 1 là chính Terrain nên không thể chỉ kiểm va chạm).
        static bool IsFlatFootprint(Vector3 ground, Quaternion rotation, out float topY)
        {
            topY = ground.y;
            var hx = HalfExtents.x * Padding;
            var hz = HalfExtents.z * Padding;
            foreach (var local in new[] { new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, -hz) })
            {
                var corner = ground + rotation * local;
                if (!Physics.Raycast(corner + Vector3.up * 1.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                    return false; // mép vực / mép hào
                if (Mathf.Abs(hit.point.y - ground.y) > MaxStep) return false;
                topY = Mathf.Max(topY, hit.point.y);
            }
            return true;
        }

        static bool IsClear(ref Vector3 ground, Quaternion rotation, Collider groundCollider)
        {
            if (!IsFlatFootprint(ground, rotation, out var topY)) return false;
            ground.y = topY; // đặt theo điểm cao nhất dưới đáy hòm để hòm không lún vào mặt đất gồ ghề
            var center = ground + Vector3.up * (HalfExtents.y + 0.05f);
            foreach (var c in Physics.OverlapBox(center, HalfExtents * Padding, rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c == groundCollider) continue;
                if (c.bounds.max.y <= ground.y + 0.05f) continue; // mặt đất liền kề, không phải vật cản
                return false;
            }
            return true;
        }
    }
}
