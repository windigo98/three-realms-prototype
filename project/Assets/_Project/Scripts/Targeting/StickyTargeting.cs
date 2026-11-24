
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Soft lock that stays on the current target until it leaves range or you cycle.</summary>
    public class StickyTargeting : MonoBehaviour
    {
        public float AcquireRange = 14f;
        public float LoseRange = 20f;
        public Transform Target { get; private set; }

        public void Tick(bool cycle)
        {
            if (cycle) Cycle();
            else if (Target == null) AcquireNearest();
            else if (HorizontalDistance(Target) > LoseRange) Target = AcquireNearest();
        }

        public void FaceTarget(Transform body, float turnSpeed)
        {
            if (Target == null || body == null) return;
            var to = Target.position - body.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            var look = Quaternion.LookRotation(to, Vector3.up);
            body.rotation = Quaternion.Slerp(body.rotation, look, turnSpeed * Time.deltaTime);
        }

        public float DistanceToTarget()
        {
            if (Target == null) return float.PositiveInfinity;
            return HorizontalDistance(Target);
        }

        Transform AcquireNearest()
        {
            TrainingDummy best = null;
            float bestD = AcquireRange;
            foreach (var d in TrainingDummy.All)
            {
                if (d == null) continue;
                float dist = HorizontalDistance(d.transform);
                if (dist < bestD) { bestD = dist; best = d; }
            }
            Target = best != null ? best.transform : null;
            return Target;
        }

        void Cycle()
        {
            var all = TrainingDummy.All;
            if (all.Count == 0) { Target = null; return; }
            int start = 0;
            if (Target != null)
            {
                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null && all[i].transform == Target) { start = i + 1; break; }
            }
            for (int n = 0; n < all.Count; n++)
            {
                var d = all[(start + n) % all.Count];
                if (d == null) continue;
                if (HorizontalDistance(d.transform) <= LoseRange)
                {
                    Target = d.transform;
                    return;
                }
            }
            Target = AcquireNearest();
        }

        float HorizontalDistance(Transform t)
        {
            var a = transform.position; a.y = 0f;
            var b = t.position; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
