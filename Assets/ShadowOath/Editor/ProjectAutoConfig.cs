using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowOath.EditorTools
{
    public static class ProjectAutoConfig
    {
        const string SettingsDir = "Assets/ShadowOath/Settings";
        const string RendererPath = SettingsDir + "/ShadowOath_Renderer.asset";
        const string PipelinePath = SettingsDir + "/ShadowOath_URP.asset";
        const string ScenePath = "Assets/ShadowOath/Scenes/Main.unity";

        static bool _ran;
        static int _materialTries;

        [InitializeOnLoadMethod]
        static void AutoConfigure()
        {
            EditorApplication.delayCall += () =>
            {
                if (_ran || EditorApplication.isPlayingOrWillChangePlaymode) return;
                _ran = true;
                EnsureFolders();
                EnsureBuildMaterials();
                bool ready = GraphicsSettings.defaultRenderPipeline != null && File.Exists(ScenePath);
                if (!ready) Configure();
                else EnsurePlayerSettings();
                AssetDatabase.SaveAssets();
            };
        }

        [MenuItem("Tools/Shadow Oath/Configure Project")]
        public static void Configure()
        {
            EnsureFolders();
            var pipeline = EnsurePipeline();
            if (pipeline != null) AssignPipeline(pipeline);
            EnsurePlayerSettings();
            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Shadow Oath: project configuration finished. Open Main and press Play.");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ShadowOath"))
                AssetDatabase.CreateFolder("Assets", "ShadowOath");
            if (!AssetDatabase.IsValidFolder("Assets/ShadowOath/Scenes"))
                AssetDatabase.CreateFolder("Assets/ShadowOath", "Scenes");
            if (!AssetDatabase.IsValidFolder(SettingsDir))
                AssetDatabase.CreateFolder("Assets/ShadowOath", "Settings");
        }

        static UniversalRenderPipelineAsset EnsurePipeline()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (existing != null) return existing;

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            if (pipeline == null)
            {
                Debug.LogError("Shadow Oath: UniversalRenderPipelineAsset.Create returned null.");
                return null;
            }

            AssetDatabase.CreateAsset(renderer, RendererPath);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        static void AssignPipeline(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            int count = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            for (int i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            if (count > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(current, 0, count - 1), false);
            else QualitySettings.renderPipeline = pipeline;
        }

        static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "Oathbound";
            PlayerSettings.productName = "Shadow Oath";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;
            ForceInputSystem();
        }

        static void ForceInputSystem()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            if (!File.Exists(path)) return;
            string text = File.ReadAllText(path);
            const string key = "activeInputHandler:";
            if (text.Contains(key))
            {
                text = System.Text.RegularExpressions.Regex.Replace(text, @"activeInputHandler:\s*\d+", "activeInputHandler: 1");
            }
            else
            {
                text = text.TrimEnd() + "\n  activeInputHandler: 1\n";
            }
            File.WriteAllText(path, text);
        }

        static void EnsureBuildMaterials()
        {
            if (Shader.Find("ShadowOath/InkOutline") == null)
            {
                if (_materialTries++ < 6) EditorApplication.delayCall += EnsureBuildMaterials;
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/ShadowOath/Resources"))
                AssetDatabase.CreateFolder("Assets/ShadowOath", "Resources");
            CreateMaterial("Assets/ShadowOath/Resources/InkOutline.mat", "ShadowOath/InkOutline");
            CreateMaterial("Assets/ShadowOath/Resources/UnlitColor.mat", "ShadowOath/UnlitColor");
            CreateMaterial("Assets/ShadowOath/Resources/InkParticle.mat", "ShadowOath/InkParticle");
            CreateMaterial("Assets/ShadowOath/Resources/Lit.mat", "Universal Render Pipeline/Lit");
        }

        static void CreateMaterial(string path, string shaderName)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("Shadow Oath: shader not found yet: " + shaderName);
                return;
            }
            var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            bool found = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == ScenePath)
                {
                    scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                    found = true;
                }
                else scenes.Add(scene);
            }
            if (!found) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
