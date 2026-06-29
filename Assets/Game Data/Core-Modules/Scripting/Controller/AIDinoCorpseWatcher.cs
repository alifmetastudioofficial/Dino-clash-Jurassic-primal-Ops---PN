using System.Collections;
using UnityEngine;

/// <summary>
/// Watches a spawned AI dino:
/// - When health reaches 0, leaves body for food for a configured lifetime
/// - Then asks AIZonePoolManager to destroy corpse and create replacement
/// </summary>
[DisallowMultipleComponent]
public class AIDinoCorpseWatcher : MonoBehaviour
{
    private AIZonePoolManager _zoneManager;
    private AIZonePoolManager.AIZone _zone;

    private Creature _creature;
    private GameObject _prefab;
    private GameObject _instance;

    private float _corpseLifetimeSeconds;
    private float _pollIntervalSeconds;
    private bool _deadDetected;
    private float _deadAtTime;
    private Coroutine _watchRoutine;

    public void Setup(
        AIZonePoolManager zoneManager,
        AIZonePoolManager.AIZone zone,
        GameObject prefab,
        GameObject instance,
        float corpseLifetimeSeconds,
        float pollIntervalSeconds)
    {
        StopAllCoroutines();
        _watchRoutine = null;
        _zoneManager = zoneManager;
        _zone = zone;
        _prefab = prefab;
        _instance = instance;
        _corpseLifetimeSeconds = Mathf.Max(0f, corpseLifetimeSeconds);
        _pollIntervalSeconds = Mathf.Max(0.05f, pollIntervalSeconds);
        _deadDetected = false;
        _deadAtTime = -1f;

        _creature = instance != null ? instance.GetComponentInChildren<Creature>(true) : null;

        _watchRoutine = StartCoroutine(WatchLoop());
    }

    private IEnumerator WatchLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(_pollIntervalSeconds);
        while (true)
        {
            if (_zoneManager == null || _zone == null || _creature == null || _instance == null)
            {
                yield return wait;
                continue;
            }

            if (!_deadDetected)
            {
                if (_creature.health <= 0.01f)
                {
                    _deadDetected = true;
                    _deadAtTime = Time.time;
                }
            }
            else
            {
                // Keep corpse available for food, then expire on interval checks.
                if (Time.time >= _deadAtTime + _corpseLifetimeSeconds)
                {
                    _zoneManager.HandleCorpseExpired(_zone, _instance);
                    yield break;
                }
            }

            yield return wait;
        }
    }
}

