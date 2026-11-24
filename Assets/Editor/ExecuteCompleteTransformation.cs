using UnityEngine;
using UnityEditor;

namespace RelicDash.Editor
{
    public static class ExecuteCompleteTransformation
    {
        public static void RunAll()
        {
            Debug.Log("<color=#FF0055><b>=================================================================</b></color>");
            Debug.Log("<color=#FF0055><b>[HOLLOW LOOP] STARTING COMPLETE AAA HORROR TRANSFORMATION PIPELINE</b></color>");
            Debug.Log("<color=#FF0055><b>=================================================================</b></color>");

            // 1. Build Complete Scene with all 3D assets, rigged characters, pond, and forest
            RelicDashSceneBuilder.BuildCompleteGameScene();

            // 2. Run All 12 CSE434 Examination Verifications
            RelicDashExamVerifier.RunAllVerifications();

            // 3. Capture all 7 In-Game High-Res Verification Screenshots
            CaptureInGameScreenshots.CaptureAllPerspectives();

            Debug.Log("<color=#00FFAA><b>=================================================================</b></color>");
            Debug.Log("<color=#00FFAA><b>[HOLLOW LOOP] TRANSFORMATION PIPELINE EXECUTED SUCCESSFULLY!</b></color>");
            Debug.Log("<color=#00FFAA><b>=================================================================</b></color>");
        }
    }
}
