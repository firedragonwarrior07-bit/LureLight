using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI loadingTipText;
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    [SerializeField] private string sceneToLoad = "Latest one"; // Your game scene
    [SerializeField] private float minLoadTime = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Loading Tips")]
    [SerializeField] private string[] loadingTips = new string[]
    {
        "💡 Collect orbs to upgrade your abilities!",
        "💡 Use Dash (Shift) to dodge enemies!",
        "💡 Charge your jump for extra height!",
        "💡 Different enemies require different strategies.",
        "💡 Explore every corner for hidden orbs!",
        "💡 Stay alive as long as possible!",
        "💡 Upgrade Speed, Jump, and Fire Rate at the shop!"
    };

    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite[] backgroundSprites;

    private bool isLoadingComplete = false;
    private float startTime;

    void Start()
    {
        // Validate scene name
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("[LoadingScreen] No scene to load! Please set sceneToLoad in inspector.");
            return;
        }

        // Check if scene exists
        if (!SceneExists(sceneToLoad))
        {
            Debug.LogError($"[LoadingScreen] Scene '{sceneToLoad}' not found in build settings!");
            Debug.Log("[LoadingScreen] Make sure 'Latest one' is added to Build Settings!");
            return;
        }

        // Set random loading tip
        SetRandomTip();

        // Set random background
        SetRandomBackground();

        // Start loading
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // Start fade in
        if (fadeCanvasGroup != null)
            yield return StartCoroutine(FadeCanvas(0f, 1f, fadeDuration));

        // Start loading scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        startTime = Time.time;
        float progress = 0f;

        while (!asyncLoad.isDone)
        {
            // Calculate progress (0-1)
            progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Update UI
            UpdateProgressUI(progress);

            // Check if loading is complete and minimum time has passed
            if (asyncLoad.progress >= 0.9f && Time.time - startTime >= minLoadTime)
            {
                // Start fade out
                if (fadeCanvasGroup != null)
                    yield return StartCoroutine(FadeCanvas(1f, 0f, fadeDuration));

                // Activate scene
                asyncLoad.allowSceneActivation = true;
                isLoadingComplete = true;
            }

            yield return null;
        }

        Debug.Log($"[LoadingScreen] Scene '{sceneToLoad}' loaded successfully!");
    }

    void UpdateProgressUI(float progress)
    {
        // Update slider
        if (progressSlider != null)
            progressSlider.value = progress;

        // Update text
        if (progressText != null)
            progressText.text = Mathf.RoundToInt(progress * 100) + "%";

        // Change text color based on progress
        if (progressText != null)
        {
            if (progress < 0.3f)
                progressText.color = Color.red;
            else if (progress < 0.7f)
                progressText.color = Color.yellow;
            else
                progressText.color = Color.green;
        }
    }

    IEnumerator FadeCanvas(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        fadeCanvasGroup.alpha = toAlpha;
    }

    void SetRandomTip()
    {
        if (loadingTipText == null || loadingTips == null || loadingTips.Length == 0) return;

        int randomIndex = Random.Range(0, loadingTips.Length);
        loadingTipText.text = loadingTips[randomIndex];
    }

    void SetRandomBackground()
    {
        if (backgroundImage == null || backgroundSprites == null || backgroundSprites.Length == 0) return;

        int randomIndex = Random.Range(0, backgroundSprites.Length);
        backgroundImage.sprite = backgroundSprites[randomIndex];
    }

    bool SceneExists(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
                return true;
        }
        return false;
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Set the scene to load
    /// </summary>
    public void SetSceneToLoad(string sceneName)
    {
        sceneToLoad = sceneName;
    }

    /// <summary>
    /// Set custom loading tips
    /// </summary>
    public void SetLoadingTips(string[] tips)
    {
        loadingTips = tips;
        SetRandomTip();
    }

    /// <summary>
    /// Skip loading and go directly to scene (for testing)
    /// </summary>
    public void SkipLoading()
    {
        if (!isLoadingComplete)
        {
            Debug.Log("[LoadingScreen] Skipping loading...");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    // ===== DEBUG =====
    [ContextMenu("Reload with Random Tip")]
    void ReloadRandomTip()
    {
        SetRandomTip();
    }
}