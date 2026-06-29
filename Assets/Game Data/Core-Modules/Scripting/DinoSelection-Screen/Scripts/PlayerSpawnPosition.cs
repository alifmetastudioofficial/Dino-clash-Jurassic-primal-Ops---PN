using UnityEngine;
using MTAssets.EasyMinimapSystem;
/// <summary>
/// DinoDemoEnv scene mein use karo. Selected player ko spawn positions mein se ek par spawn karta hai,
/// us ka Creature script enable karta hai, aur CameraManager ka target set karta hai.
/// </summary>
public class PlayerSpawnPosition : MonoBehaviour
{
    public MinimapItem PlayerMarker;
    public Transform PlayerRoot;
    [Header("Spawn Positions")]
    [Tooltip("Jin positions par player spawn ho sakta hai; spawnIndex wali use hogi.")]
    public Transform[] spawnPositions;

    [Tooltip("Kaun si spawn position use karni hai (0 = pehli).")]
    public int spawnIndex = 0;
    [Tooltip("If true, player spawns at a random valid spawn position.")]
    public bool useRandomSpawnPosition = true;

    [Header("Player Prefabs (playerId -> prefab)")]
    [Tooltip("Har selectable player ke liye prefab. playerId PlayerInfo.playerId se match honi chahiye.")]
    public PlayerPrefabEntry[] playerPrefabs;

    [Header("Camera")]
    [Tooltip("CameraManager jis ka target spawn hone wale player par set hoga. Khali choro to scene mein dhund liya jayega.")]
    public CameraManager cameraManager;

    [Header("Player Footsteps (Terrain -> Particles)")]
    [Tooltip("Template configuration for PlayerFootstepParticleManager. This settings will be copied to every spawned player.")]
    public PlayerFootstepParticleManager footstepParticleManagerTemplate;

    [Header("Fallback")]
    [Tooltip("Agar koi player select na ho to yeh playerId use hogi (optional).")]
    public string defaultPlayerId = "";

    public GameObject _spawnedPlayer;

    [SerializeField] DomeFollower objectDome;

    [System.Serializable]
    public class PlayerPrefabEntry
    {
        public string playerId;
        public GameObject prefab;
        [Tooltip("Player info (camera defaults: distance + height offset). Optional but recommended for per-player camera setup.")]
        public PlayerInfo info;
    }

    private void Start()
    {
        SpawnSelectedPlayer();
    }

    public void SpawnSelectedPlayer()
    {
        string selectedId = UnlockManager.GetSelectedPlayer(defaultPlayerId);
        if (string.IsNullOrEmpty(selectedId))
        {
            if (!string.IsNullOrEmpty(defaultPlayerId))
            {
                selectedId = defaultPlayerId;
            }
            else
            {
                return;
            }
        }

        GameObject prefab = GetPrefabForPlayer(selectedId);
        if (prefab == null)
        {
            return;
        }

        PlayerPrefabEntry entry = GetEntryForPlayer(selectedId);

        Transform spawnPoint = GetSpawnTransform();
        if (spawnPoint == null)
        {
            return;
        }

        _spawnedPlayer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, PlayerRoot);
        PlayerMarker.customGameObjectToFollowRotation = _spawnedPlayer.transform;
        if (objectDome != null)
        objectDome.SetUpTarget(_spawnedPlayer.transform);
        Creature creature = _spawnedPlayer.GetComponentInChildren<Creature>(true);
       
        if (creature != null)
        {
            creature.enabled = true;

            int savedSkin, savedEyes;
            UnlockManager.GetPlayerAppearance(selectedId, 0, 0, out savedSkin, out savedEyes);
            creature.SetMaterials(
                Mathf.Clamp(savedSkin, 0, 2),
                Mathf.Clamp(savedEyes, 0, 15)
            );

            // Player needs system (only applies when this creature is player-controlled).
            PlayerNeedsApplier applier = creature.GetComponent<PlayerNeedsApplier>();
            if (applier == null)
            {
                applier = creature.gameObject.AddComponent<PlayerNeedsApplier>();
            }

            applier.creature = creature;
            if (applier.needsManager == null)
            {
                applier.needsManager = FindFirstObjectByType<PlayerNeedsManager>();
            }

            // Player footsteps particles (only for player, not AI).
            PlayerFootstepParticleManager footMgr = creature.GetComponent<PlayerFootstepParticleManager>();
            if (footMgr == null)
            {
                footMgr = creature.gameObject.AddComponent<PlayerFootstepParticleManager>();
            }

            if (footstepParticleManagerTemplate != null && footMgr != null)
            {
                CopyFootstepSettings(footstepParticleManagerTemplate, footMgr);
            }


            GameAnalytics.Event("gameplay_started",
                GameAnalytics.P("player_id", selectedId),
                GameAnalytics.P("spawn_random", useRandomSpawnPosition ? 1 : 0),
                GameAnalytics.P("can_fly", creature.canFly ? 1 : 0),
                GameAnalytics.P("can_swim", creature.canSwim ? 1 : 0),
                GameAnalytics.P("can_latch", creature.canLatchAttack ? 1 : 0),
                GameAnalytics.P("saved_skin", savedSkin),
                GameAnalytics.P("saved_eyes", savedEyes));

        }

