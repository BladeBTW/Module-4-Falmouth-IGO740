using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Overlay Canvas")]
    public GameObject overlayCanvas;

    [Header("Video UI Cleanup")]
    [Tooltip("The whole menu/video canvas, usually MenuVideoCanvas. This should stay ON for pause/death/win screens.")]
    public GameObject menuVideoCanvas;

    [Tooltip("The black background object used during videos.")]
    public GameObject blackBackground;

    [Tooltip("The actual VideoFrame object that can block your UI.")]
    public GameObject videoFrame;

    [Tooltip("Optional panel used during videos.")]
    public GameObject menuVideoPanel;

    [Tooltip("Optional VideoPlayer used by your cutscenes.")]
    public VideoPlayer videoPlayer;

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject gameFinishedPanel;

    [Tooltip("Panel used when the hatch sequence gives a male chick.")]
    public GameObject maleChickPanel;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "GameScene";

    [Header("Input")]
    public bool allowPauseInput = true;
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode alternatePauseKey = KeyCode.Return;

    [Header("Pause Settings")]
    public bool pauseUsesTimeScale = true;

    [Header("Re-Hatch")]
    [Tooltip("If ON, Re-Hatch goes to MainMenu and auto-starts the Hatch video sequence instead of loading GameScene directly.")]
    public bool reHatchPlaysHatchVideos = true;

    [Header("Debug")]
    public bool logButtonPresses = true;

    private bool isPaused;
    private bool isGameOver;
    private bool isGameFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameUIManager] Duplicate GameUIManager found. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideAllEndPanels();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        HideAllEndPanels();

        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!allowPauseInput)
            return;

        if (isGameOver || isGameFinished)
            return;

        if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(alternatePauseKey))
            TogglePause();
    }

    // ------------------------------------------------------------
    // PAUSE
    // ------------------------------------------------------------

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isGameOver || isGameFinished)
            return;

        if (logButtonPresses)
            Debug.Log("[GameUIManager] PauseGame", this);

        isPaused = true;

        HideOnlyVideoObjects();

        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (maleChickPanel != null)
            maleChickPanel.SetActive(false);

        if (pauseUsesTimeScale)
            Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] ResumeGame", this);

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseUsesTimeScale)
            Time.timeScale = 1f;
    }

    // ------------------------------------------------------------
    // GAME OVER / WIN / MALE CHICK
    // ------------------------------------------------------------

    public void ShowGameOver()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] ShowGameOver", this);

        isGameOver = true;
        isGameFinished = false;
        isPaused = false;

        HideOnlyVideoObjects();

        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (maleChickPanel != null)
            maleChickPanel.SetActive(false);

        if (pauseUsesTimeScale)
            Time.timeScale = 0f;
    }

    public void ShowMaleChickGameOver()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] ShowMaleChickGameOver", this);

        isGameOver = true;
        isGameFinished = false;
        isPaused = false;

        HideOnlyVideoObjects();

        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (maleChickPanel != null)
        {
            maleChickPanel.SetActive(true);
        }
        else if (gameOverPanel != null)
        {
            Debug.LogWarning("[GameUIManager] Male Chick Panel missing. Falling back to Game Over Panel.", this);
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[GameUIManager] Male Chick Panel and Game Over Panel are both missing.", this);
        }

        if (pauseUsesTimeScale)
            Time.timeScale = 0f;
    }

    public void ShowGameFinished()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] ShowGameFinished", this);

        isGameOver = false;
        isGameFinished = true;
        isPaused = false;

        HideOnlyVideoObjects();

        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(true);

        if (maleChickPanel != null)
            maleChickPanel.SetActive(false);

        if (pauseUsesTimeScale)
            Time.timeScale = 0f;
    }

    public void HideAllEndPanels()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (maleChickPanel != null)
            maleChickPanel.SetActive(false);
    }

    // ------------------------------------------------------------
    // VIDEO UI
    // ------------------------------------------------------------

    public void HideOnlyVideoObjects()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] HideOnlyVideoObjects", this);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
        }

        if (videoFrame != null)
            videoFrame.SetActive(false);

        if (menuVideoPanel != null)
            menuVideoPanel.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);

        // Do NOT disable menuVideoCanvas here.
        // Your pause/death/win/male chick panels live inside it.
    }

    public void HideVideoUI()
    {
        HideOnlyVideoObjects();
    }

    public void ShowVideoUI()
    {
        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (videoFrame != null)
            videoFrame.SetActive(true);

        if (menuVideoPanel != null)
            menuVideoPanel.SetActive(true);
    }

    // ------------------------------------------------------------
    // BUTTONS
    // ------------------------------------------------------------

    public void OnReHatchClicked()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] Re-Hatch clicked", this);

        Time.timeScale = 1f;

        if (reHatchPlaysHatchVideos)
        {
            ReHatchRequest.autoStartHatchVideos = true;

            if (logButtonPresses)
                Debug.Log("[GameUIManager] Re-Hatch will load MainMenu and auto-start Hatch videos.", this);

            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMainMenuClicked()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] Main Menu clicked", this);

        Time.timeScale = 1f;

        ReHatchRequest.autoStartHatchVideos = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitClicked()
    {
        if (logButtonPresses)
            Debug.Log("[GameUIManager] Quit clicked", this);

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnResumeClicked()
    {
        ResumeGame();
    }

    // ------------------------------------------------------------
    // OLD METHOD NAME COMPATIBILITY
    // ------------------------------------------------------------

    public void Restart()
    {
        OnReHatchClicked();
    }

    public void ReHatch()
    {
        OnReHatchClicked();
    }

    public void Rehatch()
    {
        OnReHatchClicked();
    }

    public void MainMenu()
    {
        OnMainMenuClicked();
    }

    public void Quit()
    {
        OnQuitClicked();
    }

    public void Resume()
    {
        OnResumeClicked();
    }

    public void GameOver()
    {
        ShowGameOver();
    }

    public void MaleChick()
    {
        ShowMaleChickGameOver();
    }

    public void GameFinished()
    {
        ShowGameFinished();
    }

    public void OnButtonRehatch()
    {
        OnReHatchClicked();
    }

    public void OnButtonReHatch()
    {
        OnReHatchClicked();
    }

    public void OnButtonRestart()
    {
        OnReHatchClicked();
    }

    public void OnButtonMainMenu()
    {
        OnMainMenuClicked();
    }

    public void OnButtonQuit()
    {
        OnQuitClicked();
    }

    public void OnButtonResume()
    {
        OnResumeClicked();
    }
}