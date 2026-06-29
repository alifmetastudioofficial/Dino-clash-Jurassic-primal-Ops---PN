using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public bool AutoLoadScene;
    public static LoadingManager Instance { get; private set; }

    [Header("Scene Information")]
    public string GameplayScene;

    [Header("Loading UI")]
    public GameObject loadingCanvas;
    public Slider loadingSlider;
    public TMP_Text loadingText;

    [Header("Loading Images")]
    public Image loadingImage;
    public Sprite[] loadingSprites;

    [Header("Fake Loading Settings")]
    public float fakeLoadingTime = 5f;

    private bool isLoading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (loadingCanvas != null)
            loadingCanvas.SetActive(false);

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        if (loadingText != null)
            loadingText.text = "0%";

        if (AutoLoadScene)
            LoadScene(GameplayScene);
    }

    void SetRandomImage()
    {
        if (loadingSprites != null && loadingSprites.Length > 0 && loadingImage != null)
        {
            int randomIndex = Random.Range(0, loadingSprites.Length);
            loadingImage.sprite = loadingSprites[randomIndex];
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;

        

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        Time.timeScale = 1f;

        if (loadingCanvas != null)
        {
            
            loadingCanvas.SetActive(true);
            //GoogleAdManager.Instance.ShowBigBanner();
        }

        //  Random image set here
        SetRandomImage();

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        float timer = 0f;

        while (timer < fakeLoadingTime)
        {
            timer += Time.unscaledDeltaTime;

            float progress = timer / fakeLoadingTime;

            if (loadingSlider != null)
                loadingSlider.value = progress;

            int percent = Mathf.Clamp(Mathf.RoundToInt(progress * 100), 1, 100);

            if (loadingText != null)
                loadingText.text = percent + "%";

            yield return null;
        }

        if (loadingSlider != null)
            loadingSlider.value = 1f;

        if (loadingText != null)
            loadingText.text = "100%";

        yield return new WaitForSecondsRealtime(0.2f);
        
        SceneManager.LoadScene(sceneName);

        yield return null;

        if (loadingCanvas != null)
        { 
            loadingCanvas.SetActive(false);
            //GoogleAdManager.Instance.HideBigBanner();
        }

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        isLoading = false;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        LoadScene("DinoSelection New");
    }

    public void LoadMainGame()
    {
        Time.timeScale = 1f;
        LoadScene(GameplayScene);
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }
}