        // Ensure tag so Manager/JP systems treat this as a creature
        if (_spawnedPlayer.CompareTag("Untagged"))
        {
            _spawnedPlayer.tag = "Creature";
        }

        // Register as selected creature in Jurassic Pack Manager, so input condition matches
        Manager manager = FindObjectOfType<Manager>();
        if (manager != null)
        {
            if (manager.creaturesList == null)
            {
                manager.creaturesList = new System.Collections.Generic.List<GameObject>();
            }

            if (!manager.creaturesList.Contains(_spawnedPlayer))
            {
                manager.creaturesList.Add(_spawnedPlayer);
            }

            manager.selected = manager.creaturesList.IndexOf(_spawnedPlayer);
            if (manager.cameraMode == 0)
            {
                manager.cameraMode = 1; // follow mode
            }
        }

        CameraManager cam = cameraManager != null ? cameraManager : FindObjectOfType<CameraManager>();
        if (cam != null)
        {
            cam.target = _spawnedPlayer.transform;

            // Apply per-player camera defaults from PlayerInfo.
            if (entry != null && entry.info != null)
            {
                cam.distance = entry.info.cameraDistanceGP;
                cam.heightOffset = entry.info.heightOffsetGP;
            }
        }

        // Inform gameplay camera UI about current player so it loads per-player saved values.
        CameraSettingsUI ui = FindObjectOfType<CameraSettingsUI>();
        if (ui != null)
        {
            ui.SetCurrentPlayerId(selectedId);
        }



       






    }

    private void CopyFootstepSettings(PlayerFootstepParticleManager src, PlayerFootstepParticleManager dst)
    {
        if (src == null || dst == null)
            return;

        dst.terrainOverride = src.terrainOverride;
        dst.terrainRaycastMask = src.terrainRaycastMask;
        dst.rayStartHeight = src.rayStartHeight;
        dst.rayDistance = src.rayDistance;
        dst.hitNormalOffset = src.hitNormalOffset;
        dst.particlesByTerrainLayer = src.particlesByTerrainLayer;
        dst.waterParticlePrefab = src.waterParticlePrefab;
        dst.waterPoolSize = src.waterPoolSize;
        dst.useMinSpawnInterval = src.useMinSpawnInterval;
        dst.minSpawnIntervalSeconds = src.minSpawnIntervalSeconds;
    }

    private GameObject GetPrefabForPlayer(string playerId)
    {
        if (playerPrefabs == null)
        {
            return null;
        }

        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            if (playerPrefabs[i] != null && playerPrefabs[i].playerId == playerId && playerPrefabs[i].prefab != null)
            {
                return playerPrefabs[i].prefab;
            }
        }

        return null;
    }

    private PlayerPrefabEntry GetEntryForPlayer(string playerId)
    {
        if (playerPrefabs == null)
            return null;

        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            if (playerPrefabs[i] != null && playerPrefabs[i].playerId == playerId)
                return playerPrefabs[i];
        }

        return null;
    }

    private Transform GetSpawnTransform()
    {
        if (spawnPositions == null || spawnPositions.Length == 0)
        {
            return transform;
        }

        if (useRandomSpawnPosition)
        {
            // Pick only valid (non-null) points so random spawn never falls back to index 0 by mistake.
            int validCount = 0;
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                if (spawnPositions[i] != null)
                    validCount++;
            }

            if (validCount > 0)
            {
                int pick = UnityEngine.Random.Range(0, validCount);
                for (int i = 0; i < spawnPositions.Length; i++)
                {
                    if (spawnPositions[i] == null)
                        continue;
                    if (pick == 0)
                        return spawnPositions[i];
                    pick--;
                }
            }
        }

        int index = Mathf.Clamp(spawnIndex, 0, spawnPositions.Length - 1);
        return spawnPositions[index] != null ? spawnPositions[index] : transform;
    }

    /// <summary>
    /// Spawned player ka reference (e.g. Manager ya dusri scripts ke liye).
    /// </summary>
    public GameObject GetSpawnedPlayer()
    {
        return _spawnedPlayer;
    }
}
