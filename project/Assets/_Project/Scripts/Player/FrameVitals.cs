
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Health, Pulse, and Charge. In step 1 only Pulse is spent (dodge).</summary>
    public class FrameVitals : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float MaxPulse = 100f;
        public float MaxCharge = 100f;
        public float Health = 100f;
        public float Pulse = 100f;
        public float Charge = 0f;

        public float PulseRegenPerSecond = 6f;
        public float PulseRegenDelay = 1f;
        public float ChargeDrainPerSecond = 4f;
        public float OutOfCombatDelay = 3f;

        float _lastPulseSpend;
        float _lastCombat;

        public float Health01 => MaxHealth <= 0 ? 0 : Mathf.Clamp01(Health / MaxHealth);
        public float Pulse01 => MaxPulse <= 0 ? 0 : Mathf.Clamp01(Pulse / MaxPulse);
        public float Charge01 => MaxCharge <= 0 ? 0 : Mathf.Clamp01(Charge / MaxCharge);

        public bool TrySpendPulse(float cost)
        {
            if (cost <= 0f) return true;
            if (Pulse < cost) return false;
            Pulse -= cost;
            _lastPulseSpend = Time.time;
            return true;
        }

        public void AddCharge(float amount)
        {
            Charge = Mathf.Clamp(Charge + amount, 0f, MaxCharge);
            _lastCombat = Time.time;
        }

        public void NotifyCombat() => _lastCombat = Time.time;

        void Update()
        {
            if (Time.time - _lastPulseSpend >= PulseRegenDelay)
                Pulse = Mathf.Min(MaxPulse, Pulse + PulseRegenPerSecond * Time.deltaTime);

            if (Time.time - _lastCombat >= OutOfCombatDelay && Charge > 0f)
                Charge = Mathf.Max(0f, Charge - ChargeDrainPerSecond * Time.deltaTime);
        }
    }
}
