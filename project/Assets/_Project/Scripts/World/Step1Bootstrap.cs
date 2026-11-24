
using UnityEngine;
using UnityEngine.Rendering;
#if true
using UnityEngine.Rendering.Universal;
#endif

namespace ThreeRealms
{
    /// <summary>Builds the step-1 greybox scene at runtime if the scene is empty of a Frame.</summary>
    public class Step1Bootstrap : MonoBehaviour
    {
        public static void Ensure()
        {
            if (FindAnyObjectByType<GuardianMotor>() != null) return;
            var go = new GameObject("Step1Bootstrap");
            go.AddComponent<Step1Bootstrap>().Build();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoEnsure() => Ensure();

        void Awake() => Build();

        public void Build()
        {
            if (FindAnyObjectByType<FrameHardpoints>() != null) return;

            var root = new GameObject("Step1Root").transform;

            var quality = new GameObject("Quality").AddComponent<QualityTierController>();
            quality.transform.SetParent(root, false);

            GreyboxYard.Build(root);

            var sun = new GameObject("Sun");
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 110f;

            var frame = GuardianFactory.Build(new Vector3(0f, 0.6f, -6f), root);
            var dummy = BuildDummy(new Vector3(0f, 0f, 6f), root);

            var camGo = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.gameObject
                : new GameObject("Main Camera", typeof(UnityEngine.Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = 180f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.52f, 0.58f, 0.66f);
            cam.allowHDR = false;
            var follow = camGo.GetComponent<FollowCamera>() ?? camGo.AddComponent<FollowCamera>();
            follow.Subject = frame.transform;
            follow.Targeting = frame.GetComponent<StickyTargeting>();
            follow.Input = frame.GetComponent<FrameInput>();
            follow.Yaw = 0f;
            var add = camGo.GetComponent<UniversalAdditionalCameraData>();
            if (add == null) add = camGo.AddComponent<UniversalAdditionalCameraData>();
            add.renderPostProcessing = false;
            add.renderShadows = true;

            var motor = frame.GetComponent<GuardianMotor>();
            motor.CameraPivot = follow.Pivot;
            var dodge = frame.GetComponent<DodgeController>();
            dodge.CameraPivot = follow.Pivot;

            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(root, false);
            var hud = hudGo.AddComponent<GameHud>();
            hud.Vitals = frame.GetComponent<FrameVitals>();
            hud.Input = frame.GetComponent<FrameInput>();
            hud.Hardpoints = frame.GetComponent<FrameHardpoints>();
            hud.Quality = quality;
            hud.CameraRig = follow;

            var dir = hudGo.AddComponent<Step1Director>();
            dir.Motor = motor;
            dir.Dodge = dodge;
            dir.Combo = frame.GetComponent<ComboAttack>();
            dir.Targeting = frame.GetComponent<StickyTargeting>();
            dir.Input = frame.GetComponent<FrameInput>();
            dir.Hud = hud;

            _ = dummy;
        }

        static GameObject BuildDummy(Vector3 pos, Transform parent)
        {
            var root = new GameObject("TrainingDummy");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            var mat = DummyMat(new Color(0.56f, 0.57f, 0.6f));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "DummyBody";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            body.transform.localScale = new Vector3(1.1f, 1.15f, 1.1f);
            body.GetComponent<Renderer>().sharedMaterial = mat;
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "DummyHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().sharedMaterial = mat;

            var dummy = root.AddComponent<TrainingDummy>();
            dummy.BodyRenderer = body.GetComponent<Renderer>();
            dummy.MaxHealth = 80f;
            dummy.Health = 80f;

            // Combined collider on the root for targeting range.
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.2f, 0f);
            box.size = new Vector3(1.2f, 2.5f, 1.2f);
            return root;
        }

        static Material DummyMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
