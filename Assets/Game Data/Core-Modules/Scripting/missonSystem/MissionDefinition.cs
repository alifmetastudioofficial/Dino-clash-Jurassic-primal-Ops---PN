using UnityEngine;

[CreateAssetMenu(fileName = "MissionDefinition", menuName = "Missions/Mission Definition")]
public class MissionDefinition : ScriptableObject
{
    [Header("Identity")]
    public string missionId;
    public string title;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Tracking")]
    public MissionEventType eventType;
    public float targetValue = 1f;

    [Header("Reward")]
    public int cashReward = 100;

    [Header("Filters Optional")]
    public string requiredVictimSpecies;
    public string requiredPlayerSpecies;
    public bool requireVictimCanFly;
    public bool requireVictimPredator;
}