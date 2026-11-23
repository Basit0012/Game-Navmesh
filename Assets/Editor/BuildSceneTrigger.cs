using System.IO;
using UnityEngine;
using UnityEditor;

namespace RelicDash.Editor
{
    /// <summary>
    /// Checks on startup that the main game scene exists and is loaded.
    /// Can be manually rebuilt at any time via menu 'RelicDash > Build Complete Game Scene'.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildSceneTrigger
    {
        static BuildSceneTrigger()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("RelicDashEnhanced_v2", false))
                {
                    SessionState.SetBool("RelicDashEnhanced_v2", true);
                    Debug.Log("<color=#00FFAA><b>[BuildSceneTrigger] Auto-triggering enhanced game scene build & verification...</b></color>");
                    RelicDashSceneBuilder.BuildCompleteGameScene();
                    RelicDashExamVerifier.RunAllVerifications();
                }
            };
        }
    }
}
