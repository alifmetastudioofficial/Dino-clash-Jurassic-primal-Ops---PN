using System;
using System.Collections.Generic;

[Serializable]
public class MissionRuntimeData
{
    public string missionId;
    public float progress;
    public bool completed;
    public bool claimed;

    // Daily reset ke liye
    public string assignedDay;

    // Agar true hai to ye old completed mission hai jo claim ke liye carry hua hai
    public bool graceClaimOnly;
}

[Serializable]
public class MissionSaveData
{
    public string savedDay;
    public List<MissionRuntimeData> missions = new List<MissionRuntimeData>();
}