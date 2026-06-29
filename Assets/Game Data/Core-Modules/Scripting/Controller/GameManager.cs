using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight gameplay manager for player-death related safety logic.
/// Keeps nearby AI from instantly re-killing the player after revive.
/// </summary>
public class GameManager : MonoBehaviour
{
    public GameObject GamePauseScreen;

    public static GameManager Instance = null;
     [Header("References")]
    [Tooltip("Jurassic Pack manager reference. If empty, auto-found in scene.")]
    public Manager manager;
    public PlayerNeedsManager playerNeedsManager;

    [Header("Dead Player Settings")]
    [Tooltip("Enable dead-player anti-focus behavior for nearby AI.")]
    public bool enableDeadPlayerSettings = true;

    [Tooltip("When player is dead, AI within this radius will drop player target.")]
    [Min(0f)] public float deadPlayerClearAggroDistance = 30f;

    [Tooltip("Dead-player checks run on this interval (not every frame).")]
    [Min(0.05f)] public float deadPlayerCheckIntervalSeconds = 0.5f;

    [Tooltip("Remove player entry from nearby AI targetEditor list.")]
    public bool removePlayerFromTargetEditor = true;

    [Tooltip("If true, nearby AI behavior is forced after player death.")]
    public bool forceBehaviorAfterPlayerDeath = true;

    [Tooltip("Behavior applied when forceBehaviorAfterPlayerDeath is enabled.")]
    public string behaviorAfterPlayerDeath = "Idle";

    private float _nextCheckAt;

    public void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    public void Start()
    {
        GoogleAdManager.Instance.ShowSmallBannerRight();
        GameAnalytics.Event("Gameplay");
    }
    public void ShowGamePauseScreen()
    { 

        GamePauseScreen.SetActive(true);

        Time.timeScale = 0f;

        if (GoogleAdManager.Instance != null && GoogleAdManager.Instance.CanShowInterstitial())
            SignalBus.Publish(new OnGamePausedSignal());

        DOVirtual.DelayedCall(0.25f, () => { ShowInterstitial(); }).SetUpdate(true);
        GameAnalytics.Event("GamePause");

        // GoogleAdManager.Instance.ShowBigBanner();
    }

    void ShowInterstitial()

    {
        if(GoogleAdManager.Instance != null) 
        GoogleAdManager.Instance.ShowAdmobInterstitial();
    }


    public void HideGamePauseScreen()
    {
        GamePauseScreen.SetActive(false);
        Time.timeScale = 1f;
        GameAnalytics.Event("resume");
        // GoogleAdManager.Instance.HideBigBanner();
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        LoadingManager.Instance.LoadMainGame();
        GameAnalytics.Event("Restart");
    }
    public void Home()
    {
        Time.timeScale = 1f;
        LoadingManager.Instance.LoadMainMenu();
        GameAnalytics.Event("Home");
    }

    private void Update()
    {
        if (!enableDeadPlayerSettings)
            return;

        if (Time.time < _nextCheckAt)
            return;

        _nextCheckAt = Time.time + Mathf.Max(0.05f, deadPlayerCheckIntervalSeconds);

        if (manager == null)
            manager = FindFirstObjectByType<Manager>();
        if (manager == null)
            return;

        Creature player = GetCurrentPlayerCreature(manager);
        if (player == null)
        {
            return;
        }

        bool isDead = player.health <= 0.01f;
        if (isDead)
        {
            // Keep applying while dead so any new nearby aggro gets cleared as well.
            ClearNearbyAIAggroToPlayer(player.gameObject, player.transform.position, deadPlayerClearAggroDistance);
        }
    }

    private static Creature GetCurrentPlayerCreature(Manager m)
    {
        if (m == null || m.creaturesList == null || m.creaturesList.Count == 0)
            return null;
        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
            return null;

        GameObject go = m.creaturesList[m.selected];
        if (go == null || !go.activeInHierarchy)
            return null;

        Creature c = go.GetComponent<Creature>();
        if (c == null || c.useAI)
            return null;

        return c;
    }

    private void ClearNearbyAIAggroToPlayer(GameObject playerGo, Vector3 playerPos, float radius)
    {
        if (playerGo == null || manager == null || manager.creaturesList == null)
            return;

        float radiusSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);

        for (int i = 0; i < manager.creaturesList.Count; i++)
        {
            GameObject go = manager.creaturesList[i];
            if (go == null || !go.activeInHierarchy || go == playerGo)
                continue;

            Creature ai = go.GetComponent<Creature>();
            if (ai == null || !ai.useAI)
                continue;

            if ((go.transform.position - playerPos).sqrMagnitude > radiusSqr)
                continue;

            RemovePlayerFromAI(ai, playerGo);
        }
    }

    private void RemovePlayerFromAI(Creature ai, GameObject playerGo)
    {
        if (ai == null || playerGo == null)
            return;
        if (removePlayerFromTargetEditor && ai.targetEditor != null)
        {
            for (int t = ai.targetEditor.Count - 1; t >= 0; t--)
            {
                if (ai.targetEditor[t]._GameObject == playerGo)
                    ai.targetEditor.RemoveAt(t);
            }
        }
        if (ai.objTGT == playerGo)
        {
            ai.objTGT = null;
            ai.posTGT = Vector3.zero;
        }
        if (forceBehaviorAfterPlayerDeath)
        {
            ai.behavior = string.IsNullOrEmpty(behaviorAfterPlayerDeath) ? "Idle" : behaviorAfterPlayerDeath;
            ai.behaviorCount = 0f;
            ai.onAttack = false;
            if (ai.anm != null)
                ai.anm.SetBool("Attack", false);
        }
    }
}

