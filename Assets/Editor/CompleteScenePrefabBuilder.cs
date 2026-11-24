using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

namespace RelicDash.Editor
{
    public static class CompleteScenePrefabBuilder
    {
        private const string PrefabDir = "Assets/Prefabs";

        [MenuItem("RelicDash/Create All 3D Asset Prefabs", priority = 4)]
        public static Dictionary<string, GameObject> CreateAllPrefabs()
        {
            Directory.CreateDirectory(PrefabDir);
            var prefabs = new Dictionary<string, GameObject>();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            Shader waterShader = Shader.Find("RelicDash/PondWaterShader");
            if (waterShader == null) waterShader = urpLit;

            // 1. Textures & Materials
            Texture2D texA = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Textures/texture-a.png");
            Texture2D texP = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Textures/texture-p.png");
            Texture2D rockDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MossyRock/mossy_rock_diff_1k.jpg");
            Texture2D rockNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MossyRock/mossy_rock_nor_gl_1k.jpg");
            Texture2D stoneDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RusticStone/rustic_stone_wall_diff_1k.jpg");
            Texture2D stoneNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RusticStone/rustic_stone_wall_nor_gl_1k.jpg");

            Material matPlayer = GetOrCreateMat("Assets/Materials/M_Player_Skin.mat", urpLit, Color.white, texA, null, 0.05f, 0.35f);
            Material matEnemy = GetOrCreateMat("Assets/Materials/M_Enemy_Skin.mat", urpLit, new Color(0.2f, 0.15f, 0.15f), texP, null, 0.4f, 0.25f, new Color(0.9f, 0.05f, 0.02f));
            Material matRock = GetOrCreateMat("Assets/Materials/M_PBR_Rock.mat", urpLit, new Color(0.45f, 0.48f, 0.42f), rockDiff, rockNor, 0.08f, 0.35f);
            Material matStone = GetOrCreateMat("Assets/Materials/M_PBR_AncientStone.mat", urpLit, new Color(0.42f, 0.44f, 0.46f), stoneDiff, stoneNor, 0.05f, 0.35f);
            Material matOak = GetOrCreateMat("Assets/Materials/M_Tree_Oak.mat", urpLit, new Color(0.35f, 0.45f, 0.25f), null, null, 0.02f, 0.25f);
            Material matDarkTree = GetOrCreateMat("Assets/Materials/M_Tree_Dark.mat", urpLit, new Color(0.18f, 0.18f, 0.18f), null, null, 0.15f, 0.2f);
            Material matPine = GetOrCreateMat("Assets/Materials/M_Tree_Pine.mat", urpLit, new Color(0.18f, 0.35f, 0.22f), null, null, 0.02f, 0.3f);
            Material matBush = GetOrCreateMat("Assets/Materials/M_Plant_Bush.mat", urpLit, new Color(0.25f, 0.48f, 0.22f), null, null, 0.02f, 0.35f);

            Material matWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Water_Pond.mat");
            if (matWater == null)
            {
                matWater = new Material(waterShader);
                AssetDatabase.CreateAsset(matWater, "Assets/Materials/M_Water_Pond.mat");
            }
            matWater.shader = waterShader;

            // 2. Player Humanoid Prefab (character-a.fbx scaled to 100 for true 1.8m human height)
            GameObject playerFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/character-a.fbx");
            if (playerFbx != null)
            {
                var pInstance = Object.Instantiate(playerFbx);
                pInstance.name = "Player_Explorer";
                pInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                pInstance.transform.localScale = new Vector3(100f, 100f, 100f);
                ApplyMat(pInstance, matPlayer);

                // Calibrate bone sockets for authentic humanoid anatomy
                Transform torso = pInstance.transform.Find("root/torso");
                if (torso != null)
                {
                    Transform armL = torso.Find("arm-left");
                    if (armL != null)
                    {
                        armL.localPosition = new Vector3(-0.0040f, 0.0010f, 0.0110f);
                        armL.localRotation = Quaternion.identity;
                        armL.localScale = Vector3.one;
                    }
                    Transform armR = torso.Find("arm-right");
                    if (armR != null)
                    {
                        armR.localPosition = new Vector3(0.0040f, 0.0010f, 0.0110f);
                        armR.localRotation = Quaternion.identity;
                        armR.localScale = Vector3.one;
                    }
                    Transform head = torso.Find("head");
                    if (head != null)
                    {
                        head.localPosition = new Vector3(0f, 0f, 0.0120f);
                        head.localRotation = Quaternion.identity;
                        head.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    }
                }

                string pPath = $"{PrefabDir}/Player_Explorer.prefab";
                var pPrefab = PrefabUtility.SaveAsPrefabAsset(pInstance, pPath);
                Object.DestroyImmediate(pInstance);
                prefabs["Player"] = pPrefab;
            }

            // 3. Enemy Stalker Monster Prefab (enemy-stalker.fbx scaled to 135 for 2.6m predatory monster)
            GameObject enemyFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/enemy-stalker.fbx");
            if (enemyFbx != null)
            {
                var eInstance = Object.Instantiate(enemyFbx);
                eInstance.name = "Enemy_Stalker_Monster";
                eInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                eInstance.transform.localScale = new Vector3(135f, 135f, 135f);
                ApplyMat(eInstance, matEnemy);

                // Calibrate bone sockets for monster anatomy
                Transform torso = eInstance.transform.Find("root/torso");
                if (torso != null)
                {
                    Transform armL = torso.Find("arm-left");
                    if (armL != null)
                    {
                        armL.localPosition = new Vector3(-0.0040f, 0.0010f, 0.0110f);
                        armL.localRotation = Quaternion.identity;
                    }
                    Transform armR = torso.Find("arm-right");
                    if (armR != null)
                    {
                        armR.localPosition = new Vector3(0.0040f, 0.0010f, 0.0110f);
                        armR.localRotation = Quaternion.identity;
                    }
                    Transform head = torso.Find("head");
                    if (head != null)
                    {
                        head.localPosition = new Vector3(0f, 0f, 0.0120f);
                        head.localRotation = Quaternion.identity;
                        head.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    }
                }

                string ePath = $"{PrefabDir}/Enemy_Stalker_Monster.prefab";
                var ePrefab = PrefabUtility.SaveAsPrefabAsset(eInstance, ePath);
                Object.DestroyImmediate(eInstance);
                prefabs["Enemy"] = ePrefab;
            }

            // 4. Tree Oak Prefab
            GameObject oakFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_oak.fbx");
            if (oakFbx != null)
            {
                var oakInstance = Object.Instantiate(oakFbx);
                oakInstance.name = "Nature_Tree_Oak";
                oakInstance.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
                oakInstance.transform.localScale = new Vector3(650f, 650f, 650f);
                ApplyMat(oakInstance, matOak);
                var obs = oakInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 1.0f;
                obs.height = 8.0f;
                string path = $"{PrefabDir}/Nature_Tree_Oak.prefab";
                prefabs["TreeOak"] = PrefabUtility.SaveAsPrefabAsset(oakInstance, path);
                Object.DestroyImmediate(oakInstance);
            }

            // 5. Tree Dark Prefab
            GameObject darkFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_oak_dark.fbx");
            if (darkFbx != null)
            {
                var darkInstance = Object.Instantiate(darkFbx);
                darkInstance.name = "Nature_Tree_Dark";
                darkInstance.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
                darkInstance.transform.localScale = new Vector3(650f, 650f, 650f);
                ApplyMat(darkInstance, matDarkTree);
                var obs = darkInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 1.0f;
                obs.height = 8.0f;
                string path = $"{PrefabDir}/Nature_Tree_Dark.prefab";
                prefabs["TreeDark"] = PrefabUtility.SaveAsPrefabAsset(darkInstance, path);
                Object.DestroyImmediate(darkInstance);
            }

            // 6. Tree Pine Prefab
            GameObject pineFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_pineDefaultA.fbx");
            if (pineFbx != null)
            {
                var pineInstance = Object.Instantiate(pineFbx);
                pineInstance.name = "Nature_Tree_Pine";
                pineInstance.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
                pineInstance.transform.localScale = new Vector3(600f, 600f, 600f);
                ApplyMat(pineInstance, matPine);
                var obs = pineInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 0.9f;
                obs.height = 9.0f;
                string path = $"{PrefabDir}/Nature_Tree_Pine.prefab";
                prefabs["TreePine"] = PrefabUtility.SaveAsPrefabAsset(pineInstance, path);
                Object.DestroyImmediate(pineInstance);
            }

            // 7. Large Rock Prefab
            GameObject rockFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_largeA.fbx");
            if (rockFbx != null)
            {
                var rockInstance = Object.Instantiate(rockFbx);
                rockInstance.name = "Nature_Rock_Large";
                rockInstance.transform.localScale = new Vector3(250f, 250f, 250f);
                ApplyMat(rockInstance, matRock);
                var obs = rockInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 1.2f;
                obs.height = 2.5f;
                string path = $"{PrefabDir}/Nature_Rock_Large.prefab";
                prefabs["RockLarge"] = PrefabUtility.SaveAsPrefabAsset(rockInstance, path);
                Object.DestroyImmediate(rockInstance);
            }

            // 8. Bush Prefab
            GameObject bushFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bush.fbx");
            if (bushFbx != null)
            {
                var bushInstance = Object.Instantiate(bushFbx);
                bushInstance.name = "Nature_Plant_Bush";
                bushInstance.transform.localScale = new Vector3(200f, 200f, 200f);
                ApplyMat(bushInstance, matBush);
                string path = $"{PrefabDir}/Nature_Plant_Bush.prefab";
                prefabs["Bush"] = PrefabUtility.SaveAsPrefabAsset(bushInstance, path);
                Object.DestroyImmediate(bushInstance);
            }

            // 9. Fortified Wall Prefab
            GameObject wallFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/wall-fortified.fbx");
            if (wallFbx != null)
            {
                var wallInstance = Object.Instantiate(wallFbx);
                wallInstance.name = "Ruins_Fortified_Wall";
                wallInstance.transform.localScale = new Vector3(350f, 350f, 350f);
                ApplyMat(wallInstance, matStone);
                var obs = wallInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.size = new Vector3(5.0f, 4.5f, 2.0f);
                string path = $"{PrefabDir}/Ruins_Fortified_Wall.prefab";
                prefabs["Wall"] = PrefabUtility.SaveAsPrefabAsset(wallInstance, path);
                Object.DestroyImmediate(wallInstance);
            }

            // 10. Ancient Watchtower Prefab
            GameObject towerFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/tower-base.fbx");
            if (towerFbx != null)
            {
                var towerInstance = Object.Instantiate(towerFbx);
                towerInstance.name = "Ruins_Ancient_Watchtower";
                towerInstance.transform.localScale = new Vector3(550f, 550f, 550f);
                ApplyMat(towerInstance, matStone);
                var obs = towerInstance.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.size = new Vector3(5.5f, 7.0f, 5.5f);
                string path = $"{PrefabDir}/Ruins_Ancient_Watchtower.prefab";
                prefabs["Tower"] = PrefabUtility.SaveAsPrefabAsset(towerInstance, path);
                Object.DestroyImmediate(towerInstance);
            }

            // 11. Pond Water Feature Prefab
            var pondInstance = new GameObject("Pond_Water_Feature");
            var waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterSurface.name = "Pond_Water_Surface";
            waterSurface.transform.parent = pondInstance.transform;
            waterSurface.transform.localPosition = Vector3.zero;
            waterSurface.transform.localScale = new Vector3(11.2f, 0.02f, 11.2f);
            waterSurface.GetComponent<Renderer>().sharedMaterial = matWater;
            Object.DestroyImmediate(waterSurface.GetComponent<Collider>());

            var pondAudio = pondInstance.AddComponent<AudioSource>();
            pondAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/water_ripples_ambience.wav");
            pondAudio.loop = true;
            pondAudio.spatialBlend = 1.0f;
            pondAudio.minDistance = 3.0f;
            pondAudio.maxDistance = 25.0f;
            pondAudio.volume = 0.65f;
            pondAudio.playOnAwake = true;

            if (rockFbx != null)
            {
                int pRockCount = 10;
                float pRadius = 5.6f;
                for (int i = 0; i < pRockCount; i++)
                {
                    float angle = i * (Mathf.PI * 2f / pRockCount);
                    float rx = Mathf.Cos(angle) * (pRadius + 0.2f);
                    float rz = Mathf.Sin(angle) * (pRadius + 0.2f);
                    var rObj = Object.Instantiate(rockFbx, pondInstance.transform);
                    rObj.name = $"Shoreline_Bolder_{i + 1}";
                    rObj.transform.localPosition = new Vector3(rx, 0.2f, rz);
                    rObj.transform.localScale = new Vector3(100f, 100f, 100f) * 1.15f;
                    ApplyMat(rObj, matRock);
                    var rObs = rObj.AddComponent<NavMeshObstacle>();
                    rObs.carving = true;
                    rObs.shape = NavMeshObstacleShape.Capsule;
                    rObs.radius = 1.2f;
                    rObs.height = 2.5f;
                }
            }

            string pondPath = $"{PrefabDir}/Pond_Water_Feature.prefab";
            prefabs["Pond"] = PrefabUtility.SaveAsPrefabAsset(pondInstance, pondPath);
            Object.DestroyImmediate(pondInstance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00FFAA><b>[CompleteScenePrefabBuilder] Successfully created {prefabs.Count} standard AAA 3D asset prefabs in {PrefabDir}!</b></color>");
            return prefabs;
        }

        private static Material GetOrCreateMat(string path, Shader shader, Color color, Texture2D tex, Texture2D nor, float metallic, float smoothness, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            if (nor != null) { mat.SetTexture("_BumpMap", nor); mat.EnableKeyword("_NORMALMAP"); }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.SetColor("_EmissionColor", emission.Value);
                mat.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ApplyMat(GameObject obj, Material mat)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }
    }
}
