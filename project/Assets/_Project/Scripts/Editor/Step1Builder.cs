
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ThreeRealms;

namespace ThreeRealms.Editor
{
    /// <summary>Batchmode entry points: set up the greybox scene and build WebGL.</summary>
    public static class Step1Builder
    {
        public const string ScenePath = "Assets/_Project/Scenes/RampartYard.unity";
        public const string SitePath = "/home/box/projects/three-realms-site";

        public static void SetupProject()
        {
            EnsureFolders();
            ApplyPlayerSettings();
            ApplyQualitySettings();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Step1] Project setup complete.");
        }

        public static void SetupAndBuildWebGL()
        {
            SetupProject();
            var code = BuildWebGL();
            EditorApplication.Exit(code);
        }

        public static void BuildWebGLOnly()
        {
            var code = BuildWebGL();
            EditorApplication.Exit(code);
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            Directory.CreateDirectory("Assets/_Project/Settings");
            Directory.CreateDirectory("Assets/_Project/Resources");
            EnsureGreyboxMaterial();
            IncludeShaders(
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit"
            );
        }

        static void EnsureGreyboxMaterial()
        {
            const string path = "Assets/_Project/Resources/GreyboxLit.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
            {
                Debug.LogError("[Step1] Could not find URP Lit to build GreyboxLit.mat");
                return;
            }
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.5f, 0.51f, 0.54f, 1f));
                AssetDatabase.CreateAsset(mat, path);
                Debug.Log("[Step1] Created " + path);
            }
            else if (existing.shader != shader)
            {
                existing.shader = shader;
                EditorUtility.SetDirty(existing);
            }
        }

        static void IncludeShaders(params string[] names)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            var have = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < arr.arraySize; i++)
            {
                var sh = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (sh != null) have.Add(sh.name);
            }
            bool changed = false;
            foreach (var name in names)
            {
                if (have.Contains(name)) continue;
                var sh = Shader.Find(name);
                if (sh == null)
                {
                    Debug.LogWarning("[Step1] Shader not found: " + name);
                    continue;
                }
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Step1] Added URP Lit to Always Included Shaders");
            }
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Dove Wings";
            PlayerSettings.productName = "Three Realms";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.WebGL, "com.doverwings.threerealms.prototype");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.stripEngineCode = true;
            // Medium stripping: High can drop needed types; size is still fine without Brotli.
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            // GitHub Pages does not send Content-Encoding. Brotli + JS fallback
            // runs a blob-worker decoder that fails on iOS Safari ("undefined at blob:...:9").
            // Disabled = plain files Pages can serve as-is. No decompressor needed.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.initialMemorySize = 64;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Linear;
            PlayerSettings.WebGL.linearMemoryGrowthStep = 16;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.Default;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            TrySetActiveInputHandler(1); // Input System package only
            System.Environment.SetEnvironmentVariable("BEE_THREADS", "2");
            System.Environment.SetEnvironmentVariable("UNITY_IL2CPP_JOBS", "1");
        }

        static void TrySetActiveInputHandler(int value)
        {
            // 0 = Input Manager, 1 = Input System, 2 = Both
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Step1] Could not load ProjectSettings.asset to set input handler.");
                return;
            }
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null)
            {
                prop.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Step1] activeInputHandler = " + value);
            }
            else Debug.LogWarning("[Step1] activeInputHandler property not found.");
        }

        static void ApplyQualitySettings()
        {
            var names = new[] { "Low", "Medium", "High" };
            int count = QualitySettings.names.Length;
            for (int i = 0; i < names.Length && i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, true);
            }
            QualitySettings.SetQualityLevel(Mathf.Min(1, count - 1), true);
        }

        static void EnsurePhysicsKeep()
        {
            var existing = GameObject.Find("PhysicsKeep");
            if (existing != null) Object.DestroyImmediate(existing);
            var keep = new GameObject("PhysicsKeep");
            var cap = new GameObject("KeepCapsule");
            cap.transform.SetParent(keep.transform, false);
            cap.AddComponent<CapsuleCollider>();
            var sph = new GameObject("KeepSphere");
            sph.transform.SetParent(keep.transform, false);
            sph.AddComponent<SphereCollider>();
            var box = new GameObject("KeepBox");
            box.transform.SetParent(keep.transform, false);
            box.AddComponent<BoxCollider>();
            var cc = new GameObject("KeepCharacterController");
            cc.transform.SetParent(keep.transform, false);
            cc.AddComponent<CharacterController>();
            var rbGo = new GameObject("KeepRigidbody");
            rbGo.transform.SetParent(keep.transform, false);
            rbGo.AddComponent<BoxCollider>();
            var rb = rbGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            keep.transform.position = new Vector3(0f, -1000f, 0f);
            keep.SetActive(false);
            Debug.Log("[Step1] Added PhysicsKeep stub (Capsule/Sphere/Box/CharacterController/Rigidbody)");
        }

        static void CreateScene()

        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // Keep the default camera and light; bootstrap will reuse the camera and add the yard.
            var boot = new GameObject("Step1Bootstrap");
            boot.AddComponent<Step1Bootstrap>();
            // Referenced in the built scene so stripEngineCode keeps these types.
            // CreatePrimitive(Capsule/Sphere) needs CapsuleCollider/SphereCollider at runtime.
            EnsurePhysicsKeep();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            Debug.Log("[Step1] Saved scene " + ScenePath);
        }

        public static int BuildWebGL()
        {
            if (!Directory.Exists("Assets/_Project/Scenes") || !File.Exists(ScenePath))
                CreateScene();

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[Step1] Failed to switch active build target to WebGL.");
                return 2;
            }

            if (Directory.Exists(SitePath))
            {
                // Replace previous site output, keep the folder itself.
                foreach (var entry in Directory.GetFileSystemEntries(SitePath))
                {
                    try
                    {
                        if (Directory.Exists(entry)) Directory.Delete(entry, true);
                        else File.Delete(entry);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[Step1] Could not clear " + entry + ": " + e.Message);
                    }
                }
            }
            else Directory.CreateDirectory(SitePath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = SitePath,
                target = BuildTarget.WebGL,
                options = BuildOptions.CompressWithLz4
            };

            Debug.Log("[Step1] Starting WebGL build to " + SitePath);
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Step1] Build result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} size={summary.totalSize} time={summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            Debug.LogError("[Step1] " + msg.content);
                    }
                }
                return 1;
            }

            var index = Path.Combine(SitePath, "index.html");
            if (!File.Exists(index))
            {
                Debug.LogError("[Step1] Build succeeded but index.html is missing at " + index);
                return 1;
            }
            return 0;
        }
    }
}
#endif
