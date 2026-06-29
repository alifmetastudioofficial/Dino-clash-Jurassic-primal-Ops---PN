using UnityEngine;

[DisallowMultipleComponent]
public sealed class AIRandomAppearanceMobile : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Creature creature;

    [Header("Variant Counts")]
    [Tooltip("0 ho to auto-detect try karega")]
    [SerializeField] private int skinCount = 0;

    [Tooltip("0 ho to auto-detect try karega")]
    [SerializeField] private int eyeCount = 0;

    [Header("Timing")]
    [Tooltip("Agar AI setup thora late hota ho to apply se pehle delay")]
    [SerializeField] private float applyDelay = 0.05f;

    [Header("Behavior")]
    [Tooltip("Agar true ho to same scene load par same object ka same random result aa sakta hai")]
    [SerializeField] private bool deterministicPerObject = false;

    private bool applied;

    private void Awake()
    {
        if (creature == null)
            creature = GetComponent<Creature>();
    }

    private void OnEnable()
    {
        if (applied)
            return;

        if (applyDelay <= 0f)
        {
            TryApply();
        }
        else
        {
            Invoke(nameof(TryApply), applyDelay);
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void TryApply()
    {
        if (applied)
            return;

        if (creature == null)
            return;

        // Sirf AI dinos par random appearance lagani hai
        if (!creature.useAI)
            return;

        int skins = skinCount;
        int eyes = eyeCount;

        // Auto-detect only if needed
        if (skins <= 0)
        {
            skins = GetSkinCount(creature);
            if (skins <= 0) skins = 1;
        }

        if (eyes <= 0)
        {
            eyes = GetEyeCount(creature);
            if (eyes <= 0) eyes = 1;
        }

        int randomSkin;
        int randomEye;

        if (deterministicPerObject)
        {
            int seed = gameObject.GetInstanceID();
            unchecked
            {
                seed = (seed * 397) ^ transform.GetSiblingIndex();
            }

            System.Random rng = new System.Random(seed);
            randomSkin = rng.Next(0, skins);
            randomEye = rng.Next(0, eyes);
        }
        else
        {
            randomSkin = Random.Range(0, skins);
            randomEye = Random.Range(0, eyes);
        }

        creature.SetMaterials(randomSkin, randomEye);
        applied = true;
    }

    private static int GetSkinCount(Creature c)
    {
        // Assumption: Creature mein skin array hai
        return c.skin != null ? c.skin.Length : 0;
    }

    private static int GetEyeCount(Creature c)
    {
        // Assumption: Creature mein eyes array/type array hai
        // Neeche common naming ke hisaab se examples diye gaye hain.
        // Jo field tumhari Creature class mein actual موجود ho usko rakho.

        // Example 1:
        if (c.eyes != null)
            return c.eyes.Length;

        // Example 2:
        // if (c.eye != null)
        //     return c.eye.Length;

        // Example 3:
        // if (c.eyesTextureArray != null)
        //     return c.eyesTextureArray.Length;

        return 0;
    }
}