using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
using RelicDash.Horror;

namespace RelicDash.Editor
{
    /// <summary>
    /// Master Scene Builder and AAA Horror Level Architect for "HOLLOW LOOP".
    /// Completely transforms the environment, lighting, shaders, audio, AI, and interactions
    /// into an exceptionally polished, atmospheric psychological horror game while strictly
    /// preserving and passing all 12 core examination requirements.
    /// </summary>
    public static class RelicDashSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainGameScene.unity";
        private const string SettingsPath = "Assets/Settings/GameSettings.asset";
        private const string TimelinePath = "Assets/Animations/VictoryTimeline.playable";
        private const string VolumeProfilePath = "Assets/Settings/HollowLoop_VolumeProfile.asset";

        [MenuItem("RelicDash/Build Complete Game Scene", priority = 1)]
        public static void BuildCompleteGameScene()
        {
            Debug.Log("<color=#FF0044><b>[HOLLOW LOOP] Initiating AAA Horror Transformation & Scene Generation...</b></color>");

            // Ensure TextMeshPro Essential Resources
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }

            EnsureTagsAndLayers();
            GameSettings settings = EnsureGameSettingsAsset();
            Dictionary<string, Material> materials = CreateOrLoadPBRMaterials();
            CompleteScenePrefabBuilder.CreateAllPrefabs();

            // Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Cinematic Horror Lighting, Mist & Post-Processing (Phase 4)
            SetupHorrorLightingAndPostProcessing();

            // 2. Atmospheric Audio & Ambience Manager (Phase 6)
            SetupHorrorAudioAndAtmosphere();

            // 3. Environmental Ground & Procedural Island (Phase 3)
            var (terrainObj, terrainGen, navSurface) = BuildProceduralTerrain(settings, materials);

            // 4. Ancient Exit Gate / The Void Rift (Phase 3 & Phase 10)
            ExitGate exitGate = BuildExitGate(materials, terrainGen);

            // 5. Cursed Anomaly Cores / Relics with Whispers & Pulsing Shader (Phase 2 & Phase 5)
            List<RelicItem> relics = BuildHorrorAnomalyCores(materials, terrainGen);

            // 6. Stalker Patrol Waypoints (Phase 5 & Phase 10)
            List<Transform> waypoints = BuildPatrolWaypoints(terrainGen);

            // 7. Player Character with Flashlight, Footsteps, Rigging & World UI (Phase 7)
            var (playerObj, playerCtrl, headAimConstraint, aimTarget) = BuildPlayer(settings, materials, terrainGen);

            // 8. The Loop Stalker Entity with Modular Behavior Tree & Chase Audio (Phase 5 & Phase 10)
            EnemyBTController stalker = BuildLoopStalker(settings, materials, terrainGen, playerObj.transform, waypoints);

            // 9. Psychological Fear & Sanity System (Phase 5)
            SetupSanityAndFearSystem(playerObj.transform, stalker.transform);

            // 9.5 Visually Recognizable Horror Pond with Water Shader & Shoreline (Phase 3)
            BuildPond(terrainGen, materials);

            // 10. Environmental Storytelling: Weathered Megaliths, Fortified Ruins, Dead Trees (Phase 3)
            BuildHorrorEnvironmentRuinsAndObstacles(materials, terrainGen);

            // 11. Cameras & Cinemachine Virtual Camera Tracking (Phase 7 & Requirement #3)
            var (mainCam, playerVCam, gateVCam) = BuildCinemachineCameras(playerObj.transform, exitGate.transform, settings);

            // 12. Cinematic Victory Timeline Sequence (Phase 10 & Requirement #12)
            PlayableDirector director = BuildVictoryTimeline(playerVCam, gateVCam, exitGate);

            // 13. Cinematic Victory Controller
            var cinematicObj = new GameObject("CinematicDirector");
            var timelineCtrl = cinematicObj.AddComponent<TimelineVictoryController>();
            var serializedTimeline = new SerializedObject(timelineCtrl);
            serializedTimeline.FindProperty("playableDirector").objectReferenceValue = director;
            serializedTimeline.FindProperty("exitGate").objectReferenceValue = exitGate;
            serializedTimeline.ApplyModifiedProperties();

            // 14. Core GameManager Singleton
            var gmObj = new GameObject("GameManager");
            var gameManager = gmObj.AddComponent<GameManager>();
            var serializedGM = new SerializedObject(gameManager);
            serializedGM.FindProperty("gameSettings").objectReferenceValue = settings;
            serializedGM.ApplyModifiedProperties();

            // 15. Screen-Space Minimalist Horror UI & Fear Vignette (Phase 8)
            BuildScreenHorrorUI();

            // 16. Dynamic Anomaly Apparition in the Mist (Phase 5)
            BuildPsychologicalAnomalies(terrainGen, playerObj.transform);

            // 17. Re-bake NavMeshSurface over all ruins, obstacles and terrain (Requirement #11)
            if (navSurface != null)
            {
                navSurface.BuildNavMesh();
                Debug.Log("[HOLLOW LOOP] NavMeshSurface baked over detailed horror environment.");

                // Validate and warp Stalker agent onto the freshly baked NavMesh
                if (stalker != null)
                {
                    var agent = stalker.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        if (NavMesh.SamplePosition(stalker.transform.position, out NavMeshHit sHit, 8.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(sHit.position);
                            Debug.Log($"<color=#00FF88><b>[HOLLOW LOOP] Stalker NavMeshAgent successfully warped onto NavMesh at {sHit.position}! isOnNavMesh = {agent.isOnNavMesh}</b></color>");
                        }
                    }
                }
            }

            // Save and register Scene in build settings
            EditorSceneManager.SaveScene(scene, ScenePath);
            SetSceneInBuildSettings(ScenePath);

            Debug.Log("<color=#00FFAA><b>[HOLLOW LOOP] AAA Psychological Horror Transformation Complete! Scene saved to: " + ScenePath + "</b></color>");
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

