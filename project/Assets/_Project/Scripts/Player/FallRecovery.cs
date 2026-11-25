
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThreeRealms
{
    /// <summary>
    /// How a fall off the map is resolved.
    /// Step 1 uses ReturnToLastSafe only. Lethal is a hook for Chief later — this
    /// component must not apply fall damage or death on its own.
    /// </summary>
    public enum FallRecoveryMode
    {
        ReturnToLastSafe = 0,
        Lethal = 1
    }

    public struct FallRecoveryArgs
    {
        public Vector3 From;
        public Vector3 To;
        public FallRecoveryMode Mode;
        /// <summary>Always false in step 1. Reserved for a later lethal-fall rule.</summary>
        public bool Lethal;
    }

    /// <summary>
    /// Tracks a last-safe grounded point and returns the Frame there if it falls
    /// below the kill plane or leaves the yard. No damage, no death.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class FallRecovery : MonoBehaviour
    {
        public GuardianMotor Motor;
        public DodgeController Dodge;
        public ComboAttack Combo;
        public FollowCamera CameraRig;
        public FallRecoveryMode Mode = FallRecoveryMode.ReturnToLastSafe;

        public float StableSeconds = 0.3f;
        public float KillPlaneY = -10f;
        public float Bound = 42f;
        public float EdgeInset = 1.15f;
        public float RecoverLock = 0.2f;
        public float FlashDuration = 0.45f;

        public Vector3 LastSafePosition { get; private set; }
        public Quaternion LastSafeRotation { get; private set; }
        public float Flash01 { get; private set; }
        public bool IsLocked => Time.time < _lockUntil;
        public event System.Action<FallRecoveryArgs> Recovered;

        float _stable;
        float _lockUntil;
        float _flashUntil;
        bool _hasSafe;

        void Awake()
        {
            if (Motor == null) Motor = GetComponent<GuardianMotor>();
            if (Dodge == null) Dodge = GetComponent<DodgeController>();
            if (Combo == null) Combo = GetComponent<ComboAttack>();
            Remember(transform.position, transform.rotation, force: true);
        }

        void Update()
        {
            if (IsTestDropPressed())
                DropForTest();

            if (IsLocked)
            {
                TickFlash();
                return;
            }

            TickSafePoint();

            if (NeedsRecovery())
                Recover();

            TickFlash();
        }

        void TickFlash()
        {
            if (Time.time >= _flashUntil)
            {
                Flash01 = 0f;
                return;
            }
            float left = _flashUntil - Time.time;
            Flash01 = Mathf.Clamp01(left / Mathf.Max(0.05f, FlashDuration));
        }

        void TickSafePoint()
        {
            if (!IsSafeToRecord())
            {
                _stable = 0f;
                return;
            }

            _stable += Time.deltaTime;
            if (_stable >= StableSeconds)
                Remember(InsetFromEdges(transform.position), transform.rotation, force: false);
        }

        bool IsSafeToRecord()
        {
            if (Motor == null || !Motor.IsGrounded) return false;
            if (Dodge != null && Dodge.IsDodging) return false;
            if (Motor.IsJumping) return false;
            var p = transform.position;
            if (p.y < KillPlaneY + 1f) return false;
            if (Mathf.Abs(p.x) > Bound - 1.5f || Mathf.Abs(p.z) > Bound - 1.5f) return false;
            if (!HasWalkableGround(p, 0.15f)) return false;
            return true;
        }

        bool NeedsRecovery()
        {
            var p = transform.position;
            if (p.y <= KillPlaneY) return true;
            // Mid-dodge can leave IsGrounded stale, so any out-of-bounds trip is recovered.
            return Mathf.Abs(p.x) > Bound || Mathf.Abs(p.z) > Bound;
        }

        void Recover()
        {
            var from = transform.position;
            if (!_hasSafe)
                Remember(Vector3.up * 0.6f, Quaternion.identity, force: true);
            var to = LastSafePosition;

            if (Dodge != null) Dodge.Cancel();
            if (Combo != null) Combo.Cancel();
            if (Motor != null) Motor.Teleport(to);
            else transform.SetPositionAndRotation(to, LastSafeRotation);
            transform.rotation = LastSafeRotation;

            if (CameraRig != null) CameraRig.SnapToSubject();

            _lockUntil = Time.time + RecoverLock;
            _flashUntil = Time.time + FlashDuration;
            Flash01 = 1f;
            _stable = 0f;

            var args = new FallRecoveryArgs
            {
                From = from,
                To = to,
                Mode = Mode,
                Lethal = false // step 1: never lethal. Hook Mode/Recovered for Chief later.
            };
            Recovered?.Invoke(args);
            Debug.Log($"[Step1] fall recover from={from.x:0.00},{from.y:0.00},{from.z:0.00} to={to.x:0.00},{to.y:0.00},{to.z:0.00}");
        }

        void Remember(Vector3 pos, Quaternion rot, bool force)
        {
            LastSafePosition = pos;
            LastSafeRotation = rot;
            _hasSafe = true;
            if (force) _stable = StableSeconds;
        }

        Vector3 InsetFromEdges(Vector3 p)
        {
            const float reach = 1.2f;
            const float down = 2.2f;
            var pull = Vector3.zero;
            Vector3[] dirs =
            {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized, new Vector3(-1f, 0f, -1f).normalized
            };
            var origin = p + Vector3.up * 0.5f;
            foreach (var d in dirs)
            {
                if (!HasWalkableGround(p + d * reach, 0.5f, down))
                    pull -= d;
            }
            pull.y = 0f;
            if (pull.sqrMagnitude > 0.01f)
                p += pull.normalized * EdgeInset;

            p.x = Mathf.Clamp(p.x, -Bound + 3f, Bound - 3f);
            p.z = Mathf.Clamp(p.z, -Bound + 3f, Bound - 3f);
            return p;
        }

        static bool HasWalkableGround(Vector3 pos, float startUp, float down = 1.6f)
        {
            var origin = pos + Vector3.up * startUp;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, down, ~0, QueryTriggerInteraction.Ignore))
                return false;
            return hit.normal.y >= 0.45f;
        }

        static bool HasWalkableGround(Vector3 pos, float startUp) => HasWalkableGround(pos, startUp, 1.6f);

        // Greybox test hook used by the headless probe. Not a player-facing control.
        static bool IsTestDropPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.f9Key.wasPressedThisFrame;
        }

        public void DropForTest()
        {
            var drop = new Vector3(transform.position.x, KillPlaneY - 4f, transform.position.z + 8f);
            if (Mathf.Abs(drop.x) < Bound) drop.x = Bound + 6f;
            Debug.Log($"[Step1] fall test drop to={drop.x:0.00},{drop.y:0.00},{drop.z:0.00}");
            if (Dodge != null) Dodge.Cancel();
            if (Motor != null) Motor.Teleport(drop);
            else transform.position = drop;
        }
    }
}
