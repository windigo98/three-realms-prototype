
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Third-person camera that follows the Frame and frames the sticky target.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform Subject;
        public StickyTargeting Targeting;
        public FrameInput Input;
        public float Distance = 8f;
        public float Height = 2.4f;
        public float MinPitch = -25f;
        public float MaxPitch = 55f;
        public float Yaw;
        public float Pitch = 12f;
        public float FollowLag = 8f;
        public float TargetBias = 0.28f;

        Vector3 _pos;

        public Transform Pivot { get; private set; }

        void Awake()
        {
            var pivotGo = new GameObject("CameraPivot");
            Pivot = pivotGo.transform;
            Pivot.SetParent(null, true);
        }

        void LateUpdate()
        {
            if (Subject == null) return;
            if (Input != null)
            {
                Yaw += Input.Look.x * 0.12f;
                Pitch = Mathf.Clamp(Pitch - Input.Look.y * 0.12f, MinPitch, MaxPitch);
            }

            Vector3 focus = Subject.position + Vector3.up * 1.4f;
            if (Targeting != null && Targeting.Target != null)
            {
                var t = Targeting.Target.position + Vector3.up * 1.2f;
                focus = Vector3.Lerp(focus, (focus + t) * 0.5f, TargetBias);
            }

            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 desired = focus + rot * new Vector3(0f, Height * 0.15f, -Distance);
            if (Physics.SphereCast(focus, 0.25f, (desired - focus).normalized, out var hit, Distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance > 0.6f)
                    desired = focus + (desired - focus).normalized * (hit.distance - 0.3f);
            }

            _pos = Vector3.Lerp(_pos, desired, 1f - Mathf.Exp(-FollowLag * Time.deltaTime));
            transform.position = _pos;
            transform.LookAt(focus, Vector3.up);
            if (Pivot != null)
            {
                Pivot.position = Subject.position;
                Pivot.rotation = Quaternion.Euler(0f, Yaw, 0f);
            }
        }
    }
}
