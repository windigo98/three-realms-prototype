
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

        CharacterController _cc;
        float _vert;

        public Vector3 PlanarVelocity { get; private set; }
        public bool IsGrounded { get; private set; }

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

            IsGrounded = _cc.isGrounded;
            if (IsGrounded && _vert < 0f) _vert = -2f;
            _vert += Gravity * Time.deltaTime;

            var motion = PlanarVelocity * Time.deltaTime;
            motion.y = _vert * Time.deltaTime;
            _cc.Move(motion);
        }
    }
}
