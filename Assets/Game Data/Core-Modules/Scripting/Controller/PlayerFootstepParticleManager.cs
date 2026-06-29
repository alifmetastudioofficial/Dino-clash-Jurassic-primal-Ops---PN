using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player-only footstep particles.
/// - Uses Terrain alphamaps to detect which terrain layer/texture is under the footstep hit point.
/// - Spawns pooled ParticleSystem prefabs (one pool per terrain layer; optional water pool).
/// - Intended to be triggered from creature scripts when they play "Step" sounds (same frame timing).
/// </summary>
public class PlayerFootstepParticleManager : MonoBehaviour
{
    [System.Serializable]
    public class TerrainLayerToParticles
    {
        [Tooltip("This must match a layer used by the Terrain component (TerrainData terrainLayers).")]
        public TerrainLayer terrainLayer;

        [Tooltip("Particle prefab spawned when this TerrainLayer is dominant under the hit point.")]
        public ParticleSystem particlePrefab;

        [Min(0)] public int poolSize = 12;
    }

    [Header("Terrain Source")]
    [Tooltip("If empty, uses Terrain.activeTerrain.")]
    public Terrain terrainOverride;

    [Tooltip("Raycast mask used to find the terrain hit point under the player.")]
    public LayerMask terrainRaycastMask = ~0;

    [Header("Raycast")]
    [Min(0f)] public float rayStartHeight = 0.25f;
    [Min(0f)] public float rayDistance = 3.0f;

    [Tooltip("Push particles slightly above the hit surface.")]
    public float hitNormalOffset = 0.01f;

    [Header("Terrain Layer Particle Pools")]
    public List<TerrainLayerToParticles> particlesByTerrainLayer = new List<TerrainLayerToParticles>();

    [Header("Water Optional Pool")]
    [Tooltip("If set and the creature is on/in water, this pool is used instead of terrain-layer detection.")]
    public ParticleSystem waterParticlePrefab;

    [Min(0)] public int waterPoolSize = 12;

    [Header("Pool Timing")]
    [Tooltip("Prevents spawning the same particle pool multiple times in the same frame due to double triggers.")]
    public bool useMinSpawnInterval = true;
    [Min(0f)] public float minSpawnIntervalSeconds = 0.01f;

    private Terrain _terrain;
    private TerrainData _terrainData;

    private struct Pool
    {
        public ParticleSystem prefab;
        public Queue<ParticleSystem> available;
    }

    // Key: terrain layer index in terrainData.terrainLayers
    private readonly Dictionary<int, Pool> _terrainPools = new Dictionary<int, Pool>();

    private Pool _waterPool;
    private bool _hasWaterPool = false;

    private float _lastSpawnTime = -999f;

    private void Awake()
    {
        if (terrainOverride != null)
            _terrain = terrainOverride;
        else
            _terrain = Terrain.activeTerrain;

        if (_terrain != null)
            _terrainData = _terrain.terrainData;
    }

    private void Start()
    {
        BuildTerrainPools();
        BuildWaterPool();
    }

    private void BuildTerrainPools()
    {
        _terrainPools.Clear();

        if (_terrainData == null)
        {
            Debug.LogWarning($"{nameof(PlayerFootstepParticleManager)}: No terrain found for terrain-layer particles.");
            return;
        }

        TerrainLayer[] terrainLayers = _terrainData.terrainLayers;
        if (terrainLayers == null || terrainLayers.Length == 0)
            return;

        for (int i = 0; i < particlesByTerrainLayer.Count; i++)
        {
            var entry = particlesByTerrainLayer[i];
            if (entry.terrainLayer == null || entry.particlePrefab == null || entry.poolSize <= 0)
                continue;

            int layerIndex = System.Array.IndexOf(terrainLayers, entry.terrainLayer);
            if (layerIndex < 0)
            {
                Debug.LogWarning($"{nameof(PlayerFootstepParticleManager)}: TerrainLayer '{entry.terrainLayer.name}' not found in TerrainData.terrainLayers order.");
                continue;
            }

            if (_terrainPools.ContainsKey(layerIndex))
                continue;

            var queue = new Queue<ParticleSystem>(entry.poolSize);
            for (int p = 0; p < entry.poolSize; p++)
            {
                ParticleSystem ps = Instantiate(entry.particlePrefab, transform);
                ps.gameObject.SetActive(false);
                queue.Enqueue(ps);
            }

            _terrainPools.Add(layerIndex, new Pool { prefab = entry.particlePrefab, available = queue });
        }
    }

