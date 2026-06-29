using UnityEngine;

public class MenuLinks : MonoBehaviour
{
    [Header("Store / Web Links")]
    [SerializeField] private string moreGamesUrl;
    [SerializeField] private string rateUsUrl;
    [SerializeField] private string privacyPolicyUrl;
    [SerializeField] private string discordUrl;

    public void OpenMoreGames()
    {
        OpenLink(moreGamesUrl);
    }

    public void OpenRateUs()
    {
        OpenLink(rateUsUrl);
    }

    public void OpenPrivacyPolicy()
    {
        OpenLink(privacyPolicyUrl);
    }

    public void OpenDiscord()
    {
        OpenLink(discordUrl);
    }

    private void OpenLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.Log("MenuLinks: URL is empty.");
            return;
        }

        Application.OpenURL(url);
    }
}