
using System.Collections.Generic;
using UnityEngine;

namespace ThreeRealms
{
    public class DamagePopup : MonoBehaviour
    {
        static readonly List<DamagePopup> Live = new List<DamagePopup>();
        public static IReadOnlyList<DamagePopup> Active => Live;

        public string Text;
        public Color Color;
        public float Born;
        public float Life = 0.7f;
        public Vector3 WorldPos;
        public float Rise = 1.2f;

        public static void Spawn(Vector3 world, float damage, int comboIndex)
        {
            var go = new GameObject("Dmg");
            var p = go.AddComponent<DamagePopup>();
            p.WorldPos = world;
            p.Text = Mathf.RoundToInt(damage).ToString();
            p.Born = Time.unscaledTime;
            p.Color = comboIndex >= 2
                ? new Color(1f, 0.75f, 0.2f)
                : new Color(1f, 0.95f, 0.7f);
            Live.Add(p);
        }

        public float Age => Time.unscaledTime - Born;
        public bool Dead => Age >= Life;

        public Vector3 CurrentWorld => WorldPos + Vector3.up * (Rise * Mathf.Clamp01(Age / Life));

        void OnDestroy() => Live.Remove(this);

        public static void Cull()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                if (Live[i] == null || Live[i].Dead)
                {
                    if (Live[i] != null) Destroy(Live[i].gameObject);
                    else Live.RemoveAt(i);
                }
            }
        }
    }
}