    private void BuildWaterPool()
    {
        if (waterParticlePrefab == null || waterPoolSize <= 0)
        {
            _hasWaterPool = false;
            return;
        }

        var queue = new Queue<ParticleSystem>(waterPoolSize);
        for (int p = 0; p < waterPoolSize; p++)
        {
            ParticleSystem ps = Instantiate(waterParticlePrefab, transform);
            ps.gameObject.SetActive(false);
            queue.Enqueue(ps);
        }

        _waterPool = new Pool { prefab = waterParticlePrefab, available = queue };
        _hasWaterPool = true;
    }

    /// <summary>
    /// Call this from the creature scripts at the exact moment they play the "Step" sound.
    /// </summary>
    public void SpawnFootstep(Vector3 origin, bool onWater, bool inWater)
    {
        if (!enabled)
            return;

        if (useMinSpawnInterval)
        {
            if (Time.time < _lastSpawnTime + minSpawnIntervalSeconds)
                return;
            _lastSpawnTime = Time.time;
        }

        Vector3 rayOrigin = origin + Vector3.up * rayStartHeight;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, terrainRaycastMask, QueryTriggerInteraction.Ignore))
            return;

        // Water override: if on/in water, use water pool regardless of terrain texture.
        if ((onWater || inWater) && _hasWaterPool)
        {
            SpawnFromPool(ref _waterPool, hit.point, hit.normal);
            return;
        }

        if (_terrainData == null || _terrain == null)
            return;

        int dominantLayer = GetDominantTerrainLayerIndex(hit.point);
        if (dominantLayer < 0)
            return;

        if (_terrainPools.TryGetValue(dominantLayer, out Pool pool))
        {
            SpawnFromPool(ref pool, hit.point, hit.normal);
        }
    }

    private int GetDominantTerrainLayerIndex(Vector3 worldPoint)
    {
        Vector3 local = worldPoint - _terrain.transform.position;

        float uvx = local.x / _terrainData.size.x;
        float uvz = local.z / _terrainData.size.z;
        uvx = Mathf.Clamp01(uvx);
        uvz = Mathf.Clamp01(uvz);

        int ax = Mathf.Clamp((int)(uvx * (_terrainData.alphamapWidth - 1)), 0, _terrainData.alphamapWidth - 1);
        int az = Mathf.Clamp((int)(uvz * (_terrainData.alphamapHeight - 1)), 0, _terrainData.alphamapHeight - 1);

        float[,,] maps = _terrainData.GetAlphamaps(ax, az, 1, 1); // [x,y,layer]
        if (maps == null || maps.Length == 0)
            return -1;

        int layerCount = maps.GetLength(2);
        int best = 0;
        float bestWeight = maps[0, 0, 0];
        for (int i = 1; i < layerCount; i++)
        {
            float w = maps[0, 0, i];
            if (w > bestWeight)
            {
                bestWeight = w;
                best = i;
            }
        }

        return best;
    }

    private void SpawnFromPool(ref Pool pool, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (pool.available == null || pool.available.Count <= 0)
            return;

        ParticleSystem ps = pool.available.Dequeue();
        if (ps == null)
            return;

        ps.transform.position = hitPoint + hitNormal.normalized * hitNormalOffset;
        ps.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitNormal.normalized);

        var main = ps.main;
        float duration = main.duration;
        float maxStartLifetime = main.startLifetime.constantMax;
        float totalLife = Mathf.Max(0.01f, duration + maxStartLifetime);

        ps.gameObject.SetActive(true);
        ps.Play(true);

        // Return the instance to the pool only after it finishes playing.
        StartCoroutine(DisableAfterLifeAndReturn(ps, totalLife, pool.available));
    }

    private IEnumerator DisableAfterLifeAndReturn(ParticleSystem ps, float waitSeconds, Queue<ParticleSystem> queue)
    {
        yield return new WaitForSeconds(waitSeconds);

        if (ps == null)
            yield break;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);

        if (queue != null)
            queue.Enqueue(ps);
    }
}

