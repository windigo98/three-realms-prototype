using System.Collections.Generic;
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>
    /// Snap API for the eight hardpoints. Mounts or unmounts a part,
    /// keeps running Mass, and rejects a part that does not match the slot.
    /// </summary>
    public class FrameHardpoints : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            public HardpointId Id;
            public Transform Anchor;
            public FramePart Mounted;
        }

        public Slot[] Slots = new Slot[8];
        public float Mass;

        readonly Dictionary<HardpointId, Slot> _map = new Dictionary<HardpointId, Slot>();

        void Awake() => RebuildMap();

        public void RebuildMap()
        {
            _map.Clear();
            if (Slots == null) return;
            foreach (var s in Slots)
            {
                if (s == null) continue;
                _map[s.Id] = s;
            }
            RecalcMass();
        }

        public Transform GetAnchor(HardpointId id) =>
            _map.TryGetValue(id, out var s) ? s.Anchor : null;

        public FramePart GetMounted(HardpointId id) =>
            _map.TryGetValue(id, out var s) ? s.Mounted : null;

        public bool IsFitted(HardpointId id) => GetMounted(id) != null;

        public bool AllFitted()
        {
            foreach (var id in HardpointNames.All)
                if (!IsFitted(id)) return false;
            return true;
        }

        public string SnapReport()
        {
            var lines = new System.Text.StringBuilder();
            foreach (var id in HardpointNames.All)
            {
                var ok = IsFitted(id);
                lines.Append(HardpointNames.Code(id));
                lines.Append(" ");
                lines.Append(HardpointNames.Display(id));
                lines.Append(ok ? " SNAP OK" : " EMPTY");
                lines.Append('\n');
            }
            lines.Append("Mass ").Append(Mass.ToString("0.0"));
            return lines.ToString();
        }

        public bool Snap(FramePart part, HardpointId? expected = null)
        {
            if (part == null) return false;
            var id = expected ?? part.Slot;
            if (part.Slot != id) return false;
            if (!_map.TryGetValue(id, out var slot) || slot.Anchor == null) return false;
            if (slot.Mounted != null && slot.Mounted != part) Unsnap(id);
            part.transform.SetParent(slot.Anchor, false);
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.Slot = id;
            slot.Mounted = part;
            RecalcMass();
            return true;
        }

        public FramePart Unsnap(HardpointId id)
        {
            if (!_map.TryGetValue(id, out var slot) || slot.Mounted == null) return null;
            var part = slot.Mounted;
            slot.Mounted = null;
            part.transform.SetParent(null, true);
            RecalcMass();
            return part;
        }

        public void SnapAll(IEnumerable<FramePart> parts)
        {
            foreach (var p in parts) Snap(p);
        }

        public void UnsnapAll()
        {
            foreach (var id in HardpointNames.All) Unsnap(id);
        }

        void RecalcMass()
        {
            float m = 0f;
            if (Slots != null)
            {
                foreach (var s in Slots)
                    if (s != null && s.Mounted != null) m += s.Mounted.Mass;
            }
            Mass = m;
        }
    }
}
