#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fishing.V2.EditorTools
{
    public static class FishingPublicBuild
    {
        private const string ScenePath = "Assets/FishingV2/Scenes/FishingV2Prototype.unity";

        // Open a saved scene before the existing additive-scene validators run.
        // Count logged failures as well as thrown exceptions so batch CI cannot silently pass.
        public static void ValidateAndBuildWindows()
        {
            int errors = 0;
            Application.LogCallback listener = (message, stack, kind) =>
            {
                if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) errors++;
            };
            Application.logMessageReceived += listener;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                FishingV2V25Validator.ValidateV25CatalogAndMeshChannels();
                FishingV2SmokeTest.RunV25AllSpeciesSimulation();
                FishingV2SmokeTest.RunV25AfterBiteReleaseSimulation();
                if (errors > 0) throw new InvalidOperationException("Validation logged failures; build cancelled.");
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/FishingIsGood.exe"));
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                BuildReport result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (result.summary.result != BuildResult.Succeeded) errors++;
                Debug.Log("PUBLIC_BUILD result=" + result.summary.result + " errors=" + errors);
            }
            catch (Exception exception) { errors++; Debug.LogException(exception); }
            finally
            {
                Application.logMessageReceived -= listener;
                if (Application.isBatchMode) EditorApplication.Exit(errors == 0 ? 0 : 1);
            }
        }
    }
}
#endif
