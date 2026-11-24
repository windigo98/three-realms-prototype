
using UnityEngine;

namespace ThreeRealms
{
    /// <summary>Rampart Highlands yard near Stillwater Hold: greybox only.</summary>
    public static class GreyboxYard
    {
        public static void Build(Transform root)
        {
            var groundMat = MakeMat(new Color(0.42f, 0.43f, 0.45f));
            var wallMat = MakeMat(new Color(0.50f, 0.51f, 0.54f));
            var accentMat = MakeMat(new Color(0.55f, 0.56f, 0.52f));
            var coverMat = MakeMat(new Color(0.38f, 0.39f, 0.41f));

            // Floor ~80 x 80. Walk speed 5.5, so a crossing is ~15s; long enough to feel like a yard.
            var floor = Quad(root, "YardFloor", new Vector3(0, -0.5f, 0), new Vector3(80f, 1f, 80f), groundMat);
            var col = floor.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            // Low perimeter
            Wall(root, "WallN", new Vector3(0, 1.2f, 40.5f), new Vector3(82f, 2.4f, 1.2f), wallMat);
            Wall(root, "WallS", new Vector3(0, 1.2f, -40.5f), new Vector3(82f, 2.4f, 1.2f), wallMat);
            Wall(root, "WallE", new Vector3(40.5f, 1.2f, 0), new Vector3(1.2f, 2.4f, 82f), wallMat);
            Wall(root, "WallW", new Vector3(-40.5f, 1.2f, 0), new Vector3(1.2f, 2.4f, 82f), wallMat);

            // Ramp up to a ledge on the north-east side.
            var ramp = Quad(root, "Ramp", new Vector3(14f, 1.1f, 16f), new Vector3(8f, 0.4f, 14f), accentMat);
            ramp.transform.rotation = Quaternion.Euler(-14f, -25f, 0f);
            var rampCol = ramp.AddComponent<BoxCollider>();
            rampCol.size = Vector3.one;

            // Ledge / platform
            Wall(root, "Ledge", new Vector3(22f, 1.6f, 24f), new Vector3(16f, 3.2f, 10f), wallMat);
            // A thin top so you can stand on it
            Wall(root, "LedgeTop", new Vector3(22f, 3.25f, 24f), new Vector3(16.2f, 0.2f, 10.2f), accentMat);

            // Cover blocks
            Wall(root, "CoverA", new Vector3(-8f, 1.1f, 4f), new Vector3(3.2f, 2.2f, 1.4f), coverMat);
            Wall(root, "CoverB", new Vector3(6f, 0.9f, -6f), new Vector3(1.6f, 1.8f, 4.4f), coverMat);
            Wall(root, "CoverC", new Vector3(-18f, 1.3f, -14f), new Vector3(5f, 2.6f, 2f), coverMat);
            Wall(root, "CoverD", new Vector3(10f, 1.0f, 8f), new Vector3(2.2f, 2.0f, 2.2f), coverMat);

            // A short pillar row for silhouette
            Wall(root, "PillarL", new Vector3(-28f, 2.5f, 20f), new Vector3(2f, 5f, 2f), wallMat);
            Wall(root, "PillarR", new Vector3(-22f, 2.0f, 20f), new Vector3(2f, 4f, 2f), wallMat);
        }

        static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.05f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            return m;
        }

        static GameObject Quad(Transform root, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Apply(go, mat);
            return go;
        }

        static void Wall(Transform root, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            Quad(root, name, pos, scale, mat);
        }

        static void Apply(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
    }
}
