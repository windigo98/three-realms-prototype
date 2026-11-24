
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINES_UNIVERSAL || true
using UnityEngine.Rendering.Universal;
#endif

namespace ThreeRealms
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2 }

    /// <summary>Low/Medium/High URP-style quality tiers. Same art, different cost.</summary>
    public class QualityTierController : MonoBehaviour
    {
        public QualityTier Current = QualityTier.Medium;
        public static QualityTierController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                Current = QualityTier.Medium;
            else
                Current = QualityTier.High;
            Apply(Current);
        }

        public void SetTier(QualityTier tier)
        {
            Current = tier;
            Apply(tier);
        }

        public void Cycle()
        {
            SetTier((QualityTier)(((int)Current + 1) % 3));
        }

        public static void Apply(QualityTier tier)
        {
            int fps = tier == QualityTier.High ? 60 : 30;
            Application.targetFrameRate = fps;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = tier == QualityTier.Low ? UnityEngine.ShadowQuality.Disable
                : (tier == QualityTier.Medium ? UnityEngine.ShadowQuality.HardOnly : UnityEngine.ShadowQuality.All);
            QualitySettings.shadowDistance = tier == QualityTier.Low ? 18f : (tier == QualityTier.Medium ? 35f : 60f);
            QualitySettings.shadowResolution = tier == QualityTier.High ? UnityEngine.ShadowResolution.High : UnityEngine.ShadowResolution.Medium;
            QualitySettings.antiAliasing = tier == QualityTier.High ? 2 : 0;
            QualitySettings.lodBias = tier == QualityTier.Low ? 0.7f : (tier == QualityTier.Medium ? 0.9f : 1.1f);
            QualitySettings.globalTextureMipmapLimit = tier == QualityTier.Low ? 1 : 0;
            QualitySettings.particleRaycastBudget = tier == QualityTier.Low ? 16 : 64;
            QualitySettings.skinWeights = SkinWeights.TwoBones;

            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp is UniversalRenderPipelineAsset urp)
            {
                try
                {
                    urp.renderScale = tier == QualityTier.Low ? 0.7f : (tier == QualityTier.Medium ? 0.85f : 1f);
                    urp.msaaSampleCount = 1; // MSAA is flaky on iOS Safari WebGL
                    urp.shadowDistance = QualitySettings.shadowDistance;
                    // HDR off on WebGL: several URP debug/copy shaders are missing there.
                    urp.supportsHDR = false;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("QualityTier: could not tweak URP asset: " + e.Message);
                }
            }
        }
    }
}
