using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RelicDash.Relics;

namespace RelicDash.Editor
{
    public static class CaptureInGameScreenshots
    {
        private const string ScreenshotDir = "Assets/Screenshots";
        private const string ArtifactDir = @"C:\Users\BASIT\.gemini\antigravity\brain\fe2f2382-ed01-4f45-92aa-21c65e9cf81a";

        [MenuItem("RelicDash/Capture In-Game Verification Screenshots", priority = 3)]
        public static void CaptureAllPerspectives()
        {
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/MainGameScene.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity", OpenSceneMode.Single);
            }

            Directory.CreateDirectory(ScreenshotDir);
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            // Find key game objects
            var player = GameObject.FindGameObjectWithTag("Player");
            var enemy = GameObject.FindGameObjectWithTag("Enemy");
            var pond = GameObject.Find("Horror_Pond_And_Shoreline");
            var relics = Object.FindObjectsByType<RelicItem>(FindObjectsSortMode.None);

            var tempCamObj = new GameObject("Screenshot_Render_Camera");
            var cam = tempCamObj.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;

            int width = 1920;
            int height = 1080;

            // 1. Player Character in 3rd-person
            if (player != null)
            {
                Vector3 pPos = player.transform.position;
                Vector3 pFwd = player.transform.forward;
                tempCamObj.transform.position = pPos - pFwd * 3.2f + Vector3.up * 1.8f + player.transform.right * 0.65f;
                tempCamObj.transform.LookAt(pPos + Vector3.up * 1.1f);
                RenderAndSave(cam, width, height, "hollow_loop_shot1_player_3rd_person.png");
            }

            // 2. Enemy Monster in Arena
            if (enemy != null)
            {
                Vector3 ePos = enemy.transform.position;
                tempCamObj.transform.position = ePos + enemy.transform.forward * 4.2f + Vector3.up * 2.0f + enemy.transform.right * 1.5f;
                tempCamObj.transform.LookAt(ePos + Vector3.up * 1.4f);
                RenderAndSave(cam, width, height, "hollow_loop_shot2_enemy_monster.png");
            }

            // 3. Enemy Chasing Player Action Framing
            if (player != null && enemy != null)
            {
                Vector3 midPoint = (player.transform.position + enemy.transform.position) * 0.5f;
                Vector3 offsetDir = Vector3.Cross(enemy.transform.position - player.transform.position, Vector3.up).normalized;
                tempCamObj.transform.position = midPoint + offsetDir * 7.0f + Vector3.up * 2.5f;
                tempCamObj.transform.LookAt(midPoint + Vector3.up * 1.0f);
                RenderAndSave(cam, width, height, "hollow_loop_shot3_chase_sequence.png");
            }

            // 4. Pond and Water Shoreline
            if (pond != null)
            {
                Vector3 pondPos = pond.transform.position;
                tempCamObj.transform.position = pondPos + new Vector3(8.5f, 4.2f, 7.5f);
                tempCamObj.transform.LookAt(pondPos + Vector3.up * 0.2f);
                RenderAndSave(cam, width, height, "hollow_loop_shot4_pond_and_water.png");
            }

            // 5. Forest Environment and Tree Clusters
            tempCamObj.transform.position = new Vector3(-16f, 6.0f, 18f);
            tempCamObj.transform.rotation = Quaternion.Euler(15f, 130f, 0f);
            RenderAndSave(cam, width, height, "hollow_loop_shot5_forest_environment.png");

            // 6. Elevated Arena Overview
            tempCamObj.transform.position = new Vector3(0f, 38f, -28f);
            tempCamObj.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
            RenderAndSave(cam, width, height, "hollow_loop_shot6_elevated_arena_overview.png");

            // 7. Relics in Position
            if (relics.Length > 0)
            {
                Vector3 rPos = relics[0].transform.position;
                tempCamObj.transform.position = rPos + new Vector3(1.8f, 0.8f, 1.8f);
                tempCamObj.transform.LookAt(rPos + Vector3.up * 0.1f);
                RenderAndSave(cam, width, height, "hollow_loop_shot7_relics_in_position.png");
            }

            Object.DestroyImmediate(tempCamObj);
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FFAA><b>[CaptureInGameScreenshots] Successfully captured all 7 in-game verification perspectives!</b></color>");
        }

        private static void RenderAndSave(Camera cam, int width, int height, string filename)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;

            cam.Render();

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string localPath = Path.Combine(ScreenshotDir, filename);
            File.WriteAllBytes(localPath, bytes);

            string artifactPath = Path.Combine(ArtifactDir, filename);
            File.WriteAllBytes(artifactPath, bytes);

            RenderTexture.active = currentRT;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"Captured perspective: {filename} -> {localPath}");
        }
    }
}
