using UnityEngine;

namespace ThreeRealms
{
    /// <summary>A placeholder part that snaps onto one hardpoint.</summary>
    public class FramePart : MonoBehaviour
    {
        public HardpointId Slot;
        public float Mass = 1f;
        public Renderer[] Renderers;

        public void SetHighlight(bool on)
        {
            if (Renderers == null) Renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in Renderers)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        var c = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", on ? Color.Lerp(c, Color.white, 0.35f) : c);
                    }
                }
            }
        }
    }
}
