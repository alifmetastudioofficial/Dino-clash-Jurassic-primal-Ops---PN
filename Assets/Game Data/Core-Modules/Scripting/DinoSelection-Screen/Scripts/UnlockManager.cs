using UnityEngine;

public static class UnlockManager
{
    private const string SelectedPlayerKey = "selected_player";

    public static bool IsUnlocked(string playerId, bool unlockedByDefault)
    {
        string key = GetPlayerKey(playerId);
        int defaultValue = unlockedByDefault ? 1 : 0;
        int value = PlayerPrefs.GetInt(key, defaultValue);
        return value == 1;
    }

    public static void SetUnlocked(string playerId)
    {
        string key = GetPlayerKey(playerId);
        PlayerPrefs.SetInt(key, 1);
    }

    public static void SetSelectedPlayer(string playerId)
    {
        PlayerPrefs.SetString(SelectedPlayerKey, playerId);
    }

    public static string GetSelectedPlayer(string defaultId)
    {
        return PlayerPrefs.GetString(SelectedPlayerKey, defaultId);
    }

    private static string GetPlayerKey(string playerId)
    {
        return "player_" + playerId;
    }

    // --- Skin & Eyes (DinoSelection se DinoDemoEnv tak carry) ---
    private static string GetAppearanceSkinKey(string playerId) => "appearance_skin_" + playerId;
    private static string GetAppearanceEyesKey(string playerId) => "appearance_eyes_" + playerId;

    public static void SavePlayerAppearance(string playerId, int skinIndex, int eyesIndex)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        PlayerPrefs.SetInt(GetAppearanceSkinKey(playerId), skinIndex);
        PlayerPrefs.SetInt(GetAppearanceEyesKey(playerId), eyesIndex);
        PlayerPrefs.Save();
    }

    public static void GetPlayerAppearance(string playerId, int defaultSkin, int defaultEyes, out int skinIndex, out int eyesIndex)
    {
        skinIndex = defaultSkin;
        eyesIndex = defaultEyes;
        if (string.IsNullOrEmpty(playerId)) return;
        skinIndex = PlayerPrefs.GetInt(GetAppearanceSkinKey(playerId), defaultSkin);
        eyesIndex = PlayerPrefs.GetInt(GetAppearanceEyesKey(playerId), defaultEyes);
    }

    // --- Skin unlock (per player) ---
    private static string GetSkinUnlockKey(string playerId, int skinIndex) => "unlock_skin_" + playerId + "_" + skinIndex;

    public static bool IsSkinUnlocked(string playerId, int skinIndex, bool defaultUnlock)
    {
        if (string.IsNullOrEmpty(playerId)) return defaultUnlock;
        int def = defaultUnlock ? 1 : 0;
        return PlayerPrefs.GetInt(GetSkinUnlockKey(playerId, skinIndex), def) == 1;
    }

    public static void SetSkinUnlocked(string playerId, int skinIndex)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        PlayerPrefs.SetInt(GetSkinUnlockKey(playerId, skinIndex), 1);
        PlayerPrefs.Save();
    }

    // --- Eye unlock (global, sab dinos ke liye same) ---
    private static string GetEyeUnlockKey(int eyeIndex) => "unlock_eye_" + eyeIndex;

    public static bool IsEyeUnlocked(int eyeIndex, bool defaultUnlock)
    {
        int def = defaultUnlock ? 1 : 0;
        return PlayerPrefs.GetInt(GetEyeUnlockKey(eyeIndex), def) == 1;
    }

    public static void SetEyeUnlocked(int eyeIndex)
    {
        PlayerPrefs.SetInt(GetEyeUnlockKey(eyeIndex), 1);
        PlayerPrefs.Save();
    }
}

