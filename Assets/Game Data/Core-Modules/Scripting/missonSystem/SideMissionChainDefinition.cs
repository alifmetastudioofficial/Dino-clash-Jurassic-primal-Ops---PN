using UnityEngine;

[CreateAssetMenu(fileName = "SideMissionChain", menuName = "Missions/Side Mission Chain")]
public class SideMissionChainDefinition : ScriptableObject
{
    [Header("Chain Identity")]
    public string chainId;
    public string chainTitle;

    [Header("Mission Steps")]
    public MissionDefinition[] steps;
}