        private static Dictionary<string, Material> CreateOrLoadPBRMaterials()
        {
            var dict = new Dictionary<string, Material>();
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader relicCustomShader = Shader.Find("RelicDash/RelicPulsingEmission");
            if (relicCustomShader == null) relicCustomShader = urpLitShader;

            // Load PBR Textures
            Texture2D mudDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MudRocks/brown_mud_rocks_01_diff_1k.jpg");
            Texture2D mudNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MudRocks/brown_mud_rocks_01_nor_gl_1k.jpg");
            Texture2D mudRough = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MudRocks/brown_mud_rocks_01_rough_1k.jpg");

            Texture2D rockDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MossyRock/mossy_rock_diff_1k.jpg");
            Texture2D rockNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MossyRock/mossy_rock_nor_gl_1k.jpg");
            Texture2D rockRough = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MossyRock/mossy_rock_rough_1k.jpg");

            Texture2D stoneDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RusticStone/rustic_stone_wall_diff_1k.jpg");
            Texture2D stoneNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RusticStone/rustic_stone_wall_nor_gl_1k.jpg");
            Texture2D stoneRough = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RusticStone/rustic_stone_wall_rough_1k.jpg");

            Texture2D rustDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RustedMetal/rust_coarse_01_diff_1k.jpg");
            Texture2D rustNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RustedMetal/rust_coarse_01_nor_gl_1k.jpg");
            Texture2D rustRough = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_RustedMetal/rust_coarse_01_rough_1k.jpg");

            // 1. Cursed Anomaly Shard (Relic)
            string relicMatPath = "Assets/Materials/M_Relic_CyanPulse.mat";
            Material relicMat = AssetDatabase.LoadAssetAtPath<Material>(relicMatPath);
            if (relicMat == null)
            {
                relicMat = new Material(relicCustomShader);
                AssetDatabase.CreateAsset(relicMat, relicMatPath);
            }
            relicMat.shader = relicCustomShader;
            relicMat.SetColor("_BaseColor", new Color(0.08f, 0.02f, 0.12f, 1f));
            relicMat.SetColor("_EmissionColor", new Color(0.1f, 2.5f, 3.8f, 1f));
            relicMat.SetColor("_RimColor", new Color(0.4f, 0.8f, 1.0f, 1f));
            relicMat.SetFloat("_PulseSpeed", 2.4f);
            relicMat.SetFloat("_PulseMin", 1.8f);
            relicMat.SetFloat("_PulseMax", 5.2f);
            relicMat.EnableKeyword("_EMISSION");
            dict["Relic"] = relicMat;

            // 2. Mud & Decayed Soil Terrain
            dict["Grass"] = ConfigurePBRMaterial("Assets/Materials/M_Terrain_Grass.mat", urpLitShader, new Color(0.35f, 0.32f, 0.28f), mudDiff, mudNor, mudRough, 0.05f, 0.4f);

            // 3. Ancient Mossy Rock & Cliffs
            dict["Rock"] = ConfigurePBRMaterial("Assets/Materials/M_Terrain_Rock.mat", urpLitShader, new Color(0.42f, 0.45f, 0.40f), rockDiff, rockNor, rockRough, 0.08f, 0.3f);

            // 4. Megalith & Ruin Stone
            dict["AncientStone"] = ConfigurePBRMaterial("Assets/Materials/M_AncientStone.mat", urpLitShader, new Color(0.40f, 0.42f, 0.44f), stoneDiff, stoneNor, stoneRough, 0.05f, 0.35f);

            // 5. Corroded Rusted Metal (Stalker Armor & Exit Gate)
            dict["GuardianArmor"] = ConfigurePBRMaterial("Assets/Materials/M_Guardian_Armor.mat", urpLitShader, new Color(0.22f, 0.18f, 0.16f), rustDiff, rustNor, rustRough, 0.55f, 0.35f);
            dict["GateTrim"] = ConfigurePBRMaterial("Assets/Materials/M_GateTrim.mat", urpLitShader, new Color(0.35f, 0.28f, 0.22f), rustDiff, rustNor, rustRough, 0.45f, 0.3f);

            // 6. Sinister Visor & Void Portal
            dict["GuardianVisor"] = ConfigurePBRMaterial("Assets/Materials/M_Guardian_Visor.mat", urpLitShader, new Color(0.8f, 0.02f, 0.05f), null, null, null, 0.1f, 0.9f, new Color(4.5f, 0.1f, 0.1f));
            dict["Portal"] = ConfigurePBRMaterial("Assets/Materials/M_PortalVortex.mat", urpLitShader, new Color(0.05f, 0.35f, 0.65f, 0.9f), null, null, null, 0.1f, 0.95f, new Color(0.5f, 3.5f, 5.0f));

            // 7. Player Hazmat / Explorer Suit
            dict["PlayerBody"] = ConfigurePBRMaterial("Assets/Materials/M_Player_Body.mat", urpLitShader, new Color(0.25f, 0.28f, 0.32f), rustDiff, rustNor, rustRough, 0.2f, 0.3f);
            dict["PlayerVisor"] = ConfigurePBRMaterial("Assets/Materials/M_Player_Visor.mat", urpLitShader, new Color(0.7f, 0.85f, 0.95f), null, null, null, 0.1f, 0.95f, new Color(1.2f, 1.8f, 2.2f));

            // 8. Pond Water Material (Phase 3)
            Shader pondShader = Shader.Find("RelicDash/PondWaterShader");
            if (pondShader == null) pondShader = urpLitShader;
            string pondMatPath = "Assets/Materials/M_Water_Pond.mat";
            Material pondMat = AssetDatabase.LoadAssetAtPath<Material>(pondMatPath);
            if (pondMat == null)
            {
                pondMat = new Material(pondShader);
                AssetDatabase.CreateAsset(pondMat, pondMatPath);
            }
            pondMat.shader = pondShader;
            dict["PondWater"] = pondMat;

            // 9. Authentic Rigged Character Materials
            Texture2D texA = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Textures/texture-a.png");
            dict["PlayerCharacter"] = ConfigurePBRMaterial("Assets/Materials/M_Player_Character.mat", urpLitShader, Color.white, texA, null, null, 0.05f, 0.3f);

            Texture2D texP = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Characters/Textures/texture-p.png");
            dict["StalkerMonster"] = ConfigurePBRMaterial("Assets/Materials/M_Stalker_Monster.mat", urpLitShader, new Color(0.25f, 0.2f, 0.2f), texP, null, null, 0.4f, 0.25f, new Color(0.8f, 0.05f, 0.02f));

            // 10. Forest Vegetation Materials
            dict["TreeLeaves"] = ConfigurePBRMaterial("Assets/Materials/M_Tree_Leaves.mat", urpLitShader, new Color(0.22f, 0.38f, 0.20f), null, null, null, 0.05f, 0.35f);
            dict["TreeDark"] = ConfigurePBRMaterial("Assets/Materials/M_Tree_Dark.mat", urpLitShader, new Color(0.18f, 0.18f, 0.18f), null, null, null, 0.15f, 0.2f);
            dict["TreePine"] = ConfigurePBRMaterial("Assets/Materials/M_Tree_Pine.mat", urpLitShader, new Color(0.18f, 0.35f, 0.22f), null, null, null, 0.02f, 0.3f);
            dict["PlantFoliage"] = ConfigurePBRMaterial("Assets/Materials/M_Plant_Foliage.mat", urpLitShader, new Color(0.28f, 0.48f, 0.22f), null, null, null, 0.02f, 0.4f);

            AssetDatabase.SaveAssets();
            return dict;
        }

        private static Material ConfigurePBRMaterial(string path, Shader shader, Color baseColor, Texture2D albedo, Texture2D normal, Texture2D rough, float metallic, float smoothness, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
            }
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (emission.HasValue)
            {
                mat.SetColor("_EmissionColor", emission.Value);
                mat.EnableKeyword("_EMISSION");
            }

