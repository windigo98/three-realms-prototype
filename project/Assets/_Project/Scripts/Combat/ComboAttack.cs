
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
        int _nextHit;

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

        void Fire(int hit)
        {
            IsAttacking = true;
            PendingHit = hit;
            if (Targeting != null) Targeting.FaceTarget(Body, FaceSpeed * 4f);

            float dmg = (Damage != null && hit < Damage.Length) ? Damage[hit] : 10f;
            float stop = (HitStop != null && hit < HitStop.Length) ? HitStop[hit] : 0.05f;
            float rec = (Recover != null && hit < Recover.Length) ? Recover[hit] : 0.3f;

            var dummy = FindTargetDummy();
            if (dummy != null && Targeting.DistanceToTarget() <= Range)
            {
                dummy.ApplyHit(dmg, Body != null ? Body.position : transform.position, hit);
                if (Vitals != null)
                {
                    Vitals.AddCharge(ChargePerHit);
                    Vitals.NotifyCombat();
                }
                if (stop > 0f)
                    _hitStopUntil = Time.unscaledTime + stop;
            }

            _busyUntil = Time.time + rec;
            _windowUntil = Time.time + rec + ChainWindow;
            _nextHit = hit + 1;
            if (_nextHit > 2) _nextHit = 0;
        }

        TrainingDummy FindTargetDummy()
        {
            if (Targeting == null || Targeting.Target == null) return null;
            return Targeting.Target.GetComponent<TrainingDummy>()
                   ?? Targeting.Target.GetComponentInParent<TrainingDummy>();
        }
    }
}
