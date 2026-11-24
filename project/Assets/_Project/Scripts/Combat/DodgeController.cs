
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Short dash (0.5–0.8s) that costs 5 Pulse, grants i-frames, and can cancel a hit.</summary>
    public class DodgeController : MonoBehaviour
    {
        public FrameInput Input;
        public FrameVitals Vitals;
        public CharacterController Controller;
        public Transform CameraPivot;
        public float Duration = 0.65f;
        public float PulseCost = 5f;
        public float Distance = 6.5f;
        public float Cooldown = 0.15f;

        public bool IsDodging { get; private set; }
        public bool Invulnerable => IsDodging;
        public event System.Action Dodged;

        float _endTime;
        float _readyTime;
        Vector3 _dir;

        void Awake()
        {
            if (Input == null) Input = GetComponent<FrameInput>();
            if (Vitals == null) Vitals = GetComponent<FrameVitals>();
            if (Controller == null) Controller = GetComponent<CharacterController>();
        }

        public bool TryDodge()
        {
            if (IsDodging) return false;
            if (Time.time < _readyTime) return false;
            if (Vitals == null || !Vitals.TrySpendPulse(PulseCost)) return false;

            _dir = FlattenDir();
            IsDodging = true;
            _endTime = Time.time + Duration;
            _readyTime = _endTime + Cooldown;
            Dodged?.Invoke();
            return true;
        }

        public void Tick()
        {
            if (!IsDodging) return;
            float remaining = _endTime - Time.time;
            if (remaining <= 0f)
            {
                IsDodging = false;
                return;
            }
            // Ease out so the dash feels snappy, not a long slide.
            float t = 1f - Mathf.Clamp01(remaining / Duration);
            float speed = (Distance / Duration) * (1.35f - 0.7f * t);
            var delta = _dir * speed * Time.deltaTime;
            if (Controller != null) Controller.Move(delta);
            else transform.position += delta;
        }

        Vector3 FlattenDir()
        {
            Vector3 camFwd = CameraPivot != null ? CameraPivot.forward : transform.forward;
            Vector3 camRight = CameraPivot != null ? CameraPivot.right : transform.right;
            camFwd.y = 0f; camRight.y = 0f;
            camFwd.Normalize(); camRight.Normalize();
            Vector3 wish = Vector3.zero;
            if (Input != null && Input.Move.sqrMagnitude > 0.05f)
                wish = camRight * Input.Move.x + camFwd * Input.Move.y;
            if (wish.sqrMagnitude < 0.01f) wish = transform.forward;
            wish.y = 0f;
            return wish.normalized;
        }
    }
}
