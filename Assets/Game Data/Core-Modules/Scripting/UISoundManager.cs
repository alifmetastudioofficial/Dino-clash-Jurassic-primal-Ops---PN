using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    public enum ClickSoundType
    {
        SoftClick,
        MediumClick,
        HighClick
    }

    [Header("Click Sounds")]
    [SerializeField] private AudioClip softClick;
    [SerializeField] private AudioClip mediumClick;
    [SerializeField] private AudioClip highClick;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayClickSound(ClickSoundType soundType)
    {
        AudioClip clip = GetClip(soundType);

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    private AudioClip GetClip(ClickSoundType soundType)
    {
        switch (soundType)
        {
            case ClickSoundType.SoftClick:
                return softClick;

            case ClickSoundType.MediumClick:
                return mediumClick;

            case ClickSoundType.HighClick:
                return highClick;

            default:
                return null;
        }
    }
}