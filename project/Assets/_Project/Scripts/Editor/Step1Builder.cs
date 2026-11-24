
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
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
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

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // Keep the default camera and light; bootstrap will reuse the camera and add the yard.
            var boot = new GameObject("Step1Bootstrap");
            boot.AddComponent<Step1Bootstrap>();
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
