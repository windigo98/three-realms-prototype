
using UnityEngine;

namespace ThreeRealms
{
    public static class GuardianFactory
    {
        public static GameObject Build(Vector3 pos, Transform parent)
        {
            var root = new GameObject("GuardianFrame");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.tag = "Player";
            var layer = LayerMask.NameToLayer("Default");

            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1.15f, 0f);
            cc.height = 2.2f;
            cc.radius = 0.45f;
            cc.slopeLimit = 40f;
            cc.stepOffset = 0.35f;

            var bodyMat = Mat(new Color(0.62f, 0.64f, 0.68f));
            var headMat = Mat(new Color(0.70f, 0.72f, 0.76f));

            var torso = Prim(root.transform, PrimitiveType.Capsule, "Torso",
                new Vector3(0f, 1.15f, 0f), new Vector3(0.9f, 0.7f, 0.7f), bodyMat);
            DisableCollider(torso);
            var head = Prim(root.transform, PrimitiveType.Sphere, "Head",
                new Vector3(0f, 2.05f, 0.05f), Vector3.one * 0.38f, headMat);
            DisableCollider(head);

            var hp = root.AddComponent<FrameHardpoints>();
            hp.Slots = new FrameHardpoints.Slot[8];
            // Local offsets on a bare Frame.
            var anchors = new (HardpointId id, Vector3 p, Vector3 euler)[]
            {
                (HardpointId.CO, new Vector3(0f, 1.35f, 0.22f), Vector3.zero),
                (HardpointId.CH, new Vector3(0f, 0.85f, 0f), Vector3.zero),
                (HardpointId.LA, new Vector3(-0.55f, 1.45f, 0f), new Vector3(0f, 0f, 8f)),
                (HardpointId.RA, new Vector3(0.55f, 1.45f, 0f), new Vector3(0f, 0f, -8f)),
                (HardpointId.LL, new Vector3(-0.22f, 0.55f, 0f), Vector3.zero),
                (HardpointId.RL, new Vector3(0.22f, 0.55f, 0f), Vector3.zero),
                (HardpointId.WM, new Vector3(0.55f, 1.15f, 0.25f), new Vector3(0f, 0f, -15f)),
                (HardpointId.SM, new Vector3(0f, 1.55f, -0.28f), Vector3.zero)
            };
            var partColors = new Color[]
            {
                new Color(0.75f, 0.78f, 0.85f), // CO pale white-blue
                new Color(0.55f, 0.58f, 0.65f), // CH
                new Color(0.70f, 0.74f, 0.82f), // LA
                new Color(0.70f, 0.74f, 0.82f), // RA
                new Color(0.50f, 0.52f, 0.58f), // LL
                new Color(0.50f, 0.52f, 0.58f), // RL
                new Color(0.82f, 0.72f, 0.38f), // WM gold-ish
                new Color(0.45f, 0.55f, 0.75f)  // SM blue
            };
            var partScale = new Vector3[]
            {
                new Vector3(0.46f, 0.36f, 0.22f),
                new Vector3(0.7f, 0.28f, 0.5f),
                new Vector3(0.22f, 0.7f, 0.22f),
                new Vector3(0.22f, 0.7f, 0.22f),
                new Vector3(0.24f, 0.7f, 0.24f),
                new Vector3(0.24f, 0.7f, 0.24f),
                new Vector3(0.16f, 0.16f, 0.7f),
                new Vector3(0.36f, 0.18f, 0.22f)
            };

            for (int i = 0; i < 8; i++)
            {
                var a = anchors[i];
                var anchor = new GameObject("HP_" + HardpointNames.Code(a.id));
                anchor.transform.SetParent(root.transform, false);
                anchor.transform.localPosition = a.p;
                anchor.transform.localRotation = Quaternion.Euler(a.euler);
                hp.Slots[i] = new FrameHardpoints.Slot { Id = a.id, Anchor = anchor.transform };

                var typ = (a.id == HardpointId.LA || a.id == HardpointId.RA || a.id == HardpointId.LL || a.id == HardpointId.RL)
                    ? PrimitiveType.Capsule : PrimitiveType.Cube;
                if (a.id == HardpointId.CO) typ = PrimitiveType.Cube;
                var partGo = Prim(anchor.transform, typ, "Part_" + HardpointNames.Code(a.id),
                    Vector3.zero, partScale[i], Mat(partColors[i]));
                DisableCollider(partGo);
                var part = partGo.AddComponent<FramePart>();
                part.Slot = a.id;
                part.Mass = a.id == HardpointId.CH ? 2f : 1f;
                part.Renderers = partGo.GetComponentsInChildren<Renderer>();
                hp.Slots[i].Mounted = part;
            }
            hp.RebuildMap();

            root.AddComponent<FrameInput>();
            root.AddComponent<FrameVitals>();
            var motor = root.AddComponent<GuardianMotor>();
            var dodge = root.AddComponent<DodgeController>();
            var targeting = root.AddComponent<StickyTargeting>();
            var combo = root.AddComponent<ComboAttack>();
            combo.Targeting = targeting;
            combo.Dodge = dodge;
            combo.Body = root.transform;
            motor.Input = root.GetComponent<FrameInput>();
            dodge.Input = root.GetComponent<FrameInput>();
            dodge.Vitals = root.GetComponent<FrameVitals>();
            dodge.Controller = cc;

            _ = layer;
            return root;
        }

        static GameObject Prim(Transform parent, PrimitiveType typ, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(typ);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            return go;
        }

        static void DisableCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
        }

        static Material Mat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.15f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.4f);
            return m;
        }
    }
}
