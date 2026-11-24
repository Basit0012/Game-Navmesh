using System.IO;
using UnityEngine;
using UnityEditor;

namespace RelicDash.Editor
{
    /// <summary>
    /// Checks on startup and domain reload to ensure the complete AAA horror scene is built and verified.
    /// Can be manually rebuilt at any time via menu 'RelicDash > Build Complete Game Scene'.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildSceneTrigger
    {
        private const string FlagFile = "Temp/HollowLoop_HorrorBuild_v1.flag";

        static BuildSceneTrigger()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(FlagFile) || !File.Exists("Assets/Scenes/MainGameScene.unity"))
                {
                    try
                    {
                        Directory.CreateDirectory("Temp");
                        File.WriteAllText(FlagFile, System.DateTime.UtcNow.ToString("O"));
                        Debug.Log("<color=#00FFAA><b>[BuildSceneTrigger] Automatically building HOLLOW LOOP complete game scene...</b></color>");
                        RelicDashSceneBuilder.BuildCompleteGameScene();
                        RelicDashExamVerifier.RunAllVerifications();
                        Debug.Log("<color=#00FFAA><b>[BuildSceneTrigger] Scene build and verification completed successfully!</b></color>");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[BuildSceneTrigger] Error during scene build: {ex}");
                    }
                }
            };
        }
    }
}
