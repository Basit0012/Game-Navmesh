using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Animations.Rigging;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using RelicDash.Core;
using RelicDash.Data;
using RelicDash.Player;
using RelicDash.Relics;
using RelicDash.AI;
using RelicDash.Environment;
using RelicDash.UI;
using RelicDash.Cinematics;

namespace RelicDash.Editor
{
    /// <summary>
    /// Automated Scene Builder and Validator for "RELIC DASH: THE LOST ISLAND".
    /// Fulfills all CSE434 examination requirements programmatically:
    /// 1. Procedural Terrain with runtime Perlin Noise
    /// 2. Relic Shader with pulsing emission and particles on 3 relics
    /// 3. Cinemachine camera tracking player
    /// 4. Player controls with New Input System
    /// 5. Animation Rigging Multi-Aim Constraint for relic head-tracking
    /// 6. World-Space UI relic counter
    /// 7. GameSettings ScriptableObject
    /// 8. Singleton GameManager with State Pattern
    /// 9. Observer Pattern GameEvents bus
    /// 10. Modular Behavior Tree Enemy AI (Patrol, 8m Detection, Chase, Catch)
    /// 11. NavMeshSurface bake and NavMeshObstacles with Carve
    /// 12. 5-second Victory Timeline sequence with gate animation and Cinemachine track
    /// </summary>
    public static class RelicDashSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainGameScene.unity";
        private const string SettingsPath = "Assets/Settings/GameSettings.asset";
        private const string TimelinePath = "Assets/Animations/VictoryTimeline.playable";

        [MenuItem("RelicDash/Build Complete Game Scene", priority = 1)]
        public static void BuildCompleteGameScene()
        {
            Debug.Log("<color=#00FFAA><b>[RelicDashSceneBuilder] Starting Complete Scene Generation...</b></color>");

            // Ensure TMP Essential Resources are imported
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
            }

            EnsureTagsAndLayers();
            GameSettings settings = EnsureGameSettingsAsset();
            Dictionary<string, Material> materials = CreateOrLoadMaterials();

            // Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting & Atmosphere
            SetupLightingAndPostProcessing();

            // Environment & Procedural Terrain
            var (terrainObj, terrainGen, navSurface) = BuildProceduralTerrain(settings, materials);

            // Ancient Exit Gate
            ExitGate exitGate = BuildExitGate(materials, terrainGen);

            // Three Ancient Energy Relics
            List<RelicItem> relics = BuildAncientRelics(materials, terrainGen);

            // Patrol Waypoints
            List<Transform> waypoints = BuildPatrolWaypoints(terrainGen);

            // Player Avatar with Rigging, New Input System, and World UI
            var (playerObj, playerCtrl, headAimConstraint, aimTarget) = BuildPlayer(settings, materials, terrainGen);

            // Enemy Guardian with NavMeshAgent and Behavior Tree
            EnemyBTController guardian = BuildEnemyGuardian(settings, materials, terrainGen, playerObj.transform, waypoints);

            // Ancient Ruins & Carving Obstacles
            BuildRuinsAndObstacles(materials, terrainGen);

            // Nature Foliage & Island Props
            BuildEnvironmentFoliageAndProps(terrainGen);

            // Background Music & Audio Atmosphere
            BuildAudioAtmosphere();

            // Cameras & Cinemachine
            var (mainCam, playerVCam, gateVCam) = BuildCinemachineCameras(playerObj.transform, exitGate.transform);

            // Timeline Victory Sequence
            PlayableDirector director = BuildVictoryTimeline(playerVCam, gateVCam, exitGate);

            // Cinematic Victory Controller
            var cinematicObj = new GameObject("CinematicDirector");
            var timelineCtrl = cinematicObj.AddComponent<TimelineVictoryController>();
            // Use reflection or serialized object to assign fields cleanly
            var serializedTimeline = new SerializedObject(timelineCtrl);
            serializedTimeline.FindProperty("playableDirector").objectReferenceValue = director;
            serializedTimeline.FindProperty("exitGate").objectReferenceValue = exitGate;
            serializedTimeline.ApplyModifiedProperties();

            // Core GameManager
            var gmObj = new GameObject("GameManager");
            var gameManager = gmObj.AddComponent<GameManager>();
            var serializedGM = new SerializedObject(gameManager);
            serializedGM.FindProperty("gameSettings").objectReferenceValue = settings;
            serializedGM.ApplyModifiedProperties();

            // Screen-space Canvas & HUD
            BuildScreenUI();

            // Bake NavMesh over generated terrain
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
                Debug.Log("[RelicDashSceneBuilder] NavMeshSurface baked successfully.");
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetSceneInBuildSettings(ScenePath);