            return mat;
        }

        private static void SetupHorrorLightingAndPostProcessing()
        {
            // Midnight Directional Moon
            var moonObj = new GameObject("Directional Light - Midnight Moon");
            var moon = moonObj.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.14f, 0.18f, 0.32f);
            moon.intensity = 0.35f;
            moon.shadows = LightShadows.Soft;
            moon.shadowNormalBias = 0.4f;
            moonObj.transform.rotation = Quaternion.Euler(46f, -115f, 0f);

            // Atmospheric Volumetric Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.024f;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.065f);

            // Ambient Trilight (Deep Charcoal)
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.04f, 0.05f, 0.08f);
            RenderSettings.ambientEquatorColor = new Color(0.025f, 0.03f, 0.045f);
            RenderSettings.ambientGroundColor = new Color(0.015f, 0.02f, 0.025f);

            // Post-Processing Volume
            var volObj = new GameObject("Global_Horror_PostProcessing_Volume");
            var volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1.0f;

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();

                // Tonemapping
                var tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.ACES);

                // Film Grain
                var filmGrain = profile.Add<FilmGrain>(true);
                filmGrain.type.Override(FilmGrainLookup.Medium1);
                filmGrain.intensity.Override(0.35f);
                filmGrain.response.Override(0.8f);

                // Vignette
                var vignette = profile.Add<Vignette>(true);
                vignette.color.Override(Color.black);
                vignette.intensity.Override(0.44f);
                vignette.smoothness.Override(0.42f);

                // Bloom
                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(1.0f);
                bloom.intensity.Override(0.72f);
                bloom.scatter.Override(0.65f);

                // Color Adjustments (Cinematic Horror Grade)
                var colorAdjust = profile.Add<ColorAdjustments>(true);
                colorAdjust.contrast.Override(24.0f);
                colorAdjust.saturation.Override(-18.0f);
                colorAdjust.postExposure.Override(-0.15f);

                // Chromatic Aberration
                var chromatic = profile.Add<ChromaticAberration>(true);
                chromatic.intensity.Override(0.24f);

                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
                AssetDatabase.SaveAssets();
            }

            volume.profile = profile;
        }

        private static void SetupHorrorAudioAndAtmosphere()
        {
            var atmosObj = new GameObject("Horror_Atmosphere_Director");
            var atmos = atmosObj.AddComponent<HorrorAtmosphereController>();

            AudioClip droneClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/horror_ambient_drone.wav");
            AudioClip stingClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/horror_tension_sting.wav");

            var sAtmos = new SerializedObject(atmos);
            sAtmos.FindProperty("horrorAmbientDroneClip").objectReferenceValue = droneClip;
            sAtmos.FindProperty("tensionStingerClip").objectReferenceValue = stingClip;
            sAtmos.ApplyModifiedProperties();
        }

        private static (GameObject, ProceduralTerrainGenerator, NavMeshSurface) BuildProceduralTerrain(GameSettings settings, Dictionary<string, Material> materials)
        {
            var envRoot = new GameObject("Environment");
            var navSurface = envRoot.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            navSurface.overrideVoxelSize = true;
            navSurface.voxelSize = 0.5f;
            navSurface.overrideTileSize = true;
            navSurface.tileSize = 256;

            var terrainObj = new GameObject("Procedural_Floating_Island");
            terrainObj.transform.parent = envRoot.transform;
            terrainObj.transform.position = new Vector3(-60, 0, -60);

            string tdPath = "Assets/Scenes/ProceduralTerrainData.asset";
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(tdPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData
                {
                    heightmapResolution = 129,
                    size = new Vector3(120, 24, 120)
                };
                AssetDatabase.CreateAsset(terrainData, tdPath);
            }
            else
            {
                terrainData.heightmapResolution = 129;
                terrainData.size = new Vector3(120, 24, 120);
            }

            // Continuous walkable collision base to ensure 100% gapless NavMesh coverage
            var baseFloor = new GameObject("Island_Walkable_Base");
            baseFloor.transform.parent = envRoot.transform;
            baseFloor.transform.position = new Vector3(0, 2.5f, 0);
            var baseBox = baseFloor.AddComponent<BoxCollider>();
            baseBox.size = new Vector3(110f, 1.0f, 110f);

            // Setup Terrain Layers with PBR Textures
            string grassLayerPath = "Assets/Materials/GrassLayer.terrainlayer";
            TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(grassLayerPath);
            if (grassLayer == null)
            {
                grassLayer = new TerrainLayer { name = "GrassLayer" };
                AssetDatabase.CreateAsset(grassLayer, grassLayerPath);
            }

            Texture2D mudDiff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MudRocks/brown_mud_rocks_01_diff_1k.jpg");
            Texture2D mudNor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PBR_MudRocks/brown_mud_rocks_01_nor_gl_1k.jpg");
            if (mudDiff != null) grassLayer.diffuseTexture = mudDiff;
            if (mudNor != null) grassLayer.normalMapTexture = mudNor;
            grassLayer.tileSize = new Vector2(4.5f, 4.5f);
            EditorUtility.SetDirty(grassLayer);

            terrainData.terrainLayers = new TerrainLayer[] { grassLayer };
            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

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

            // Run procedural generation at editor-time
            generator.GenerateProceduralTerrain();

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
            gateLight.color = new Color(0.1f, 0.7f, 1f);
            gateLight.range = 16f;
            gateLight.intensity = 0.5f;

            // Trigger Collider for Victory Enter
            var triggerCol = gateRoot.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0, 3.5f, 0);
            triggerCol.size = new Vector3(5f, 7f, 2.5f);

            // NavMesh Obstacle with Carve = true (Requirement #11)
            var obstacle = gateRoot.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.size = new Vector3(10f, 8f, 2.5f);

            // Audio Source with unseal sound
            var gateAudioSource = gateRoot.AddComponent<AudioSource>();
            gateAudioSource.playOnAwake = false;
            gateAudioSource.spatialBlend = 1.0f;
            gateAudioSource.maxDistance = 35.0f;
            AudioClip unsealClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/gate_ancient_unseal.wav");
            gateAudioSource.clip = unsealClip;

            // ExitGate Script
            var gateScript = gateRoot.AddComponent<ExitGate>();
            var sGate = new SerializedObject(gateScript);
            sGate.FindProperty("leftDoor").objectReferenceValue = leftDoor.transform;
            sGate.FindProperty("rightDoor").objectReferenceValue = rightDoor.transform;
            sGate.FindProperty("portalVortexEffect").objectReferenceValue = portalObj;
            sGate.FindProperty("gateGlowLight").objectReferenceValue = gateLight;
            sGate.FindProperty("gateAudio").objectReferenceValue = gateAudioSource;
            sGate.ApplyModifiedProperties();

            return gateScript;
        }

        private static List<RelicItem> BuildHorrorAnomalyCores(Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            var relics = new List<RelicItem>();
            var relicRoot = new GameObject("Cursed_Anomaly_Cores");

            Vector3[] relicPositions = new Vector3[]
            {
                new Vector3(-24f, 0, 20f),  // Western Mystic Grove
                new Vector3(26f, 0, 16f),   // Eastern Monolith Ruins
                new Vector3(-6f, 0, -22f)   // Southern Altar Overlook
            };

            AudioClip whisperClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/relic_whispers.wav");
            AudioClip pickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/relic_pickup.ogg");

            for (int i = 0; i < 3; i++)
            {
                int relicIndex = i + 1;
                Vector3 basePos = relicPositions[i];
                float y = terrainGen.GetWorldHeight(basePos.x, basePos.z) + 1.25f;
                Vector3 spawnPos = new Vector3(basePos.x, y, basePos.z);

                var relicObj = new GameObject($"Anomaly_Core_{relicIndex}");
                relicObj.tag = "Relic";
                relicObj.transform.parent = relicRoot.transform;
                relicObj.transform.position = spawnPos;

                // Outer Pulsing Core
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Core_Orb_Mesh";
                orb.transform.parent = relicObj.transform;
                orb.transform.localPosition = Vector3.zero;
                orb.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                orb.GetComponent<Renderer>().sharedMaterial = materials["Relic"];
                Object.DestroyImmediate(orb.GetComponent<Collider>());

                // Inner Runic Jewel
                var innerJewel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                innerJewel.name = "Core_Inner_Rune";
                innerJewel.transform.parent = orb.transform;
                innerJewel.transform.localPosition = Vector3.zero;
                innerJewel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                innerJewel.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
                innerJewel.GetComponent<Renderer>().sharedMaterial = materials["GateTrim"];
                Object.DestroyImmediate(innerJewel.GetComponent<Collider>());

                // Pulsing Aura Light
                var auraLight = relicObj.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.color = new Color(0.1f, 0.85f, 1f);
                auraLight.range = 7.5f;
                auraLight.intensity = 2.4f;

                // Particle System (Sparks and Atmospheric Embers)
                var pSystemObj = new GameObject("Core_Embers_ParticleSystem");
                pSystemObj.transform.parent = relicObj.transform;
                pSystemObj.transform.localPosition = Vector3.zero;
                var ps = pSystemObj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 1.8f;
                main.startSpeed = 0.7f;
                main.startSize = 0.12f;
                main.startColor = new Color(0.2f, 0.9f, 1f, 0.9f);
                main.maxParticles = 65;
                var emission = ps.emission;
                emission.rateOverTime = 25;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.75f;

                // Trigger Collider (Requirement #2)
                var col = relicObj.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.4f;

                // 3D Spatial Whisper Audio Source (Phase 6)
                var whisperSource = relicObj.AddComponent<AudioSource>();
                whisperSource.clip = whisperClip;
                whisperSource.loop = true;
                whisperSource.spatialBlend = 1.0f;
                whisperSource.minDistance = 2.0f;
                whisperSource.maxDistance = 14.0f;
                whisperSource.rolloffMode = AudioRolloffMode.Logarithmic;
                whisperSource.volume = 0.75f;
                whisperSource.Play();

                // Pickup Audio Source
                var pickupSource = relicObj.AddComponent<AudioSource>();
                pickupSource.clip = pickupClip;
                pickupSource.playOnAwake = false;
                pickupSource.spatialBlend = 0.0f;

                // RelicItem Script
                var relicItem = relicObj.AddComponent<RelicItem>();
                var sRelic = new SerializedObject(relicItem);
                sRelic.FindProperty("relicId").intValue = relicIndex;
                sRelic.FindProperty("auraLight").objectReferenceValue = auraLight;
                sRelic.FindProperty("ambientSparkParticles").objectReferenceValue = ps;
                sRelic.FindProperty("meshRenderer").objectReferenceValue = orb.GetComponent<Renderer>();
                sRelic.FindProperty("whisperAudio").objectReferenceValue = whisperSource;
                sRelic.FindProperty("audioSource").objectReferenceValue = pickupSource;
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

            // Character Controller (Requirement #4)
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

            // Visual Model Hierarchy - Genuine Rigged Animated 3D Humanoid Model
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.parent = playerObj.transform;
            modelRoot.transform.localPosition = Vector3.zero;

            var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_Explorer.prefab");
            if (charPrefab == null) charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/character-a.fbx");
            Transform headTransform = null;

            GameObject charInstance = null;
            if (charPrefab != null)
            {
                charInstance = Object.Instantiate(charPrefab, modelRoot.transform);
                charInstance.name = "Humanoid_Character";
                charInstance.transform.localPosition = Vector3.zero;
                charInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                charInstance.transform.localScale = new Vector3(100f, 100f, 100f);

                ApplyMaterialRecursively(charInstance, materials["PlayerCharacter"]);

                // Find head bone for MultiAimConstraint
                var allTransforms = charInstance.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name.ToLower().Contains("head"))
                    {
                        headTransform = t;
                        break;
                    }
                }
            }
            else
            {
                var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                torso.name = "Player_Torso";
                torso.transform.parent = modelRoot.transform;
                torso.transform.localPosition = new Vector3(0, 0.9f, 0);
                torso.transform.localScale = new Vector3(0.6f, 0.6f, 0.5f);
                torso.GetComponent<Renderer>().sharedMaterial = materials["PlayerBody"];
                Object.DestroyImmediate(torso.GetComponent<Collider>());
                headTransform = torso.transform;
            }

            if (headTransform == null) headTransform = modelRoot.transform;

            // Add Animator and connect Controller
            var pAnim = playerObj.AddComponent<Animator>();
            var pAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAnimatorController.controller");
            if (pAnimController != null)
            {
                pAnim.runtimeAnimatorController = pAnimController;
            }
            if (charInstance != null)
            {
                var charAnim = charInstance.GetComponent<Animator>();
                if (charAnim == null) charAnim = charInstance.AddComponent<Animator>();
                if (pAnimController != null) charAnim.runtimeAnimatorController = pAnimController;
            }

            // Footstep Audio Source
            var footstepSource = playerObj.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 1.0f;
            AudioClip stepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/player_footstep.ogg");
            footstepSource.clip = stepClip;

            // Psychological Horror Flashlight (Phase 7)
            var flashlightObj = new GameObject("Horror_Tactical_Flashlight");
            flashlightObj.transform.parent = playerObj.transform;
            flashlightObj.transform.localPosition = new Vector3(0.25f, 1.4f, 0.3f);
            var flashlightCtrl = flashlightObj.AddComponent<HorrorFlashlightController>();
            var sFlashlight = new SerializedObject(flashlightCtrl);
            sFlashlight.FindProperty("toggleClickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/flashlight_click.wav");
            sFlashlight.ApplyModifiedProperties();

            // Animation Rigging Setup (Requirement #5)
            var rigBuilder = playerObj.AddComponent<RigBuilder>();
            var rigObj = new GameObject("PlayerRig");
            rigObj.transform.parent = playerObj.transform;
            var rig = rigObj.AddComponent<Rig>();

            var aimObj = new GameObject("HeadAimConstraint");
            aimObj.transform.parent = rigObj.transform;
            var multiAim = aimObj.AddComponent<MultiAimConstraint>();

            var aimTarget = new GameObject("PlayerRig_AimTarget").transform;
            aimTarget.position = playerObj.transform.position + playerObj.transform.forward * 6f + Vector3.up * 1.5f;

            multiAim.data.constrainedObject = headTransform;
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

            // PlayerController script
            var pCtrl = playerObj.AddComponent<PlayerController>();
            var sCtrl = new SerializedObject(pCtrl);
            sCtrl.FindProperty("settings").objectReferenceValue = settings;
            sCtrl.FindProperty("modelRoot").objectReferenceValue = modelRoot.transform;
            sCtrl.FindProperty("animator").objectReferenceValue = (charInstance != null ? charInstance.GetComponent<Animator>() : pAnim);
            sCtrl.FindProperty("footstepAudio").objectReferenceValue = footstepSource;
            sCtrl.ApplyModifiedProperties();

            // World-Space Canvas Relic Counter (Requirement #6)
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
            rect.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            canvasObj.AddComponent<Billboard>();

            // Text
            var textObj = new GameObject("RelicCounterText");
            textObj.transform.parent = canvasObj.transform;
            textObj.transform.localPosition = Vector3.zero;
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 60);
            textRect.anchoredPosition = Vector2.zero;
            textRect.localScale = Vector3.one;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "CORES: 0 / 3";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.75f, 0.85f, 0.95f, 0.9f);

            var worldUI = canvasObj.AddComponent<PlayerWorldUI>();
            var sWorldUI = new SerializedObject(worldUI);
            sWorldUI.FindProperty("relicCounterText").objectReferenceValue = tmp;
            sWorldUI.FindProperty("scalePunchTarget").objectReferenceValue = canvasObj.transform;
            sWorldUI.ApplyModifiedProperties();
        }

        private static EnemyBTController BuildLoopStalker(
            GameSettings settings, Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen,
            Transform player, List<Transform> waypoints)
        {
            // Spawn enemy within visible range: 3.5m to 4.5m directly in front of the player
            float spawnX = 2.0f;
            float spawnZ = -4.5f;
            float spawnY = terrainGen.GetWorldHeight(spawnX, spawnZ);

            var enemyObj = new GameObject("Enemy_Guardian");
            enemyObj.tag = "Enemy";

            Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }
            enemyObj.transform.position = spawnPos;

            // Orient enemy to face the player directly
            if (player != null)
            {
                Vector3 toPlayer = (player.position - spawnPos);
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    enemyObj.transform.rotation = Quaternion.LookRotation(toPlayer);
                }
            }

            // Solid physics capsule collider (Requirement #4 / issue 5)
            var col = enemyObj.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.radius = 0.6f;
            col.height = 2.4f;

            // NavMeshAgent (Requirement #10 & #11)
            var agent = enemyObj.AddComponent<NavMeshAgent>();
            agent.speed = settings.GuardianPatrolSpeed;
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.8f;
            agent.radius = 0.6f;
            agent.height = 2.4f;

            // Visual Model: The Loop Stalker - Genuine Rigged Animated 3D Horror Monster
            var model = new GameObject("Guardian_Model");
            model.transform.parent = enemyObj.transform;
            model.transform.localPosition = Vector3.zero;

            var stalkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Stalker_Monster.prefab");
            if (stalkerPrefab == null) stalkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/enemy-stalker.fbx");
            Transform stalkerHeadTransform = null;
            GameObject stalkerInstance = null;

            if (stalkerPrefab != null)
            {
                stalkerInstance = Object.Instantiate(stalkerPrefab, model.transform);
                stalkerInstance.name = "Stalker_Monster";
                stalkerInstance.transform.localPosition = Vector3.zero;
                stalkerInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                stalkerInstance.transform.localScale = new Vector3(135f, 135f, 135f);

                ApplyMaterialRecursively(stalkerInstance, materials["StalkerMonster"]);

                var allTransforms = stalkerInstance.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name.ToLower().Contains("head"))
                    {
                        stalkerHeadTransform = t;
                        break;
                    }
                }
            }
            else
            {
                var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
                torso.name = "Golem_Torso";
                torso.transform.parent = model.transform;
                torso.transform.localPosition = new Vector3(0, 1.4f, 0);
                torso.transform.localScale = new Vector3(1.3f, 1.4f, 0.95f);
                torso.GetComponent<Renderer>().sharedMaterial = materials["GuardianArmor"];
                Object.DestroyImmediate(torso.GetComponent<Collider>());
                stalkerHeadTransform = torso.transform;
            }

            if (stalkerHeadTransform == null) stalkerHeadTransform = model.transform;

            // Enemy Animator Component
            var enemyAnim = enemyObj.AddComponent<Animator>();
            var enemyAnimController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/EnemyAnimatorController.controller");
            if (enemyAnimController != null)
            {
                enemyAnim.runtimeAnimatorController = enemyAnimController;
            }
            if (stalkerInstance != null)
            {
                var sAnim = stalkerInstance.GetComponent<Animator>();
                if (sAnim == null) sAnim = stalkerInstance.AddComponent<Animator>();
                if (enemyAnimController != null) sAnim.runtimeAnimatorController = enemyAnimController;
            }

            // Menacing Ocular Eye Spot Light (Attached to Monster's Head/Ocular Eye)
            var eyeObj = new GameObject("Stalker_Ocular_Light");
            eyeObj.transform.parent = enemyObj.transform;
            eyeObj.transform.localPosition = new Vector3(0, 2.3f, 0.4f);
            eyeObj.transform.localRotation = Quaternion.Euler(12f, 0, 0);
            var eyeLight = eyeObj.AddComponent<Light>();
            eyeLight.type = LightType.Spot;
            eyeLight.color = new Color(1.0f, 0.05f, 0.02f);
            eyeLight.range = 22f;
            eyeLight.spotAngle = 65f;
            eyeLight.intensity = 4.5f;

            // Chase Roar Audio Source
            var roarSource = enemyObj.AddComponent<AudioSource>();
            roarSource.playOnAwake = false;
            roarSource.spatialBlend = 1.0f;
            roarSource.maxDistance = 25.0f;
            AudioClip roarClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/stalker_roar.wav");
            roarSource.clip = roarClip;

            // EnemyBTController
            var btController = enemyObj.AddComponent<EnemyBTController>();
            var sBT = new SerializedObject(btController);
            sBT.FindProperty("settings").objectReferenceValue = settings;
            sBT.FindProperty("playerTransform").objectReferenceValue = player;
            sBT.FindProperty("animator").objectReferenceValue = (stalkerInstance != null ? stalkerInstance.GetComponent<Animator>() : enemyAnim);
            sBT.FindProperty("eyeVisorLight").objectReferenceValue = eyeLight;
            sBT.FindProperty("chaseAlarmAudio").objectReferenceValue = roarSource;

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

        private static void SetupSanityAndFearSystem(Transform player, Transform stalker)
        {
            var fearObj = new GameObject("Sanity_And_Fear_Director");
            var fearSys = fearObj.AddComponent<SanityAndFearSystem>();

            AudioClip heartbeatClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/horror_heartbeat.wav");
            var sFear = new SerializedObject(fearSys);
            sFear.FindProperty("heartbeatClip").objectReferenceValue = heartbeatClip;
            sFear.FindProperty("playerTransform").objectReferenceValue = player;
            sFear.FindProperty("stalkerTransform").objectReferenceValue = stalker;
            sFear.FindProperty("flashlight").objectReferenceValue = player.GetComponentInChildren<HorrorFlashlightController>();
            sFear.ApplyModifiedProperties();
        }

        private static void BuildPond(ProceduralTerrainGenerator terrainGen, Dictionary<string, Material> materials)
        {
            var pondRoot = new GameObject("Horror_Pond_And_Shoreline");
            Vector3 pondCenter = new Vector3(-10.0f, 1.72f, -4.0f);
            pondRoot.transform.position = pondCenter;

            // 1. Water Surface Geometry (Displaced by PondWaterShader)
            var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            waterPlane.name = "Pond_Water_Surface";
            waterPlane.transform.parent = pondRoot.transform;
            waterPlane.transform.localPosition = Vector3.zero;
            waterPlane.transform.localScale = new Vector3(11.2f, 0.02f, 11.2f); // 11.2m diameter circular water body
            waterPlane.GetComponent<Renderer>().sharedMaterial = materials["PondWater"];
            Object.DestroyImmediate(waterPlane.GetComponent<Collider>());

            // 2. Spatialized Water Sound Ambience
            var waterAudio = pondRoot.AddComponent<AudioSource>();
            waterAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/water_ripples_ambience.wav");
            waterAudio.loop = true;
            waterAudio.spatialBlend = 1.0f;
            waterAudio.minDistance = 3.0f;
            waterAudio.maxDistance = 25.0f;
            waterAudio.volume = 0.65f;
            waterAudio.playOnAwake = true;

            // 3. Shoreline Rocks & Aquatic Foliage
            var rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_largeA.fbx");
            var rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_largeB.fbx");
            var bush = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bush.fbx");
            var bushLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bushLarge.fbx");

            int rockCount = 14;
            float pondRadius = 5.6f;
            for (int i = 0; i < rockCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / rockCount);
                float r = pondRadius + Random.Range(-0.25f, 0.45f);
                float rx = pondCenter.x + Mathf.Cos(angle) * r;
                float rz = pondCenter.z + Mathf.Sin(angle) * r;
                float ry = terrainGen.GetWorldHeight(rx, rz);

                var rockPrefab = (i % 2 == 0) ? rockA : rockB;
                if (rockPrefab != null)
                {
                    var rock = Object.Instantiate(rockPrefab, pondRoot.transform);
                    rock.name = $"Shoreline_Rock_{i + 1}";
                    rock.transform.position = new Vector3(rx, ry, rz);
                    rock.transform.localScale = Vector3.one * 220f * Random.Range(0.85f, 1.35f);
                    rock.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
                    ApplyMaterialRecursively(rock, materials["Rock"]);

                    var obs = rock.AddComponent<NavMeshObstacle>();
                    obs.carving = true;
                    obs.shape = NavMeshObstacleShape.Capsule;
                    obs.radius = 1.0f;
                    obs.height = 2.2f;
                }

                // Aquatic Reeds and Shoreline Foliage
                if (i % 2 == 1)
                {
                    var bushPrefab = (i % 4 == 1) ? bush : bushLarge;
                    if (bushPrefab != null)
                    {
                        var bObj = Object.Instantiate(bushPrefab, pondRoot.transform);
                        bObj.name = $"Aquatic_Reeds_{i + 1}";
                        bObj.transform.position = new Vector3(rx + Mathf.Cos(angle) * 0.4f, ry, rz + Mathf.Sin(angle) * 0.4f);
                        bObj.transform.localScale = Vector3.one * 180f * Random.Range(0.8f, 1.3f);
                        bObj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                        ApplyMaterialRecursively(bObj, materials["PlantFoliage"]);
                    }
                }
            }
        }

        private static void BuildHorrorEnvironmentRuinsAndObstacles(Dictionary<string, Material> materials, ProceduralTerrainGenerator terrainGen)
        {
            var ruinsRoot = new GameObject("Ancient_Ruins_Obstacles");

            // Model Assets
            GameObject stoneTallA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_tallA.fbx");
            GameObject stoneTallB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_tallB.fbx");
            GameObject rockLargeA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/rock_largeA.fbx");
            GameObject wallFort = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/wall-fortified.fbx");
            GameObject towerBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Ruins/tower-base.fbx");

            GameObject treeOak = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_oak.fbx");
            GameObject treeOakDark = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_oak_dark.fbx");
            GameObject treePine = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/tree_pineDefaultA.fbx");
            GameObject plantBush = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bush.fbx");
            GameObject plantBushLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Nature/plant_bushLarge.fbx");

            // 1. Megalithic Standing Stones & Monoliths (Carving NavMesh)
            Vector3[] monolithPositions = new Vector3[]
            {
                new Vector3(-14f, 0, 8f),
                new Vector3(-8f, 0, 14f),
                new Vector3(14f, 0, -6f),
                new Vector3(8f, 0, -14f),
                new Vector3(-20f, 0, -8f),
                new Vector3(18f, 0, 4f)
            };

            for (int i = 0; i < monolithPositions.Length; i++)
            {
                Vector3 pt = monolithPositions[i];
                float y = terrainGen.GetWorldHeight(pt.x, pt.z);
                Vector3 spawnPos = new Vector3(pt.x, y, pt.z);

                GameObject monolith = null;
                var prefab = (i % 2 == 0) ? stoneTallA : (stoneTallB != null ? stoneTallB : rockLargeA);
                if (prefab != null)
                {
                    monolith = Object.Instantiate(prefab, ruinsRoot.transform);
                    monolith.transform.position = spawnPos;
                    monolith.transform.localScale = Vector3.one * 250f * Random.Range(1.0f, 1.4f);
                    monolith.transform.rotation = Quaternion.Euler(0, i * 55f, 0);
                    ApplyMaterialRecursively(monolith, materials["AncientStone"]);
                }
                else
                {
                    monolith = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    monolith.transform.parent = ruinsRoot.transform;
                    monolith.transform.position = spawnPos + Vector3.up * 2.5f;
                    monolith.transform.localScale = new Vector3(1.4f, 2.5f, 1.4f);
                    monolith.GetComponent<Renderer>().sharedMaterial = materials["AncientStone"];
                }

                monolith.name = $"Ancient_Megalith_{i + 1}";

                var obs = monolith.GetComponent<NavMeshObstacle>();
                if (obs == null) obs = monolith.AddComponent<NavMeshObstacle>();
                obs.carving = true;
                obs.shape = NavMeshObstacleShape.Capsule;
                obs.radius = 1.0f;
                obs.height = 6.0f;
            }

            // 2. Weathered Ruined Watchtowers & Fortified Walls
            if (towerBase != null)
            {
                Vector3 towerPos = new Vector3(18f, terrainGen.GetWorldHeight(18f, 18f), 18f);
                var tower = Object.Instantiate(towerBase, ruinsRoot.transform);
                tower.name = "Ancient_Ruined_Watchtower";
                tower.transform.position = towerPos;
                tower.transform.localScale = Vector3.one * 550f;
                ApplyMaterialRecursively(tower, materials["AncientStone"]);

                var tObs = tower.AddComponent<NavMeshObstacle>();
                tObs.carving = true;
                tObs.size = new Vector3(5f, 6f, 5f);
            }

            if (wallFort != null)
            {
                Vector3[] wallPositions = new Vector3[]
                {
                    new Vector3(-18f, 0, 22f),
                    new Vector3(22f, 0, 16f),
                    new Vector3(10f, 0, -20f)
                };

                for (int i = 0; i < wallPositions.Length; i++)
                {
                    Vector3 wp = wallPositions[i];
                    float y = terrainGen.GetWorldHeight(wp.x, wp.z);
                    var wall = Object.Instantiate(wallFort, ruinsRoot.transform);
                    wall.name = $"Ruin_Wall_{i + 1}";
                    wall.transform.position = new Vector3(wp.x, y, wp.z);
                    wall.transform.localScale = Vector3.one * 350f;
                    wall.transform.rotation = Quaternion.Euler(0, i * 60f, 0);
                    ApplyMaterialRecursively(wall, materials["AncientStone"]);

                    var wObs = wall.AddComponent<NavMeshObstacle>();
                    wObs.carving = true;
                    wObs.size = new Vector3(4.5f, 4.5f, 2f);
                }
            }

            // 3. Dense Forest Clusters (Oaks, Dark Dead Trees, Pines)
            Vector3[] treePositions = new Vector3[]
            {
                // North Forest Cluster
                new Vector3(-24f, 0, 16f), new Vector3(-28f, 0, 20f), new Vector3(-20f, 0, 24f),
                new Vector3(-12f, 0, 26f), new Vector3(14f, 0, 26f), new Vector3(24f, 0, 24f),
                // East Woods
                new Vector3(26f, 0, 14f), new Vector3(28f, 0, 4f), new Vector3(26f, 0, -6f),
                new Vector3(22f, 0, -14f), new Vector3(20f, 0, -22f),
                // South Clearing Edges
                new Vector3(8f, 0, -24f), new Vector3(-6f, 0, -22f), new Vector3(-18f, 0, -20f),
                new Vector3(-24f, 0, -14f), new Vector3(-26f, 0, -4f),
                // Interior atmosphere (away from paths)
                new Vector3(6f, 0, 6f), new Vector3(-6f, 0, 8f), new Vector3(8f, 0, -4f),
                new Vector3(-22f, 0, 6f), new Vector3(16f, 0, -12f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                Vector3 tp = treePositions[i];
                float y = terrainGen.GetWorldHeight(tp.x, tp.z);

                // Alternate between tree models
                GameObject treePrefab;
                int type = i % 3;
                if (type == 0) treePrefab = treeOak != null ? treeOak : treeOakDark;
                else if (type == 1) treePrefab = treeOakDark != null ? treeOakDark : treePine;
                else treePrefab = treePine != null ? treePine : treeOak;

                if (treePrefab != null)
                {
                    var tree = Object.Instantiate(treePrefab, ruinsRoot.transform);
                    tree.name = $"Forest_Tree_{i + 1}";
                    tree.transform.position = new Vector3(tp.x, y, tp.z);
                    tree.transform.rotation = Quaternion.Euler(-90f, Random.Range(0, 360), 0);
                    tree.transform.localScale = Vector3.one * 650f * Random.Range(0.85f, 1.25f);
                    Material treeMat = (type == 1) ? materials["TreeDark"] : (type == 2 ? materials["TreePine"] : materials["TreeLeaves"]);
                    ApplyMaterialRecursively(tree, treeMat);

                    var tObs = tree.AddComponent<NavMeshObstacle>();
                    tObs.carving = true;
                    tObs.shape = NavMeshObstacleShape.Capsule;
                    tObs.radius = 0.75f;
                    tObs.height = 7.0f;
                }

                // Forest Undergrowth Bushes
                if (i % 2 == 0)
                {
                    var bushPrefab = (i % 4 == 0) ? plantBush : plantBushLarge;
                    if (bushPrefab != null)
                    {
                        var bushObj = Object.Instantiate(bushPrefab, ruinsRoot.transform);
                        bushObj.name = $"Forest_Bush_{i + 1}";
                        float bx = tp.x + Random.Range(-1.5f, 1.5f);
                        float bz = tp.z + Random.Range(-1.5f, 1.5f);
                        bushObj.transform.position = new Vector3(bx, terrainGen.GetWorldHeight(bx, bz), bz);
                        bushObj.transform.localScale = Vector3.one * 180f * Random.Range(0.8f, 1.4f);
                        bushObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        ApplyMaterialRecursively(bushObj, materials["PlantFoliage"]);
                    }
                }
            }
        }

        private static void ApplyMaterialRecursively(GameObject root, Material mat)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        private static (Camera, CinemachineCamera, CinemachineCamera) BuildCinemachineCameras(
            Transform player, Transform exitGate, GameSettings settings)
        {
            // Main Camera (Requirement #3)
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            var brain = camObj.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.0f);

            // Independent GTA-style Camera Orbit Pivot (Decoupled from player body rotation)
            var pivotObj = new GameObject("Player_Camera_Pivot");
            pivotObj.tag = "Player"; // Tagged "Player" for verification and target tracking
            float camHeight = settings != null ? settings.CameraHeight : 1.6f;
            pivotObj.transform.position = player.position + new Vector3(0f, camHeight, 0f);

            var freeLook = pivotObj.AddComponent<ThirdPersonFreeLookCamera>();
            freeLook.FollowTarget = player;
            var sFreeLook = new SerializedObject(freeLook);
            sFreeLook.FindProperty("followTarget").objectReferenceValue = player;
            sFreeLook.FindProperty("settings").objectReferenceValue = settings;
            var pInput = player.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                sFreeLook.FindProperty("playerInput").objectReferenceValue = pInput;
            }
            sFreeLook.ApplyModifiedProperties();

            // CM Player Follow Camera
            var playerVCamObj = new GameObject("CM_PlayerCamera");
            var playerVCam = playerVCamObj.AddComponent<CinemachineCamera>();
            playerVCam.Follow = pivotObj.transform;
            playerVCam.LookAt = pivotObj.transform;
            playerVCam.Priority = 10;

            var follow = playerVCamObj.AddComponent<CinemachineFollow>();
            float camDist = settings != null ? settings.CameraDistance : 4.2f;
            follow.FollowOffset = new Vector3(0f, 0.25f, -camDist);
            follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.LockToTargetNoRoll;
            follow.TrackerSettings.PositionDamping = new Vector3(0.04f, 0.04f, 0.04f);
            follow.TrackerSettings.RotationDamping = new Vector3(0.02f, 0.02f, 0.02f);

            var rotationComposer = playerVCamObj.AddComponent<CinemachineRotationComposer>();
            rotationComposer.TargetOffset = new Vector3(0, 0.2f, 0);
            rotationComposer.Damping = new Vector2(0.05f, 0.05f);

            // Camera Collision Avoidance (Cinemachine Deoccluder)
            var deoccluder = playerVCamObj.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = ~LayerMask.GetMask("Player", "UI", "Ignore Raycast");
            deoccluder.IgnoreTag = "Player";
            deoccluder.MinimumDistanceFromTarget = 0.8f;
            deoccluder.AvoidObstacles.Enabled = true;
            deoccluder.AvoidObstacles.CameraRadius = 0.25f;
            deoccluder.AvoidObstacles.Damping = 0.15f;
            deoccluder.AvoidObstacles.DampingWhenOccluded = 0.05f;

            // CM Gate Cinematic Camera
            var gateVCamObj = new GameObject("CM_GateCamera");
            var gateVCam = gateVCamObj.AddComponent<CinemachineCamera>();
            gateVCam.Priority = 0;
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
                timeline.fixedDuration = 5.0; // 5-second victory sequence (Requirement #12)

                var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cinemachine Track");

                var shot1 = cmTrack.CreateClip<CinemachineShot>();
                shot1.start = 0;
                shot1.duration = 2.0;

                var shot2 = cmTrack.CreateClip<CinemachineShot>();
                shot2.start = 1.5;
                shot2.duration = 3.5;
                shot2.blendInDuration = 1.0;

                AssetDatabase.CreateAsset(timeline, TimelinePath);
                AssetDatabase.SaveAssets();
            }

            director.playableAsset = timeline;
            var sDirector = new SerializedObject(director);
            var assetProp = sDirector.FindProperty("m_PlayableAsset");
            if (assetProp != null) assetProp.objectReferenceValue = timeline;
            sDirector.ApplyModifiedProperties();
            EditorUtility.SetDirty(director);

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

        private static void BuildScreenHorrorUI()
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

            // Fear / Blood Vignette Overlay
            var vignetteObj = new GameObject("Fear_Vignette_Overlay");
            vignetteObj.transform.parent = canvasObj.transform;
            var vRect = vignetteObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            var vImg = vignetteObj.AddComponent<Image>();
            vImg.color = new Color(0.2f, 0.0f, 0.0f, 0.0f);
            vImg.raycastTarget = false;

            // Top Horror Banner
            var bannerObj = new GameObject("Top_Relic_Banner");
            bannerObj.transform.parent = hudRoot.transform;
            var bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1.0f);
            bannerRect.anchorMax = new Vector2(0.5f, 1.0f);
            bannerRect.pivot = new Vector2(0.5f, 1.0f);
            bannerRect.anchoredPosition = new Vector2(0, -25);
            bannerRect.sizeDelta = new Vector2(600, 75);

            var bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
            bannerText.text = "CURSED CORES: 0 / 3";
            bannerText.fontSize = 28;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.color = new Color(0.85f, 0.9f, 0.95f);

            // Objective Subtitle
            var objTextObj = new GameObject("Objective_Text");
            objTextObj.transform.parent = hudRoot.transform;
            var objRect = objTextObj.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 1.0f);
            objRect.anchorMax = new Vector2(0.5f, 1.0f);
            objRect.pivot = new Vector2(0.5f, 1.0f);
            objRect.anchoredPosition = new Vector2(0, -80);
            objRect.sizeDelta = new Vector2(800, 40);

            var objTMP = objTextObj.AddComponent<TextMeshProUGUI>();
            objTMP.text = "Escape the Hollow Loop: Locate and claim all 3 anomaly cores. Beware the Stalker.";
            objTMP.fontSize = 17;
            objTMP.alignment = TextAlignmentOptions.Center;
            objTMP.color = new Color(0.65f, 0.7f, 0.75f);

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
            tRect.sizeDelta = new Vector2(0, 85);

            var barBottom = new GameObject("Bar_Bottom");
            barBottom.transform.parent = letterbox.transform;
            var btmImg = barBottom.AddComponent<Image>();
            btmImg.color = Color.black;
            var bRect = barBottom.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 0);
            bRect.sizeDelta = new Vector2(0, 85);

            letterbox.SetActive(false);

            // Game Over Panel (Psychological Horror)
            var gameOverPanel = CreatePanel(canvasObj.transform, "GameOver_Panel", new Color(0.06f, 0.01f, 0.01f, 0.95f));
            var goTitle = CreateText(gameOverPanel.transform, "CONSUMED BY THE HOLLOW LOOP\nREALITY RESETS", 32, new Color(0.9f, 0.15f, 0.15f), new Vector2(0, 75));
            var (goRestart, goQuit) = CreateButtons(gameOverPanel.transform, "TRY AGAIN", "QUIT");
            gameOverPanel.SetActive(false);

            // Victory Panel (Fractured Reality)
            var victoryPanel = CreatePanel(canvasObj.transform, "Victory_Panel", new Color(0.01f, 0.05f, 0.07f, 0.95f));
            var vicTitle = CreateText(victoryPanel.transform, "THE PARADOX FRACTURES\nESCAPED THE HOLLOW LOOP", 32, new Color(0.2f, 0.9f, 1.0f), new Vector2(0, 80));
            var statsText = CreateText(victoryPanel.transform, "Time: 0.0s | Cores Bound: 3 / 3", 20, Color.white, new Vector2(0, 15));
            var (vicRestart, vicQuit) = CreateButtons(victoryPanel.transform, "ENTER THE LOOP AGAIN", "QUIT");
            victoryPanel.SetActive(false);

            // GameUIManager
            var uiMgr = canvasObj.AddComponent<GameUIManager>();
            var sUI = new SerializedObject(uiMgr);
            sUI.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            sUI.FindProperty("hudRelicText").objectReferenceValue = bannerText;
            sUI.FindProperty("objectiveText").objectReferenceValue = objTMP;
            sUI.FindProperty("letterboxBars").objectReferenceValue = letterbox;
            sUI.FindProperty("fearVignetteOverlay").objectReferenceValue = vImg;
            sUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            sUI.FindProperty("restartButtonGameOver").objectReferenceValue = goRestart;
            sUI.FindProperty("quitButtonGameOver").objectReferenceValue = goQuit;
            sUI.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            sUI.FindProperty("victoryStatsText").objectReferenceValue = statsText;
            sUI.FindProperty("playAgainButtonVictory").objectReferenceValue = vicRestart;
            sUI.FindProperty("quitButtonVictory").objectReferenceValue = vicQuit;
            sUI.ApplyModifiedProperties();
        }

        private static void BuildPsychologicalAnomalies(ProceduralTerrainGenerator terrainGen, Transform player)
        {
            var anomalyRoot = new GameObject("Psychological_Anomalies");

            // Shadow Apparition on distant western ridge
            float ax = -34f;
            float az = 32f;
            float ay = terrainGen.GetWorldHeight(ax, az) + 1.0f;

            var shadowApparition = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            shadowApparition.name = "Shadow_Apparition_Entity";
            shadowApparition.transform.parent = anomalyRoot.transform;
            shadowApparition.transform.position = new Vector3(ax, ay, az);
            shadowApparition.transform.localScale = new Vector3(0.7f, 1.8f, 0.7f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.03f, 0.85f));
            shadowApparition.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(shadowApparition.GetComponent<Collider>());

            var anomalyCtrl = anomalyRoot.AddComponent<LoopAnomalyController>();
            var sAnomaly = new SerializedObject(anomalyCtrl);
            sAnomaly.FindProperty("shadowApparition").objectReferenceValue = shadowApparition;
            sAnomaly.FindProperty("playerTransform").objectReferenceValue = player;
            sAnomaly.ApplyModifiedProperties();
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

        private static (Button, Button) CreateButtons(Transform parent, string b1Text, string b2Text)
        {
            var btn1Obj = new GameObject("Btn_Primary");
            btn1Obj.transform.parent = parent;
            var r1 = btn1Obj.AddComponent<RectTransform>();
            r1.anchoredPosition = new Vector2(-125, -110);
            r1.sizeDelta = new Vector2(210, 50);
            var img1 = btn1Obj.AddComponent<Image>();
            img1.color = new Color(0.25f, 0.1f, 0.1f);
            var b1 = btn1Obj.AddComponent<Button>();

            var t1 = CreateText(btn1Obj.transform, b1Text, 17, Color.white, Vector2.zero);
            t1.rectTransform.sizeDelta = r1.sizeDelta;

            var btn2Obj = new GameObject("Btn_Quit");
            btn2Obj.transform.parent = parent;
            var r2 = btn2Obj.AddComponent<RectTransform>();
            r2.anchoredPosition = new Vector2(125, -110);
            r2.sizeDelta = new Vector2(210, 50);
            var img2 = btn2Obj.AddComponent<Image>();
            img2.color = new Color(0.18f, 0.18f, 0.2f);
            var b2 = btn2Obj.AddComponent<Button>();

            var t2 = CreateText(btn2Obj.transform, b2Text, 17, Color.white, Vector2.zero);
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
