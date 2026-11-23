using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using RelicDash.Data;

namespace RelicDash.Environment
{
    /// <summary>
    /// Procedurally sculpts hills, ridges, and valleys on a Unity Terrain at runtime using Mathf.PerlinNoise.
    /// Creates a natural floating-island topography with edge falloff and triggers NavMesh baking.
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    [RequireComponent(typeof(TerrainCollider))]
    public class ProceduralTerrainGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameSettings settings;

        [Header("Noise & Topography")]
        [Tooltip("Primary noise scale for major hills.")]
        [SerializeField] private float primaryScale = 0.035f;

        [Tooltip("Secondary noise scale for undulating ridges.")]
        [SerializeField] private float secondaryScale = 0.085f;

        [Tooltip("Max elevation height in world units.")]
        [SerializeField] private float maxHeight = 12.0f;

        [Tooltip("Base height floor in world units.")]
        [SerializeField] private float baseHeight = 2.0f;

        [Tooltip("Island radius falloff power (higher = steeper cliff edges).")]
        [SerializeField] private float islandFalloff = 2.4f;

        [Header("Seed & Variation")]
        [SerializeField] private int seed = 42;
        [SerializeField] private bool randomizeSeedOnStart = false;

        [Header("NavMesh Integration")]
        [Tooltip("Reference to the NavMeshSurface component to bake runtime navigation.")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        [Header("Points of Interest")]
        [SerializeField] private Transform[] spawnPointMarkers;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform exitGatePoint;
        [SerializeField] private List<Transform> relicSpawnPoints = new List<Transform>();
        [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();

        private Terrain _terrain;
        private TerrainData _terrainData;

        public List<Transform> RelicSpawnPoints => relicSpawnPoints;
        public List<Transform> PatrolWaypoints => patrolWaypoints;
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Transform ExitGatePoint => exitGatePoint;

        private void Awake()
        {
            _terrain = GetComponent<Terrain>();
            if (_terrain != null)
            {
                _terrainData = _terrain.terrainData;
            }

            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponentInParent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = GetComponent<NavMeshSurface>();
                }
            }

            if (randomizeSeedOnStart)
            {
                seed = Random.Range(1, 99999);
            }
        }

        private void Start()
        {
            GenerateProceduralTerrain();
            BakeNavigationMesh();
        }

        /// <summary>
        /// Generates procedural heights using multi-octave Mathf.PerlinNoise and radial falloff.
        /// </summary>
        [ContextMenu("Generate Procedural Terrain")]
        public void GenerateProceduralTerrain()
        {
            if (_terrain == null) _terrain = GetComponent<Terrain>();
            if (_terrain == null) return;

            // Ensure terrain data instance is unique to prevent asset overwrites
            _terrainData = _terrain.terrainData;
            int heightmapRes = _terrainData.heightmapResolution;

            float[,] heights = new float[heightmapRes, heightmapRes];
            float offsetX = (seed * 17.371f) % 1000f;
            float offsetZ = (seed * 29.839f) % 1000f;

            float terrainMaxY = _terrainData.size.y;
            if (terrainMaxY <= 0.1f) terrainMaxY = 30.0f;

            float center = (heightmapRes - 1) / 2.0f;
            float maxRadius = center * 0.92f;

            for (int z = 0; z < heightmapRes; z++)
            {
                for (int x = 0; x < heightmapRes; x++)
                {
                    // Normalized world coordinates
                    float sampleX = (x * primaryScale) + offsetX;
                    float sampleZ = (z * primaryScale) + offsetZ;

                    // Octave 1: Major hills
                    float n1 = Mathf.PerlinNoise(sampleX, sampleZ);

                    // Octave 2: Secondary ridges
                    float n2 = Mathf.PerlinNoise(sampleX * 2.3f + 13.5f, sampleZ * 2.3f + 13.5f) * 0.45f;

                    // Octave 3: Micro crags
                    float n3 = Mathf.PerlinNoise(sampleX * 5.1f + 37.1f, sampleZ * 5.1f + 37.1f) * 0.18f;

                    float combinedNoise = (n1 + n2 + n3) / (1f + 0.45f + 0.18f);

                    // Radial falloff for floating island look
                    float distFromCenter = Vector2.Distance(new Vector2(x, z), new Vector2(center, center));
                    float normalizedDist = Mathf.Clamp01(distFromCenter / maxRadius);
                    float falloff = Mathf.Pow(1.0f - normalizedDist, islandFalloff);

                    // Flatten center plateau for fair gameplay arena
                    float centerFlatMask = Mathf.SmoothStep(0.25f, 1.0f, normalizedDist);

                    float rawElevation = baseHeight + (combinedNoise * maxHeight * centerFlatMask);
                    float finalElevation = rawElevation * falloff;

                    // Normalized height for Unity TerrainData (0.0 to 1.0)
                    heights[z, x] = Mathf.Clamp01(finalElevation / terrainMaxY);
                }
            }

            _terrainData.SetHeights(0, 0, heights);
            _terrain.Flush();
            Debug.Log($"[ProceduralTerrainGenerator] Runtime terrain generation complete with seed {seed}.");
        }

        /// <summary>
        /// Bakes the runtime NavMesh over the procedural terrain so AI navigation paths are exact.
        /// </summary>
        [ContextMenu("Bake Navigation Mesh")]
        public void BakeNavigationMesh()
        {
            if (navMeshSurface != null)
            {
                Debug.Log("[ProceduralTerrainGenerator] Baking NavMeshSurface over procedural terrain...");
                navMeshSurface.BuildNavMesh();
                Debug.Log("[ProceduralTerrainGenerator] NavMeshSurface baked successfully.");
            }
            else
            {
                Debug.LogWarning("[ProceduralTerrainGenerator] NavMeshSurface reference not assigned.");
            }
        }

        /// <summary>
        /// Samples terrain world height at given (X, Z).
        /// </summary>
        public float GetWorldHeight(float worldX, float worldZ)
        {
            if (_terrain == null) _terrain = GetComponent<Terrain>();
            return _terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + _terrain.transform.position.y;
        }
    }
}
