using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerSelectButton : MonoBehaviour
{
    [Header("Ids / References")]
    [Tooltip("Yeh id PlayerInfo.playerId se match honi chahiye.")]
    public string playerId;

    [Tooltip("Central PlayerSelectionManager jo 3D models ko control kar raha hai.")]
    public PlayerSelectionManager selectionManager;

    [Header("Visuals")]
    [Tooltip("Is button ke upar lock icon ka Image.")]
    public Image lockImage;

    [Tooltip("Optional: price text jo is button ke upar show hoga.")]
    public TMPro.TextMeshProUGUI priceText;

    [Tooltip("Selector/highlight image jo current unlocked player button par on hogi.")]
    public GameObject selectedHighlight;
    public DOTweenAnimation SelectionAnimation;

    private void OnEnable()
    {
        RefreshVisual();
    }

    public void OnClickSelect()
    {
        if (selectionManager == null || string.IsNullOrEmpty(playerId))
        {
            return;
        }

        selectionManager.SelectById(playerId);
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        PlayerInfo info = FindPlayerInfo();
        bool unlockedByDefault = info != null && info.unlockedByDefault;

        // Unlock state
        bool isUnlocked = UnlockManager.IsUnlocked(playerId, unlockedByDefault);

        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(!isUnlocked);
        }

        // Price text (optional, sirf locked pe dikhani ho to)
        if (priceText != null)
        {
            if (!isUnlocked)
            {
                priceText.text = info != null ? info.unlockPrice.ToString() : string.Empty;
            }
            else
            {
                priceText.text = string.Empty;
            }
        }

        // Selector: sirf jab yeh hi current selected player ho aur unlocked ho
        if (selectedHighlight != null && selectionManager != null)
        {
            string currentId = selectionManager.GetCurrentPlayerId();
            bool isCurrent = !string.IsNullOrEmpty(currentId) && currentId == playerId;
           // selectedHighlight.SetActive(isCurrent && isUnlocked);
            selectedHighlight.SetActive(isCurrent);
            if (!SelectionAnimation)
            {
                return;
            }
            if (selectedHighlight.activeInHierarchy)
            {
                SelectionAnimation.DOPlay();
            }
            else
                SelectionAnimation.DORewind();

        }
    }

    private PlayerInfo FindPlayerInfo()
    {
        if (selectionManager == null || selectionManager.players == null)
        {
            return null;
        }

        foreach (SelectablePlayer p in selectionManager.players)
        {
            if (p != null && p.info != null && p.info.playerId == playerId)
            {
                return p.info;
            }
        }

        return null;
    }
}

