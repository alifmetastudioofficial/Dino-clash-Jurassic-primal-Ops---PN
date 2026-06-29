using UnityEngine;

public class SelectablePlayer : MonoBehaviour
{
    public PlayerInfo info;

    [HideInInspector]
    public bool IsUnlocked;

    [Header("Selection Feedback")]
    [Tooltip("Animator on this dino used to play select animation.")]
    public Animator animator;

    [Tooltip("Animation state name to play when this dino is selected (empty = skip).")]
    public string selectAnimationName;

    [Tooltip("Optional select sound to play when this dino is selected.")]
    public AudioClip selectSound;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _audioSource = GetComponentInChildren<AudioSource>();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// Called by PlayerSelectionManager jab yeh dino select hota hai (visible + currentIndex match).
    /// Plays optional animation and select sound.
    /// </summary>
    public void PlaySelectFeedback()
    {
        if (animator != null && !string.IsNullOrEmpty(selectAnimationName))
        {
            animator.Play(selectAnimationName);
        }

        if (selectSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(selectSound);
        }
    }
}

