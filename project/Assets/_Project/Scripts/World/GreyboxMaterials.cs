using UnityEngine;

namespace ThreeRealms
{
    /// <summary>
    /// Runtime materials for the greybox. Prefers a Resources material so URP Lit
    /// is not stripped from WebGL (Shader.Find alone is not a shader reference).
    /// </summary>
    public static class GreyboxMaterials
    {
        static Shader _shader;
        static Material _template;

        public static Material Make(Color color)
        {
            if (_template == null)
                _template = Resources.Load<Material>("GreyboxLit");
            if (_shader == null)
            {
                if (_template != null) _shader = _template.shader;
                if (_shader == null)
                    _shader = Shader.Find("Universal Render Pipeline/Lit");
                if (_shader == null)
                    _shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (_shader == null)
                    _shader = Shader.Find("Sprites/Default");
            }
            if (_shader == null && _template == null)
            {
                Debug.LogError("GreyboxMaterials: no URP Lit shader or Resources/GreyboxLit material in this build.");
                return null;
            }

            var mat = _template != null ? new Material(_template) : new Material(_shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.08f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.28f);
            return mat;
        }
    }
}
