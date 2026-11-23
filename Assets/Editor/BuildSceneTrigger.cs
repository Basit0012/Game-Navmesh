using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RelicDash.Editor
{
    /// <summary>
    /// Checks on startup that the main game scene exists and is loaded.
    /// Can be manually rebuilt at any time via menu 'RelicDash > Build Complete Game Scene'
    /// or loaded via 'RelicDash > Open Main Game Scene'.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildSceneTrigger
    {
        static BuildSceneTrigger()
        {
            EditorApplication.delayCall += () =>
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled" || activeScene.path != "Assets/Scenes/MainGameScene.unity")
                {
                    if (File.Exists("Assets/Scenes/MainGameScene.unity"))
                    {
                        EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity", OpenSceneMode.Single);
                        Debug.Log("<color=#00FFAA><b>[BuildSceneTrigger] Automatically opened Assets/Scenes/MainGameScene.unity</b></color>");
                    }
                    else
                    {
                        RelicDashSceneBuilder.BuildCompleteGameScene();
                    }
                }

                if (!SessionState.GetBool("RelicDashEnhanced_v3", false))
                {
                    SessionState.SetBool("RelicDashEnhanced_v3", true);
                    Debug.Log("<color=#00FFAA><b>[BuildSceneTrigger] Auto-triggering enhanced game scene build & verification...</b></color>");
                    RelicDashSceneBuilder.BuildCompleteGameScene();
                    RelicDashExamVerifier.RunAllVerifications();
                }
            };
        }
    }
}