            // Open scene in active Unity Editor so examiner and player see it immediately
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("<color=#00FFAA><b>[RelicDashSceneBuilder] Complete Game Scene Built and Saved to: " + ScenePath + "</b></color>");
        }

        private static void EnsureTagsAndLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            string[] neededTags = { "Player", "Relic", "ExitGate", "Enemy", "Waypoint" };
            foreach (string t in neededTags)
            {
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == t)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = t;
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        private static GameSettings EnsureGameSettingsAsset()
        {
            GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        private static Dictionary<string, Material> CreateOrLoadMaterials()
        {
            var dict = new Dictionary<string, Material>();
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader relicCustomShader = Shader.Find("RelicDash/RelicPulsingEmission");
            if (relicCustomShader == null) relicCustomShader = urpLitShader;

            // Relic Pulsing Material
            string relicMatPath = "Assets/Materials/M_Relic_CyanPulse.mat";
            Material relicMat = AssetDatabase.LoadAssetAtPath<Material>(relicMatPath);
            if (relicMat == null)
            {
                relicMat = new Material(relicCustomShader);
                relicMat.SetColor("_BaseColor", new Color(0.05f, 0.5f, 0.7f, 1f));
                relicMat.SetColor("_EmissionColor", new Color(0f, 3.5f, 4.5f, 1f));
                relicMat.EnableKeyword("_EMISSION");
                AssetDatabase.CreateAsset(relicMat, relicMatPath);
            }
            dict["Relic"] = relicMat;

            // Grass Material
            dict["Grass"] = GetOrCreateMaterial("Assets/Materials/M_Terrain_Grass.mat", urpLitShader, new Color(0.24f, 0.48f, 0.18f));
            // Cliff Rock Material
            dict["Rock"] = GetOrCreateMaterial("Assets/Materials/M_Terrain_Rock.mat", urpLitShader, new Color(0.38f, 0.38f, 0.4f));
            // Ancient Stone Material
            dict["AncientStone"] = GetOrCreateMaterial("Assets/Materials/M_AncientStone.mat", urpLitShader, new Color(0.35f, 0.4f, 0.36f));
            // Exit Gate Gold Material
            dict["GateTrim"] = GetOrCreateMaterial("Assets/Materials/M_GateTrim.mat", urpLitShader, new Color(0.75f, 0.65f, 0.25f), new Color(1.5f, 1.2f, 0.3f));
            // Portal Vortex Material
            dict["Portal"] = GetOrCreateMaterial("Assets/Materials/M_PortalVortex.mat", urpLitShader, new Color(0f, 0.85f, 1f, 0.85f), new Color(0f, 2.5f, 3.5f));
            // Guardian Armor Material
            dict["GuardianArmor"] = GetOrCreateMaterial("Assets/Materials/M_Guardian_Armor.mat", urpLitShader, new Color(0.12f, 0.12f, 0.15f));
            // Guardian Visor Material
            dict["GuardianVisor"] = GetOrCreateMaterial("Assets/Materials/M_Guardian_Visor.mat", urpLitShader, new Color(0.9f, 0.05f, 0.7f), new Color(3f, 0.1f, 2.5f));
            // Player Suit Material
            dict["PlayerBody"] = GetOrCreateMaterial("Assets/Materials/M_Player_Body.mat", urpLitShader, new Color(0.18f, 0.35f, 0.7f));
            // Player Visor Material
            dict["PlayerVisor"] = GetOrCreateMaterial("Assets/Materials/M_Player_Visor.mat", urpLitShader, new Color(0.95f, 0.8f, 0.25f), new Color(1.5f, 1.2f, 0.2f));

            AssetDatabase.SaveAssets();
            return dict;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color baseColor, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.SetColor("_BaseColor", baseColor);
                if (emission.HasValue)
                {
                    mat.SetColor("_EmissionColor", emission.Value);
                    mat.EnableKeyword("_EMISSION");
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetupLightingAndPostProcessing()
        {
            // Directional Sun
            var sunObj = new GameObject("Directional Light - Sun");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.95f, 0.85f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Atmospheric Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.68f, 0.80f, 0.90f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.75f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.2f);
        }

        private static (TerrainLayer, TerrainLayer) EnsureTerrainLayers()
        {
            string grassTexPath = "Assets/Materials/T_Grass_Stylized.png";
            string rockTexPath = "Assets/Materials/T_Rock_Stylized.png";

            if (!File.Exists(grassTexPath))
            {
                var grassTex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
                Color baseGreen = new Color(0.26f, 0.52f, 0.20f);
                Color darkGreen = new Color(0.16f, 0.38f, 0.12f);
                Color brightGreen = new Color(0.36f, 0.65f, 0.26f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                        float n2 = Mathf.PerlinNoise(x * 0.25f + 10f, y * 0.25f + 10f);
                        Color c = Color.Lerp(darkGreen, baseGreen, n1);
                        if (n2 > 0.65f) c = Color.Lerp(c, brightGreen, (n2 - 0.65f) * 2.5f);
                        grassTex.SetPixel(x, y, c);
                    }
                }
                grassTex.Apply();
                File.WriteAllBytes(grassTexPath, grassTex.EncodeToPNG());
                AssetDatabase.ImportAsset(grassTexPath, ImportAssetOptions.ForceUpdate);
            }

            if (!File.Exists(rockTexPath))
            {
                var rockTex = new Texture2D(256, 256, TextureFormat.RGBA32, true);
                Color baseRock = new Color(0.38f, 0.38f, 0.40f);
                Color darkRock = new Color(0.22f, 0.22f, 0.24f);
                Color lightRock = new Color(0.52f, 0.52f, 0.54f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                        float n2 = Mathf.PerlinNoise(x * 0.18f + 25f, y * 0.18f + 25f);
                        Color c = Color.Lerp(darkRock, baseRock, n1);
                        if (n2 > 0.6f) c = Color.Lerp(c, lightRock, (n2 - 0.6f) * 2.2f);
                        rockTex.SetPixel(x, y, c);
                    }
                }
                rockTex.Apply();
                File.WriteAllBytes(rockTexPath, rockTex.EncodeToPNG());
                AssetDatabase.ImportAsset(rockTexPath, ImportAssetOptions.ForceUpdate);
            }

            Texture2D gTex = AssetDatabase.LoadAssetAtPath<Texture2D>(grassTexPath);
            Texture2D rTex = AssetDatabase.LoadAssetAtPath<Texture2D>(rockTexPath);

            string grassLayerPath = "Assets/Materials/GrassLayer.terrainlayer";
            TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(grassLayerPath);
            if (grassLayer == null)
            {
                grassLayer = new TerrainLayer();
                AssetDatabase.CreateAsset(grassLayer, grassLayerPath);
            }
            grassLayer.diffuseTexture = gTex != null ? gTex : Texture2D.whiteTexture;
            grassLayer.tileSize = new Vector2(4, 4);
            EditorUtility.SetDirty(grassLayer);

            string rockLayerPath = "Assets/Materials/RockLayer.terrainlayer";
            TerrainLayer rockLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(rockLayerPath);
            if (rockLayer == null)
            {
                rockLayer = new TerrainLayer();
                AssetDatabase.CreateAsset(rockLayer, rockLayerPath);
            }
            rockLayer.diffuseTexture = rTex != null ? rTex : Texture2D.whiteTexture;
            rockLayer.tileSize = new Vector2(6, 6);
            EditorUtility.SetDirty(rockLayer);

            AssetDatabase.SaveAssets();
            return (grassLayer, rockLayer);
        }

        private static (GameObject, ProceduralTerrainGenerator, NavMeshSurface) BuildProceduralTerrain(GameSettings settings, Dictionary<string, Material> materials)
        {
            var envRoot = new GameObject("Environment");
            var navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;

            var terrainObj = new GameObject("Procedural_Floating_Island");
            terrainObj.transform.parent = envRoot.transform;
            terrainObj.transform.position = new Vector3(-60, 0, -60);

            var terrainData = new TerrainData
            {
                heightmapResolution = 129,
                size = new Vector3(120, 24, 120),
                alphamapResolution = 128
            };

            // Setup Terrain Layers with procedural stylized textures
            var (grassLayer, rockLayer) = EnsureTerrainLayers();
            terrainData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer };

            var terrain = terrainObj.AddComponent<Terrain>();
            var terrainCol = terrainObj.AddComponent<TerrainCollider>();
            terrain.terrainData = terrainData;
            terrainCol.terrainData = terrainData;
            terrain.materialTemplate = materials["Grass"];

            var generator = terrainObj.AddComponent<ProceduralTerrainGenerator>();
            var serializedGen = new SerializedObject(generator);
            serializedGen.FindProperty("settings").objectReferenceValue = settings;
            serializedGen.FindProperty("navMeshSurface").objectReferenceValue = navSurface;
            serializedGen.ApplyModifiedProperties();

            // Run procedural generation at editor-time to establish base terrain heights
            generator.GenerateProceduralTerrain();

            // Paint rock on steep cliff edges and grass on gentle plateaus
            int alphaRes = terrainData.alphamapResolution;
            float[,,] alphaMaps = new float[alphaRes, alphaRes, 2];
            for (int y = 0; y < alphaRes; y++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    float normX = (float)x / (alphaRes - 1);
                    float normY = (float)y / (alphaRes - 1);
                    float steepness = terrainData.GetSteepness(normX, normY);
                    float rockWeight = Mathf.Clamp01((steepness - 15f) / 25f);
                    alphaMaps[y, x, 0] = 1f - rockWeight; // Grass
                    alphaMaps[y, x, 1] = rockWeight;      // Rock
                }
            }
            terrainData.SetAlphamaps(0, 0, alphaMaps);

            return (terrainObj, generator, navSurface);
        }

        private static ExitGate BuildExitGate(Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            var gateRoot = new GameObject("Ancient_Exit_Gate");
            gateRoot.tag = "ExitGate";
            float gateX = 0f;
            float gateZ = 38f;
            float gateY = terrainGen.GetWorldHeight(gateX, gateZ);
            gateRoot.transform.position = new Vector3(gateX, gateY, gateZ);

            // Left Monolith Pillar
            var leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPillar.name = "Pillar_Left";
            leftPillar.transform.parent = gateRoot.transform;
            leftPillar.transform.localPosition = new Vector3(-4.0f, 4.0f, 0);
            leftPillar.transform.localScale = new Vector3(1.6f, 8.0f, 1.6f);
            leftPillar.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];

            // Right Monolith Pillar
            var rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPillar.name = "Pillar_Right";
            rightPillar.transform.parent = gateRoot.transform;
            rightPillar.transform.localPosition = new Vector3(4.0f, 4.0f, 0);
            rightPillar.transform.localScale = new Vector3(1.6f, 8.0f, 1.6f);
            rightPillar.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];

            // Lintel Arch
            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "Arch_Lintel";
            lintel.transform.parent = gateRoot.transform;
            lintel.transform.localPosition = new Vector3(0, 8.4f, 0);
            lintel.transform.localScale = new Vector3(10.0f, 1.4f, 2.0f);
            lintel.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];

            // Left Sliding Door
            var leftDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftDoor.name = "Door_Left";
            leftDoor.transform.parent = gateRoot.transform;
            leftDoor.transform.localPosition = new Vector3(-1.6f, 3.8f, 0);
            leftDoor.transform.localScale = new Vector3(3.2f, 7.6f, 0.6f);
            leftDoor.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];

            // Right Sliding Door
            var rightDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightDoor.name = "Door_Right";
            rightDoor.transform.parent = gateRoot.transform;
            rightDoor.transform.localPosition = new Vector3(1.6f, 3.8f, 0);
            rightDoor.transform.localScale = new Vector3(3.2f, 7.6f, 0.6f);
            rightDoor.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];

            // Portal Vortex Disc (Visual feedback when opened)
            var portalObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portalObj.name = "Portal_Vortex";
            portalObj.transform.parent = gateRoot.transform;
            portalObj.transform.localPosition = new Vector3(0, 4.0f, 0.2f);
            portalObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            portalObj.transform.localScale = new Vector3(5.5f, 0.05f, 6.5f);
            portalObj.GetComponent<Renderer>().sharedMaterial = materials["Portal"];
            Object.DestroyImmediate(portalObj.GetComponent<Collider>());

            // Gate Light
            var gateLight = gateRoot.AddComponent<Light>();
            gateLight.type = LightType.Point;
            gateLight.color = new Color(0f, 0.9f, 1f);
            gateLight.range = 14f;
            gateLight.intensity = 1.0f;

            // Trigger Collider for Victory Enter
            var triggerCol = gateRoot.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0, 3.5f, 0);
            triggerCol.size = new Vector3(5f, 7f, 2.5f);

            // NavMesh Obstacle with Carve = true
            var obstacle = gateRoot.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(10f, 8f, 2.5f);

            // 3D Ancient Stone Stairs decoration
            var stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/stairs-stone.glb");
            if (stairsPrefab != null)
            {
                var stairs = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, gateRoot.transform);
                stairs.name = "Gate_Stairs";
                stairs.transform.localPosition = new Vector3(0, 0, -3.0f);
                stairs.transform.localScale = new Vector3(2.5f, 1.2f, 2.0f);
                stairs.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            }

            // ExitGate Script
            var gateScript = gateRoot.AddComponent<ExitGate>();
            var sGate = new SerializedObject(gateScript);
            sGate.FindProperty("leftDoor").objectReferenceValue = leftDoor.transform;
            sGate.FindProperty("rightDoor").objectReferenceValue = rightDoor.transform;
            sGate.FindProperty("portalVortexEffect").objectReferenceValue = portalObj;
            sGate.FindProperty("gateGlowLight").objectReferenceValue = gateLight;
            sGate.ApplyModifiedProperties();

            return gateScript;
        }

        private static List<RelicItem> BuildAncientRelics(Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            var relics = new List<RelicItem>();
            var relicRoot = new GameObject("Relics_Ancient_Orbs");

            Vector3[] relicPositions = new Vector3[]
            {
                new Vector3(-24f, 0, 20f),  // Western Mystic Grove
                new Vector3(26f, 0, 16f),   // Eastern Monolith Ruins
                new Vector3(-6f, 0, -22f)   // Southern Altar Overlook
            };

            var ringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/statue_ring.glb");
            AudioClip pickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/relic_pickup.ogg");

            for (int i = 0; i < 3; i++)
            {
                int relicIndex = i + 1;
                Vector3 basePos = relicPositions[i];
                float y = terrainGen.GetWorldHeight(basePos.x, basePos.z) + 1.2f;
                Vector3 spawnPos = new Vector3(basePos.x, y, basePos.z);

                var relicObj = new GameObject($"Relic_Ancient_Orb_{relicIndex}");
                relicObj.tag = "Relic";
                relicObj.transform.parent = relicRoot.transform;
                relicObj.transform.position = spawnPos;

                // 3D Mystic Altar Pedestal beneath orb
                if (ringPrefab != null)
                {
                    var altar = (GameObject)PrefabUtility.InstantiatePrefab(ringPrefab, relicObj.transform);
                    altar.name = "Altar_Pedestal";
                    altar.transform.localPosition = new Vector3(0, -1.0f, 0);
                    altar.transform.localScale = Vector3.one * 1.5f;
                }

                // Glowing Outer Orb Mesh
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Relic_Sphere_Core";
                orb.transform.parent = relicObj.transform;
                orb.transform.localPosition = Vector3.zero;
                orb.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                orb.GetComponent<Renderer>().sharedMaterial = materials["Relic"];
                Object.DestroyImmediate(orb.GetComponent<Collider>());

                // Inner floating jewel
                var innerJewel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                innerJewel.name = "Relic_Inner_Rune";
                innerJewel.transform.parent = orb.transform;
                innerJewel.transform.localPosition = Vector3.zero;
                innerJewel.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                innerJewel.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
                innerJewel.GetComponent<Renderer>().sharedMaterial = materials["GateTrim"];
                Object.DestroyImmediate(innerJewel.GetComponent<Collider>());

                // Cyan Aura Light
                var auraLight = relicObj.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.color = new Color(0f, 0.95f, 1f);
                auraLight.range = 6.0f;
                auraLight.intensity = 2.8f;

                // Particle System (Glow and Sparks)
                var pSystemObj = new GameObject("Relic_Sparks_ParticleSystem");
                pSystemObj.transform.parent = relicObj.transform;
                pSystemObj.transform.localPosition = Vector3.zero;
                var ps = pSystemObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1.6f;
                main.startSpeed = 0.8f;
                main.startSize = 0.12f;
                main.startColor = new Color(0f, 0.95f, 1f, 0.9f);
                main.maxParticles = 60;
                var emission = ps.emission;
                emission.rateOverTime = 25;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.65f;

                // AudioSource with relic pickup sound
                var audioSrc = relicObj.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0.5f;
                audioSrc.clip = pickupClip;

                // Trigger Collider
                var col = relicObj.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.35f;

                // RelicItem Script
                var relicItem = relicObj.AddComponent<RelicItem>();
                var sRelic = new SerializedObject(relicItem);
                sRelic.FindProperty("relicId").intValue = relicIndex;
                sRelic.FindProperty("auraLight").objectReferenceValue = auraLight;
                sRelic.FindProperty("ambientSparkParticles").objectReferenceValue = ps;
                sRelic.FindProperty("meshRenderer").objectReferenceValue = orb.GetComponent<Renderer>();
                sRelic.FindProperty("audioSource").objectReferenceValue = audioSrc;
                sRelic.ApplyModifiedProperties();

                // Sinusoidal Bobbing and Spinning
                relicObj.AddComponent<RelicBobAndSpin>();

                relics.Add(relicItem);
            }

            return relics;
        }

        private static List<Transform> BuildPatrolWaypoints(ProceduralTerrainGenerator terrainGen)
        {
            var waypointsRoot = new GameObject("Guardian_Patrol_Waypoints");
            var waypoints = new List<Transform>();

            Vector2[] points = new Vector2[]
            {
                new Vector2(-15f, 15f),
                new Vector2(18f, 22f),
                new Vector2(20f, -12f),
                new Vector2(-18f, -15f)
            };

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 pt = points[i];
                float y = terrainGen.GetWorldHeight(pt.x, pt.y) + 0.5f;
                var wp = new GameObject($"Waypoint_{i + 1}");
                wp.tag = "Waypoint";
                wp.transform.parent = waypointsRoot.transform;
                wp.transform.position = new Vector3(pt.x, y, pt.y);
                waypoints.Add(wp.transform);
            }

            return waypoints;
        }

        private static (GameObject, PlayerController, MultiAimConstraint, Transform) BuildPlayer(
            GameSettings settings, Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            float spawnX = 0f;
            float spawnZ = -8f;
            float spawnY = terrainGen.GetWorldHeight(spawnX, spawnZ) + 1.2f;

            var playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(spawnX, spawnY, spawnZ);

            // Character Controller
            var cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.42f;
            cc.center = new Vector3(0, 0.9f, 0);

            // New Input System PlayerInput
            var playerInput = playerObj.AddComponent<PlayerInput>();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = "Player";
            }

            // Visual Model Hierarchy
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.parent = playerObj.transform;
            modelRoot.transform.localPosition = Vector3.zero;

            // Torso
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Player_Torso";
            torso.transform.parent = modelRoot.transform;
            torso.transform.localPosition = new Vector3(0, 0.9f, 0);
            torso.transform.localScale = new Vector3(0.6f, 0.6f, 0.5f);
            torso.GetComponent<Renderer>().sharedMaterial = materials["PlayerBody"];
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            // Head / Visor bone (constrained by MultiAimConstraint)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Player_Head";
            head.transform.parent = torso.transform;
            head.transform.localPosition = new Vector3(0, 0.72f, 0.05f);
            head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            head.GetComponent<Renderer>().sharedMaterial = materials["PlayerBody"];
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Player_Visor";
            visor.transform.parent = head.transform;
            visor.transform.localPosition = new Vector3(0, 0.05f, 0.38f);
            visor.transform.localScale = new Vector3(0.65f, 0.28f, 0.2f);
            visor.GetComponent<Renderer>().sharedMaterial = materials["PlayerVisor"];
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            // Animation Rigging Setup
            var rigBuilder = playerObj.AddComponent<RigBuilder>();
            var rigObj = new GameObject("PlayerRig");
            rigObj.transform.parent = playerObj.transform;
            var rig = rigObj.AddComponent<Rig>();

            var aimObj = new GameObject("HeadAimConstraint");
            aimObj.transform.parent = rigObj.transform;
            var multiAim = aimObj.AddComponent<MultiAimConstraint>();

            var aimTarget = new GameObject("PlayerRig_AimTarget").transform;
            aimTarget.position = playerObj.transform.position + playerObj.transform.forward * 6f + Vector3.up * 1.5f;

            // Configure MultiAimConstraint data
            multiAim.data.constrainedObject = head.transform;
            multiAim.data.sourceObjects = new WeightedTransformArray { new WeightedTransform(aimTarget, 1.0f) };
            multiAim.data.aimAxis = MultiAimConstraintData.Axis.Z;
            multiAim.data.upAxis = MultiAimConstraintData.Axis.Y;
            multiAim.data.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            multiAim.weight = 1.0f;

            rigBuilder.layers.Clear();
            rigBuilder.layers.Add(new RigLayer(rig));

            // RelicLookAtTarget script
            var lookAtScript = playerObj.AddComponent<RelicLookAtTarget>();
            var sLookAt = new SerializedObject(lookAtScript);
            sLookAt.FindProperty("settings").objectReferenceValue = settings;
            sLookAt.FindProperty("multiAimConstraint").objectReferenceValue = multiAim;
            sLookAt.FindProperty("aimTarget").objectReferenceValue = aimTarget;
            sLookAt.ApplyModifiedProperties();

            // Audio sources for player
            AudioClip footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/player_footstep.ogg");
            AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/player_jump.ogg");
            AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/player_land.ogg");

            var footstepSrc = playerObj.AddComponent<AudioSource>();
            footstepSrc.clip = footstepClip;
            footstepSrc.loop = true;
            footstepSrc.playOnAwake = false;
            footstepSrc.volume = 0.35f;

            var jumpSrc = playerObj.AddComponent<AudioSource>();
            jumpSrc.clip = jumpClip;
            jumpSrc.playOnAwake = false;
            jumpSrc.volume = 0.5f;

            var landSrc = playerObj.AddComponent<AudioSource>();
            landSrc.clip = landClip;
            landSrc.playOnAwake = false;
            landSrc.volume = 0.5f;

            // PlayerController script
            var pCtrl = playerObj.AddComponent<PlayerController>();
            var sCtrl = new SerializedObject(pCtrl);
            sCtrl.FindProperty("settings").objectReferenceValue = settings;
            sCtrl.FindProperty("modelRoot").objectReferenceValue = modelRoot.transform;
            sCtrl.FindProperty("footstepAudio").objectReferenceValue = footstepSrc;
            sCtrl.FindProperty("jumpAudio").objectReferenceValue = jumpSrc;
            sCtrl.FindProperty("landAudio").objectReferenceValue = landSrc;
            sCtrl.ApplyModifiedProperties();

            // World-Space Canvas Relic Counter
            BuildWorldSpaceUI(playerObj.transform);

            return (playerObj, pCtrl, multiAim, aimTarget);
        }

        private static void BuildWorldSpaceUI(Transform player)
        {
            var canvasObj = new GameObject("Player_WorldSpace_UI");
            canvasObj.transform.parent = player;
            canvasObj.transform.localPosition = new Vector3(0, 2.25f, 0);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 60);
            rect.localScale = new Vector3(0.008f, 0.008f, 0.008f);

            canvasObj.AddComponent<Billboard>();

            // Text
            var textObj = new GameObject("RelicCounterText");
            textObj.transform.parent = canvasObj.transform;
            textObj.transform.localPosition = Vector3.zero;
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 60);
            textRect.anchoredPosition = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "RELICS: 0 / 3";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.95f, 1f, 1f);

            var worldUI = canvasObj.AddComponent<PlayerWorldUI>();
            var sWorldUI = new SerializedObject(worldUI);
            sWorldUI.FindProperty("relicCounterText").objectReferenceValue = tmp;
            sWorldUI.FindProperty("scalePunchTarget").objectReferenceValue = canvasObj.transform;
            sWorldUI.ApplyModifiedProperties();
        }

        private static EnemyBTController BuildEnemyGuardian(
            GameSettings settings, Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen,
            Transform player, List<Transform> waypoints)
        {
            float spawnX = 12f;
            float spawnZ = 12f;
            float spawnY = terrainGen.GetWorldHeight(spawnX, spawnZ) + 1.2f;

            var enemyObj = new GameObject("Enemy_Guardian");
            enemyObj.tag = "Enemy";
            enemyObj.transform.position = new Vector3(spawnX, spawnY, spawnZ);

            // NavMeshAgent
            var agent = enemyObj.AddComponent<NavMeshAgent>();
            agent.speed = settings.GuardianPatrolSpeed;
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.8f;
            agent.radius = 0.6f;
            agent.height = 2.2f;

            // Visual Intimidating Golem Model
            var model = new GameObject("Guardian_Model");
            model.transform.parent = enemyObj.transform;
            model.transform.localPosition = Vector3.zero;

            // Massive Torso
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Golem_Torso";
            torso.transform.parent = model.transform;
            torso.transform.localPosition = new Vector3(0, 1.3f, 0);
            torso.transform.localScale = new Vector3(1.2f, 1.2f, 0.9f);
            torso.GetComponent<Renderer>().sharedMaterial = materials["GuardianArmor"];
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            // Horned Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Golem_Head";
            head.transform.parent = torso.transform;
            head.transform.localPosition = new Vector3(0, 0.75f, 0.1f);
            head.transform.localScale = new Vector3(0.7f, 0.65f, 0.7f);
            head.GetComponent<Renderer>().sharedMaterial = materials["GuardianArmor"];
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Glowing Eye Visor
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Golem_Visor";
            visor.transform.parent = head.transform;
            visor.transform.localPosition = new Vector3(0, 0.05f, 0.45f);
            visor.transform.localScale = new Vector3(0.8f, 0.22f, 0.2f);
            visor.GetComponent<Renderer>().sharedMaterial = materials["GuardianVisor"];
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            // Eye Light
            var eyeLight = enemyObj.AddComponent<Light>();
            eyeLight.type = LightType.Spot;
            eyeLight.color = new Color(0.9f, 0.05f, 0.8f);
            eyeLight.range = 10f;
            eyeLight.spotAngle = 60f;
            eyeLight.intensity = 3.5f;

            // EnemyBTController
            var btController = enemyObj.AddComponent<EnemyBTController>();
            var sBT = new SerializedObject(btController);
            sBT.FindProperty("settings").objectReferenceValue = settings;
            sBT.FindProperty("playerTransform").objectReferenceValue = player;
            sBT.FindProperty("eyeVisorLight").objectReferenceValue = eyeLight;

            var waypointsProp = sBT.FindProperty("waypoints");
            waypointsProp.ClearArray();
            for (int i = 0; i < waypoints.Count; i++)
            {
                waypointsProp.InsertArrayElementAtIndex(i);
                waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            sBT.ApplyModifiedProperties();

            return btController;
        }

        private static void BuildRuinsAndObstacles(Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            var ruinsRoot = new GameObject("Ancient_Ruins_Obstacles");

            var colPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/statue_column.glb");
            var colDamagedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/statue_columnDamaged.glb");
            var obeliskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/statue_obelisk.glb");

            Vector3[] pillarPositions = new Vector3[]
            {
                new Vector3(-12f, 0, 8f),
                new Vector3(-8f, 0, 14f),
                new Vector3(14f, 0, -6f),
                new Vector3(8f, 0, -14f),
                new Vector3(-20f, 0, -8f)
            };

            for (int i = 0; i < pillarPositions.Length; i++)
            {
                Vector3 pt = pillarPositions[i];
                float y = terrainGen.GetWorldHeight(pt.x, pt.z);

                GameObject pillarObj;
                GameObject prefab = (i % 3 == 0) ? obeliskPrefab : ((i % 2 == 0) ? colDamagedPrefab : colPrefab);
                if (prefab != null)
                {
                    pillarObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ruinsRoot.transform);
                    pillarObj.name = $"Ancient_Ruin_{i + 1}";
                    pillarObj.transform.position = new Vector3(pt.x, y, pt.z);
                    pillarObj.transform.localScale = Vector3.one * 1.8f;
                }
                else
                {
                    pillarObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillarObj.name = $"Ancient_Pillar_{i + 1}";
                    pillarObj.transform.parent = ruinsRoot.transform;
                    pillarObj.transform.position = new Vector3(pt.x, y + 2.5f, pt.z);
                    pillarObj.transform.localScale = new Vector3(1.4f, 2.5f, 1.4f);
                    pillarObj.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];
                }

                // Dynamic NavMeshObstacle with Carve = true (Requirement #11!)
                var obs = pillarObj.GetComponent<NavMeshObstacle>();
                if (obs == null) obs = pillarObj.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 1.1f;
                obs.height = 5.5f;

                // Ensure collider
                if (pillarObj.GetComponent<Collider>() == null)
                {
                    var boxCol = pillarObj.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(1.8f, 5.0f, 1.8f);
                    boxCol.center = new Vector3(0, 2.5f, 0);
                }
            }
        }

        private static void BuildEnvironmentFoliageAndProps(ProceduralTerrainGenerator terrainGen)
        {
            var natureRoot = new GameObject("Environment_Nature_And_Props");
            natureRoot.transform.parent = GameObject.Find("Environment")?.transform;

            // Load downloaded 3D models
            var pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_pineDefaultA.glb");
            var oakPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_oak.glb");
            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_largeA.glb");
            var tallRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_tallA.glb");
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bush.glb");
            var purpleFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/flower_purpleA.glb");
            var yellowFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/flower_yellowA.glb");
            var cloudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/cloud.glb");

            // Scatter trees on safe elevation plateau (avoiding center player spawn and exit gate)
            Vector2[] treeCoords = new Vector2[]
            {
                new Vector2(-18, -4), new Vector2(-22, -12), new Vector2(-14, -18),
                new Vector2(18, -4), new Vector2(22, -10), new Vector2(14, -20),
                new Vector2(-28, 8), new Vector2(-20, 24), new Vector2(-10, 28),
                new Vector2(28, 8), new Vector2(22, 22), new Vector2(12, 26),
                new Vector2(-32, -6), new Vector2(30, -16), new Vector2(6, -26)
            };

            for (int i = 0; i < treeCoords.Length; i++)
            {
                Vector2 coord = treeCoords[i];
                float y = terrainGen.GetWorldHeight(coord.x, coord.y);
                if (y < 2.0f) continue;

                GameObject prefab = (i % 3 == 0) ? oakPrefab : pinePrefab;
                if (prefab != null)
                {
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, natureRoot.transform);
                    tree.transform.position = new Vector3(coord.x, y, coord.y);
                    float scale = Random.Range(1.3f, 1.9f);
                    tree.transform.localScale = Vector3.one * scale;
                    tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    var col = tree.AddComponent<CapsuleCollider>();
                    col.center = new Vector3(0, 2f, 0);
                    col.radius = 0.5f;
                    col.height = 4.5f;
                }
            }

            // Scatter rocks and boulders
            Vector2[] rockCoords = new Vector2[]
            {
                new Vector2(-15, 6), new Vector2(16, 4), new Vector2(-8, -12),
                new Vector2(8, -8), new Vector2(-24, -20), new Vector2(20, -22),
                new Vector2(-4, 22), new Vector2(5, 20)
            };

            for (int i = 0; i < rockCoords.Length; i++)
            {
                Vector2 coord = rockCoords[i];
                float y = terrainGen.GetWorldHeight(coord.x, coord.y);
                GameObject prefab = (i % 2 == 0) ? rockPrefab : tallRockPrefab;
                if (prefab != null)
                {
                    var rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab, natureRoot.transform);
                    rock.transform.position = new Vector3(coord.x, y, coord.y);
                    rock.transform.localScale = Vector3.one * Random.Range(1.2f, 1.8f);
                    rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    rock.AddComponent<BoxCollider>();
                }
            }

            // Scatter bushes and colorful flowers around grassy plateaus
            Vector2[] flowerCoords = new Vector2[]
            {
                new Vector2(-5, -3), new Vector2(4, -4), new Vector2(-2, -6),
                new Vector2(-12, 4), new Vector2(10, 8), new Vector2(-18, 16),
                new Vector2(18, 14), new Vector2(-6, 12), new Vector2(7, 14)
            };

            for (int i = 0; i < flowerCoords.Length; i++)
            {
                Vector2 coord = flowerCoords[i];
                float y = terrainGen.GetWorldHeight(coord.x, coord.y);
                GameObject prefab = (i % 3 == 0) ? bushPrefab : ((i % 3 == 1) ? purpleFlowerPrefab : yellowFlowerPrefab);
                if (prefab != null)
                {
                    var flower = (GameObject)PrefabUtility.InstantiatePrefab(prefab, natureRoot.transform);
                    flower.transform.position = new Vector3(coord.x, y, coord.y);
                    flower.transform.localScale = Vector3.one * Random.Range(1.0f, 1.5f);
                    flower.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                }
            }

            // Floating fantasy clouds drifting around island perimeter
            if (cloudPrefab != null)
            {
                Vector3[] cloudOffsets = new Vector3[]
                {
                    new Vector3(-55, 4, -30),
                    new Vector3(50, 6, -20),
                    new Vector3(-40, 2, 45),
                    new Vector3(45, 8, 40),
                    new Vector3(0, -2, -55)
                };

                foreach (var cPos in cloudOffsets)
                {
                    var cloud = (GameObject)PrefabUtility.InstantiatePrefab(cloudPrefab, natureRoot.transform);
                    cloud.transform.position = cPos;
                    cloud.transform.localScale = new Vector3(6f, 3f, 4f);
                    cloud.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                }
            }
        }

        private static void BuildAudioAtmosphere()
        {
            var bgmObj = new GameObject("BGM_AudioManager");
            var audioSrc = bgmObj.AddComponent<AudioSource>();
            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ambient_bgm.ogg");
            if (bgmClip != null)
            {
                audioSrc.clip = bgmClip;
                audioSrc.loop = true;
                audioSrc.playOnAwake = true;
                audioSrc.volume = 0.32f;
                audioSrc.Play();
            }
        }

        private static (Camera, CinemachineCamera, CinemachineCamera) BuildCinemachineCameras(
            Transform player, Transform exitGate)
        {
            // Main Camera
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CinemachineBrain>();

            // CM Player Follow Camera
            var playerVCamObj = new GameObject("CM_PlayerCamera");
            var playerVCam = playerVCamObj.AddComponent<CinemachineCamera>();
            playerVCam.Follow = player;
            playerVCam.LookAt = player;
            playerVCam.Priority = 10;

            var follow = playerVCamObj.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0, 2.6f, -5.5f);

            var rotationComposer = playerVCamObj.AddComponent<CinemachineRotationComposer>();
            rotationComposer.TargetOffset = new Vector3(0, 1.4f, 0);

            // CM Gate Cinematic Camera
            var gateVCamObj = new GameObject("CM_GateCamera");
            var gateVCam = gateVCamObj.AddComponent<CinemachineCamera>();
            gateVCam.Priority = 0; // Inactive until Timeline plays
            gateVCam.transform.position = exitGate.position + exitGate.forward * 14f + Vector3.up * 4.5f;
            gateVCam.LookAt = exitGate;

            return (cam, playerVCam, gateVCam);
        }

        private static PlayableDirector BuildVictoryTimeline(
            CinemachineCamera playerCam, CinemachineCamera gateCam, ExitGate gate)
        {
            var timelineObj = new GameObject("Victory_Timeline_Director");
            var director = timelineObj.AddComponent<PlayableDirector>();
            director.playOnAwake = false;

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
                timeline.fixedDuration = 5.0; // 5-second victory sequence!

                // Add Cinemachine Track
                var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cinemachine Track");

                // Shot 1: Player Camera (0s to 2s)
                var shot1 = cmTrack.CreateClip<CinemachineShot>();
                shot1.start = 0;
                shot1.duration = 2.0;
                var shot1Behavior = shot1.asset as CinemachineShot;

                // Shot 2: Gate Camera with smooth blend (1.5s to 5s)
                var shot2 = cmTrack.CreateClip<CinemachineShot>();
                shot2.start = 1.5;
                shot2.duration = 3.5;
                shot2.blendInDuration = 1.0;
                var shot2Behavior = shot2.asset as CinemachineShot;

                AssetDatabase.CreateAsset(timeline, TimelinePath);
                AssetDatabase.SaveAssets();
            }

            director.playableAsset = timeline;
            var sDirector = new SerializedObject(director);
            var assetProp = sDirector.FindProperty("m_PlayableAsset");
            if (assetProp != null) assetProp.objectReferenceValue = timeline;
            sDirector.ApplyModifiedProperties();
            EditorUtility.SetDirty(director);

            // Bind CinemachineBrain to CinemachineTrack
            var mainBrain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : Object.FindFirstObjectByType<CinemachineBrain>();
            if (mainBrain != null && timeline.GetOutputTracks() != null)
            {
                foreach (var track in timeline.GetOutputTracks())
                {
                    if (track is CinemachineTrack)
                    {
                        director.SetGenericBinding(track, mainBrain);
                    }
                }
            }

            return director;
        }

        private static void BuildScreenUI()
        {
            var canvasObj = new GameObject("ScreenSpace_HUD_Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventObj.AddComponent<InputSystemUIInputModule>();
            }

            // HUD Root
            var hudRoot = new GameObject("HUD_Root");
            hudRoot.transform.parent = canvasObj.transform;
            var hudRect = hudRoot.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;

            // Top Banner
            var bannerObj = new GameObject("Top_Relic_Banner");
            bannerObj.transform.parent = hudRoot.transform;
            var bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1.0f);
            bannerRect.anchorMax = new Vector2(0.5f, 1.0f);
            bannerRect.pivot = new Vector2(0.5f, 1.0f);
            bannerRect.anchoredPosition = new Vector2(0, -25);
            bannerRect.sizeDelta = new Vector2(500, 75);

            var bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
            bannerText.text = "RELICS: 0 / 3";
            bannerText.fontSize = 32;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.color = new Color(0.2f, 0.95f, 1f);

            // Objective Text
            var objTextObj = new GameObject("Objective_Text");
            objTextObj.transform.parent = hudRoot.transform;
            var objRect = objTextObj.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 1.0f);
            objRect.anchorMax = new Vector2(0.5f, 1.0f);
            objRect.pivot = new Vector2(0.5f, 1.0f);
            objRect.anchoredPosition = new Vector2(0, -85);
            objRect.sizeDelta = new Vector2(700, 40);

            var objTMP = objTextObj.AddComponent<TextMeshProUGUI>();
            objTMP.text = "Explore the lost floating island and recover all 3 ancient energy relics.";
            objTMP.fontSize = 18;
            objTMP.alignment = TextAlignmentOptions.Center;
            objTMP.color = new Color(0.85f, 0.9f, 0.95f);

            // Letterbox Bars
            var letterbox = new GameObject("Letterbox_Cinematic_Bars");
            letterbox.transform.parent = canvasObj.transform;
            var lbRect = letterbox.AddComponent<RectTransform>();
            lbRect.anchorMin = Vector2.zero;
            lbRect.anchorMax = Vector2.one;
            lbRect.sizeDelta = Vector2.zero;

            var barTop = new GameObject("Bar_Top");
            barTop.transform.parent = letterbox.transform;
            var topImg = barTop.AddComponent<Image>();
            topImg.color = Color.black;
            var tRect = barTop.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 1);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.sizeDelta = new Vector2(0, 80);

            var barBottom = new GameObject("Bar_Bottom");
            barBottom.transform.parent = letterbox.transform;
            var btmImg = barBottom.AddComponent<Image>();
            btmImg.color = Color.black;
            var bRect = barBottom.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 0);
            bRect.sizeDelta = new Vector2(0, 80);

            letterbox.SetActive(false);

            // Game Over Panel
            var gameOverPanel = CreatePanel(canvasObj.transform, "GameOver_Panel", new Color(0.1f, 0.02f, 0.02f, 0.92f));
            var goTitle = CreateText(gameOverPanel.transform, "CAUGHT BY THE ISLAND GUARDIAN", 36, Color.red, new Vector2(0, 70));
            var (goRestart, goQuit) = CreateButtons(gameOverPanel.transform);
            gameOverPanel.SetActive(false);

            // Victory Panel
            var victoryPanel = CreatePanel(canvasObj.transform, "Victory_Panel", new Color(0.02f, 0.1f, 0.08f, 0.92f));
            var vicTitle = CreateText(victoryPanel.transform, "ANCIENT ISLE CONQUERED!\nEXIT GATE UNSEALED", 36, new Color(0.2f, 1f, 0.5f), new Vector2(0, 80));
            var statsText = CreateText(victoryPanel.transform, "Time: 0.0s | Relics: 3 / 3", 22, Color.white, new Vector2(0, 20));
            var (vicRestart, vicQuit) = CreateButtons(victoryPanel.transform);
            victoryPanel.SetActive(false);

            // GameUIManager
            var uiMgr = canvasObj.AddComponent<GameUIManager>();
            var sUI = new SerializedObject(uiMgr);
            sUI.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            sUI.FindProperty("hudRelicText").objectReferenceValue = bannerText;
            sUI.FindProperty("objectiveText").objectReferenceValue = objTMP;
            sUI.FindProperty("letterboxBars").objectReferenceValue = letterbox;
            sUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            sUI.FindProperty("restartButtonGameOver").objectReferenceValue = goRestart;
            sUI.FindProperty("quitButtonGameOver").objectReferenceValue = goQuit;
            sUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            sUI.FindProperty("victoryStatsText").objectReferenceValue = statsText;
            sUI.FindProperty("playAgainButtonVictory").objectReferenceValue = vicRestart;
            sUI.FindProperty("quitButtonVictory").objectReferenceValue = vicQuit;
            sUI.ApplyModifiedProperties();
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            var panel = new GameObject(name);
            panel.transform.parent = parent;
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650, 420);
            rect.anchoredPosition = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, Vector2 pos)
        {
            var textObj = new GameObject("Text");
            textObj.transform.parent = parent;
            var rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(600, 100);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static (Button, Button) CreateButtons(Transform parent)
        {
            // Restart / Action button
            var btn1Obj = new GameObject("Btn_Primary");
            btn1Obj.transform.parent = parent;
            var r1 = btn1Obj.AddComponent<RectTransform>();
            r1.anchoredPosition = new Vector2(-120, -110);
            r1.sizeDelta = new Vector2(190, 50);
            var img1 = btn1Obj.AddComponent<Image>();
            img1.color = new Color(0.2f, 0.6f, 0.9f);
            var b1 = btn1Obj.AddComponent<Button>();

            var t1 = CreateText(btn1Obj.transform, "PLAY AGAIN", 20, Color.white, Vector2.zero);
            t1.rectTransform.sizeDelta = r1.sizeDelta;

            // Quit button
            var btn2Obj = new GameObject("Btn_Quit");
            btn2Obj.transform.parent = parent;
            var r2 = btn2Obj.AddComponent<RectTransform>();
            r2.anchoredPosition = new Vector2(120, -110);
            r2.sizeDelta = new Vector2(190, 50);
            var img2 = btn2Obj.AddComponent<Image>();
            img2.color = new Color(0.4f, 0.4f, 0.4f);
            var b2 = btn2Obj.AddComponent<Button>();

            var t2 = CreateText(btn2Obj.transform, "QUIT", 20, Color.white, Vector2.zero);
            t2.rectTransform.sizeDelta = r2.sizeDelta;

            return (b1, b2);
        }

        private static void SetSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    found = true;
                    s.enabled = true;
                    break;
                }
            }
            if (!found)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
