
using UnityEngine;

namespace ThreeRealms
{
    [RequireComponent(typeof(CharacterController))]
    public class GuardianMotor : MonoBehaviour
    {
        public FrameInput Input;
        public Transform CameraPivot;
        public float MoveSpeed = 5.5f;
        public float TurnSpeed = 12f;
        public float Gravity = -24f;
        /// <summary>Peak height of a grounded jump, in meters. Covers cover blocks and the ramp; the tall ledge is reached from the ramp.</summary>
        public float JumpHeight = 1.75f;
        public float GroundGrace = 0.1f;

        CharacterController _cc;
        float _vert;
        float _groundedUntil;
        bool _inJump;
        float _apexY;

        public Vector3 PlanarVelocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsJumping => _inJump;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (Input == null) Input = GetComponent<FrameInput>();
        }

        public void Move(float speedMultiplier)
        {
            if (Input == null) return;
            Vector3 camFwd = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (CameraPivot != null)
            {
                camFwd = CameraPivot.forward;
                camRight = CameraPivot.right;
                camFwd.y = 0f;
                camRight.y = 0f;
                camFwd.Normalize();
                camRight.Normalize();
            }

            Vector3 wish = camRight * Input.Move.x + camFwd * Input.Move.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            PlanarVelocity = wish * MoveSpeed * speedMultiplier;

            if (wish.sqrMagnitude > 0.01f)
            {
                var look = Quaternion.LookRotation(wish, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, TurnSpeed * Time.deltaTime);
            }

            IsGrounded = _cc != null && _cc.isGrounded;
            if (IsGrounded) _groundedUntil = Time.time + GroundGrace;

            if (IsGrounded && _vert <= 0f)
            {
                if (_inJump)
                {
                    Debug.Log($"[Step1] jump land y={transform.position.y:0.00} apex={_apexY:0.00}");
                    _inJump = false;
                }
                _vert = -2f;
            }

            // Grounded jump only; _inJump blocks a second hop until we land.
            if (Input.JumpPressed && !_inJump && Time.time <= _groundedUntil)
            {
                float g = Mathf.Abs(Gravity) < 0.01f ? 24f : Mathf.Abs(Gravity);
                _vert = Mathf.Sqrt(2f * g * Mathf.Max(0.1f, JumpHeight));
                _inJump = true;
                _apexY = transform.position.y;
                _groundedUntil = 0f;
                Debug.Log($"[Step1] jump start y={transform.position.y:0.00} speed={_vert:0.00}");
            }

            _vert += Gravity * Time.deltaTime;

            var motion = PlanarVelocity * Time.deltaTime;
            motion.y = _vert * Time.deltaTime;
            if (_cc != null) _cc.Move(motion);
            else transform.position += motion;

            if (_inJump && transform.position.y > _apexY)
                _apexY = transform.position.y;
        }
    }
}
