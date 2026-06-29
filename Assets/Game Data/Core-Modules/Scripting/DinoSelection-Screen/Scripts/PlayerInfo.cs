using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInfo", menuName = "Game/Player Info")]
public class PlayerInfo : ScriptableObject
{
    [Header("Identity")]
    public string playerId;
    public string displayName;

    [Header("Unlocking")]
    public int unlockPrice = 0;
    public bool unlockedByDefault = false;

    [Header("Selection Camera")]
    public float cameraDistance = 8f;
    public Vector3 cameraOffset = new Vector3(0f, 2f, 0f);

    [Header("Gameplay Camera Settings (GP)")]
    [Tooltip("Gameplay CameraManager distance (per player).")]
    public float cameraDistanceGP = 8f;
    [Tooltip("Gameplay CameraManager height offset (Y) (per player).")]
    public float heightOffsetGP = 2f;

    [Header("Skins (per dino, 3 = SkinA/B/C)")]
    public int[] skinPrice = new int[] { 0, 100, 200 };
    public bool[] skinDefaultUnlock = new bool[] { true, false, false };

    [Header("Skins Rewarded Ads Required (per dino, 3 = SkinA/B/C)")]
    public int[] skinRewardedAdsRequired = new int[] { 0, 2, 2 };

    [Header("Eyes (shared, 16 = Type0..Type15)")]
    public int[] eyePrice = new int[] { 0, 50, 50, 50, 75, 75, 75, 100, 100, 100, 100, 150, 150, 150, 200, 200 };
    public bool[] eyeDefaultUnlock = new bool[] { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };

    [Header("Eyes Rewarded Ads Required (shared, 16 = Type0..Type15)")]
    public int[] eyeRewardedAdsRequired = new int[] { 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
}


//using UnityEngine;

//[CreateAssetMenu(fileName = "PlayerInfo", menuName = "Game/Player Info")]
//public class PlayerInfo : ScriptableObject
//{
//    [Header("Identity")]
//    public string playerId;
//    public string displayName;

//    [Header("Unlocking")]
//    public int unlockPrice = 0;
//    public bool unlockedByDefault = false;

//    [Header("Selection Camera")]
//    public float cameraDistance = 8f;
//    public Vector3 cameraOffset = new Vector3(0f, 2f, 0f);

//    [Header("Gameplay Camera Settings (GP)")]
//    [Tooltip("Gameplay CameraManager distance (per player).")]
//    public float cameraDistanceGP = 8f;
//    [Tooltip("Gameplay CameraManager height offset (Y) (per player).")]
//    public float heightOffsetGP = 2f;

//    [Header("Skins (per dino, 3 = SkinA/B/C)")]
//    public int[] skinPrice = new int[] { 0, 100, 200 };
//    public bool[] skinDefaultUnlock = new bool[] { true, false, false };

//    [Header("Eyes (shared, 16 = Type0..Type15)")]
//    public int[] eyePrice = new int[] { 0, 50, 50, 50, 75, 75, 75, 100, 100, 100, 100, 150, 150, 150, 200, 200 };
//    public bool[] eyeDefaultUnlock = new bool[] { true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
//}

