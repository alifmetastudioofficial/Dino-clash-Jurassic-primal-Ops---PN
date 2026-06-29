using System;
using System.Collections.Generic;

[Serializable]
public class SideMissionRuntimeData
{
    public string chainId;
    public int currentStepIndex;
    public float progress;
    public bool completed;
    public bool claimed;
}

[Serializable]
public class SideMissionSaveData
{
    public List<SideMissionRuntimeData> chains = new List<SideMissionRuntimeData>();
}