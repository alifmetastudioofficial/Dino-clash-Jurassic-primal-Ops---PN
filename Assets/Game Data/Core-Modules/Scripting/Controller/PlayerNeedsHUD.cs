using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas HUD for current player/selected creature's needs.
/// Assign 4 fill Images (type: Filled) or leave any empty to skip.
/// </summary>
public class PlayerNeedsHUD : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Whole HUD root (Canvas panel). If empty, this GameObject will be used.")]
    public GameObject root;

    [Header("Fill Images (0..1)")]
    public Image healthFill;
    public Image staminaFill;
    public Image foodFill;
    public Image waterFill;

    [Header("Options")]
    [Tooltip("If true, finds Manager automatically if not assigned.")]
    public bool findManagerIfMissing = true;

    public Manager manager;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
    }

    private void Update()
    {
        if (manager == null && findManagerIfMissing)
            manager = FindFirstObjectByType<Manager>();

        if (manager == null)
            return;

        SetVisible(manager.showPlayerNeedsHUD);

        if (!manager.showPlayerNeedsHUD)
            return;

        Creature c = GetCurrentCreature(manager);
        if (c == null)
            return;

        SetFill(healthFill, c.health);
        SetFill(staminaFill, c.stamina);
        SetFill(foodFill, c.food);
        SetFill(waterFill, c.water);
    }

    public void SetVisible(bool visible)
    {
        if (root != null && root.activeSelf != visible)
            root.SetActive(visible);
    }

    private static Creature GetCurrentCreature(Manager m)
    {
        if (m == null || m.creaturesList == null || m.creaturesList.Count == 0)
            return null;
        if (m.selected < 0 || m.selected >= m.creaturesList.Count)
            return null;

        GameObject go = m.creaturesList[m.selected];
        if (go == null || !go.activeInHierarchy)
            return null;

        return go.GetComponent<Creature>();
    }

    private static void SetFill(Image img, float value01_100)
    {
        if (img == null)
            return;

        img.fillAmount = Mathf.Clamp01(value01_100 / 100f);
    }
}

