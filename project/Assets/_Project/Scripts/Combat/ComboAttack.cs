
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Three-hit basic combo with hit-stop. Can be cancelled into a dodge.</summary>
    public class ComboAttack : MonoBehaviour
    {
        public FrameInput Input;
        public FrameVitals Vitals;
        public StickyTargeting Targeting;
        public DodgeController Dodge;
        public Transform Body;
        public float Range = 3.6f;
        public float ChainWindow = 0.55f;
        public float FaceSpeed = 16f;
        public float[] Damage = { 10f, 16f, 24f };
        public float[] HitStop = { 0.04f, 0.06f, 0.09f };
        public float[] Recover = { 0.28f, 0.32f, 0.42f };
        public float ChargePerHit = 4f;

        public int PendingHit { get; private set; }
        public bool IsAttacking { get; private set; }

        float _windowUntil;
        float _busyUntil;
        float _hitStopUntil;
        float _lastRecover = 0.3f;
        int _nextHit;

        Transform _swing;
        Quaternion _swingRest;
        bool _swingResolved;

        void Awake()
        {
            if (Input == null) Input = GetComponent<FrameInput>();
            if (Vitals == null) Vitals = GetComponent<FrameVitals>();
            if (Targeting == null) Targeting = GetComponent<StickyTargeting>();
            if (Dodge == null) Dodge = GetComponent<DodgeController>();
            if (Body == null) Body = transform;
            if (Dodge != null) Dodge.Dodged += Cancel;
        }

        void OnDestroy()
        {
            if (Dodge != null) Dodge.Dodged -= Cancel;
        }

        public void Cancel()
        {
            IsAttacking = false;
            _nextHit = 0;
            _windowUntil = 0f;
            _busyUntil = 0f;
            _hitStopUntil = 0f;
        }

        public bool IsInHitStop => Time.unscaledTime < _hitStopUntil;

        public void Tick()
        {
            if (Dodge != null && Dodge.IsDodging)
            {
                Cancel();
                return;
            }

            if (Time.unscaledTime < _hitStopUntil) return;

            if (IsAttacking && Time.time >= _busyUntil)
                IsAttacking = false;

            if (Time.time > _windowUntil && !IsAttacking)
                _nextHit = 0;

            if (Input == null || !Input.AttackPressed) return;
            if (IsAttacking) return;

            int hit = _nextHit;
            if (hit < 0 || hit > 2) hit = 0;
            Fire(hit);
        }

        void LateUpdate()
        {
            EnsureSwing();
            if (_swing == null) return;
            if (IsAttacking)
            {
                float rec = Mathf.Max(0.05f, _lastRecover);
                float t = Mathf.Clamp01(1f - (_busyUntil - Time.time) / rec);
                float amp = PendingHit >= 2 ? 95f : 70f;
                float ang = Mathf.Sin(t * Mathf.PI) * amp;
                _swing.localRotation = _swingRest * Quaternion.Euler(ang, ang * 0.25f, 0f);
            }
            else
            {
                _swing.localRotation = Quaternion.Slerp(_swing.localRotation, _swingRest, 18f * Time.deltaTime);
            }
        }

        void EnsureSwing()
        {
            if (_swingResolved) return;
            _swingResolved = true;
            _swing = FindSwing("HP_WM") ?? FindSwing("HP_RA") ?? FindSwing("HP_LA");
            if (_swing != null) _swingRest = _swing.localRotation;
        }

        Transform FindSwing(string anchorName)
        {
            var t = transform.Find(anchorName);
            if (t == null) return null;
            return t.childCount > 0 ? t.GetChild(0) : t;
        }

        void Fire(int hit)
        {
            IsAttacking = true;
            PendingHit = hit;
            if (Targeting != null) Targeting.FaceTarget(Body, FaceSpeed * 4f);

            float dmg = (Damage != null && hit < Damage.Length) ? Damage[hit] : 10f;
            float stop = (HitStop != null && hit < HitStop.Length) ? HitStop[hit] : 0.05f;
            float rec = (Recover != null && hit < Recover.Length) ? Recover[hit] : 0.3f;
            _lastRecover = rec;

            var dummy = FindTargetDummy();
            if (dummy != null)
            {
                dummy.ApplyHit(dmg, Body != null ? Body.position : transform.position, hit);
                if (Vitals != null)
                {
                    Vitals.AddCharge(ChargePerHit);
                    Vitals.NotifyCombat();
                }
                if (stop > 0f)
                    _hitStopUntil = Time.unscaledTime + stop;
                Debug.Log($"[Step1] hit dummy dmg={dmg} health={dummy.Health} charge={(Vitals != null ? Vitals.Charge : 0f)} combo={hit}");
            }
            else
            {
                Debug.Log($"[Step1] swing miss combo={hit}");
            }

            _busyUntil = Time.time + rec;
            _windowUntil = Time.time + rec + ChainWindow;
            _nextHit = hit + 1;
            if (_nextHit > 2) _nextHit = 0;
        }

        TrainingDummy FindTargetDummy()
        {
            TrainingDummy best = null;
            float bestD = Range;

            if (Targeting != null && Targeting.Target != null)
            {
                var locked = Targeting.Target.GetComponent<TrainingDummy>()
                             ?? Targeting.Target.GetComponentInParent<TrainingDummy>();
                if (locked != null)
                {
                    float d = Targeting.DistanceToTarget();
                    if (d <= Range) return locked;
                }
            }

            foreach (var dummy in TrainingDummy.All)
            {
                if (dummy == null) continue;
                var a = transform.position; a.y = 0f;
                var b = dummy.transform.position; b.y = 0f;
                float dist = Vector3.Distance(a, b);
                if (dist <= Range && dist < bestD)
                {
                    bestD = dist;
                    best = dummy;
                }
            }

            if (best != null) return best;

            // Collider backup once Capsule/Sphere/Box are kept in the player.
            var origin = (Body != null ? Body.position : transform.position) + Vector3.up * 1.1f + transform.forward * 0.8f;
            var cols = Physics.OverlapSphere(origin, Mathf.Max(1.2f, Range * 0.6f));
            foreach (var col in cols)
            {
                if (col == null) continue;
                var dummy = col.GetComponentInParent<TrainingDummy>();
                if (dummy != null) return dummy;
            }
            return null;
        }
    }
}
