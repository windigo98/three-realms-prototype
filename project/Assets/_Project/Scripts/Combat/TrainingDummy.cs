
using System.Collections.Generic;
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Self-healing training dummy. Flashes on hit and pops a damage number.</summary>
    public class TrainingDummy : MonoBehaviour
    {
        public static readonly List<TrainingDummy> All = new List<TrainingDummy>();

        public float MaxHealth = 80f;
        public float Health = 80f;
        public float RefillDelay = 2.2f;
        public float RefillPerSecond = 80f;
        public Renderer BodyRenderer;
        public Color FlashColor = new Color(1f, 0.85f, 0.35f, 1f);

        float _lastHit;
        Color _baseColor = new Color(0.55f, 0.55f, 0.58f, 1f);
        Material _mat;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            if (BodyRenderer == null) BodyRenderer = GetComponentInChildren<Renderer>();
            if (BodyRenderer != null)
            {
                _mat = BodyRenderer.material;
                if (_mat.HasProperty(BaseColorId)) _baseColor = _mat.GetColor(BaseColorId);
                else if (_mat.HasProperty(ColorId)) _baseColor = _mat.GetColor(ColorId);
            }
        }

        void OnDisable() => All.Remove(this);

        public void ApplyHit(float damage, Vector3 from, int comboIndex)
        {
            Health = Mathf.Max(0f, Health - damage);
            _lastHit = Time.time;
            Flash();
            DamagePopup.Spawn(transform.position + Vector3.up * 2.2f, damage, comboIndex);
        }

        void Update()
        {
            if (Time.time - _lastHit > 0.12f) SetColor(_baseColor);
            if (Health < MaxHealth && Time.time - _lastHit >= RefillDelay)
                Health = Mathf.Min(MaxHealth, Health + RefillPerSecond * Time.deltaTime);
        }

        void Flash() => SetColor(FlashColor);

        void SetColor(Color c)
        {
            if (_mat == null) return;
            if (_mat.HasProperty(BaseColorId)) _mat.SetColor(BaseColorId, c);
            if (_mat.HasProperty(ColorId)) _mat.SetColor(ColorId, c);
        }
    }
}
