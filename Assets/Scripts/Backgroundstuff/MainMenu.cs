using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    public Button creditsButton;

    [Header("Settings")]
    public string gameSceneName = "Latest one";
    public string settingsSceneName = "Settings";
    public string creditsSceneName = "Credits";

    [Header("Animations")]
    public float fadeDuration = 0.5f;
    public CanvasGroup fadeCanvasGroup;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;

    private bool isLoading = false;

    public void Start()
    {
        // Setup buttons
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);

        // Setup click sounds automatically
        SetupButtonClickAudio(playButton);
        SetupButtonClickAudio(settingsButton);
        SetupButtonClickAudio(quitButton);
        SetupButtonClickAudio(creditsButton);

        // Start with fade in
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = false; // Prevents blocking button clicks!
            StartCoroutine(FadeIn());
        }

        // Ensure time scale is normal when returning from a paused game
        Time.timeScale = 1f;
        Debug.Log("Main Menu loaded successfully!");
    }

    public void SetupButtonClickAudio(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(() => PlaySound(buttonClickSound));
    }

    public void Update()
    {
        // Quick play with Enter key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isLoading) PlayGame();
        }

        // Quit with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    public void PlayGame()
    {
        if (isLoading) return;
        Debug.Log("Loading game...");

        string targetScene = SceneExists("LoadingScreen") ? "LoadingScreen" : gameSceneName;
        StartCoroutine(TransitionToScene(targetScene));
    }

    public void OpenSettings()
    {
        if (isLoading) return;
        Debug.Log("Opening settings...");

        if (SceneExists(settingsSceneName))
        {
            StartCoroutine(TransitionToScene(settingsSceneName));
        }
        else
        {
            Debug.LogWarning("Settings scene not found in Build Settings!");
        }
    }

    public void OpenCredits()
    {
        if (isLoading) return;
        Debug.Log("Opening credits...");

        if (SceneExists(creditsSceneName))
        {
            StartCoroutine(TransitionToScene(creditsSceneName));
        }
        else
        {
            Debug.LogWarning("Credits scene not found in Build Settings!");
        }
    }

    public void QuitGame()
    {
        if (isLoading) return;
        isLoading = true;
        Debug.Log("Quitting game...");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // NEW: Handles smooth fade-out BEFORE loading the next scene!
    public IEnumerator TransitionToScene(string sceneName)
    {
        isLoading = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true; // Block clicks during fade
            yield return StartCoroutine(FadeOut());
        }

        SceneManager.LoadScene(sceneName);
    }

    public bool SceneExists(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName) return true;
        }
        return false;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }

    public IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    // Hook these up using Unity UI "Event Trigger" component -> OnPointerEnter!
    public void OnPlayButtonHover() => PlaySound(buttonHoverSound);
    public void OnSettingsButtonHover() => PlaySound(buttonHoverSound);
    public void OnQuitButtonHover() => PlaySound(buttonHoverSound);
    public void OnCreditsButtonHover() => PlaySound(buttonHoverSound);
}