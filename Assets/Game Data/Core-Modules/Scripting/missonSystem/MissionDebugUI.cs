using UnityEngine;

public class MissionDebugUI : MonoBehaviour
{
    public MissionManager missionManager;

    private void Awake()
    {
        if (missionManager == null)
            missionManager = MissionManager.Instance;
    }

    public void OnNextDayPressed()
    {
        if (missionManager != null)
            missionManager.Debug_ForceNextDay();
    }

    public void OnResetPressed()
    {
        if (missionManager != null)
            missionManager.Debug_ResetAllMissions();
    }
}