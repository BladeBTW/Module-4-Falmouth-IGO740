using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Pause / Video Overlay")]
    [Tooltip("The full video/menu canvas used for pause, game over and cutscenes. Example: MenuVideoCanvas")]
    public GameObject menuVideoCanvas;

    [Tooltip("The black fullscreen background inside MenuVideoCanvas.")]
    public GameObject blackBackground;

    [Tooltip("Pause menu panel shown when pressing ESC.")]
    public GameObject pausePanel;

    [Header("Game Result Panels")]
    [Tooltip("Game Over panel with Re-Hatch / Main Menu buttons.")]
    public GameObject gameOverPanel;

    [Tooltip("Game Finished panel. Usually shown only after the success cutscene.")]
    public GameObject gameFinishedPanel;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "GameScene";

    private bool _isPaused = false;
    private bool _gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameUIManager] Duplicate instance found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _isPaused = false;
        _gameEnded = false;

        Time.timeScale = 1f;

        HideAllOverlayUI();

        Debug.Log("[GameUIManager] Awake complete.");
    }

    private void Update()
    {
        if (_gameEnded)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    // ───────────────── PAUSE LOGIC ─────────────────

    public void PauseGame()
    {
        if (_gameEnded)
            return;

        Debug.Log("[GameUIManager] PauseGame()");

        _isPaused = true;
        Time.timeScale = 0f;

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        Debug.Log("[GameUIManager] ResumeGame()");

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(false);
    }

    // ───────────────── GAME OVER / FINISHED ─────────────────

    public void ShowGameOver()
    {
        Debug.Log("[GameUIManager] ShowGameOver()");

        _gameEnded = true;
        _isPaused = false;

        Time.timeScale = 1f;

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        else
            Debug.LogError("[GameUIManager] gameOverPanel is not assigned.");
    }

    public void ShowGameFinished()
    {
        Debug.Log("[GameUIManager] ShowGameFinished()");

        _gameEnded = true;
        _isPaused = false;

        Time.timeScale = 1f;

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(true);
        else
            Debug.LogWarning("[GameUIManager] gameFinishedPanel is not assigned.");
    }

    // ───────────────── BUTTON HANDLERS ─────────────────

    public void OnButtonResume()
    {
        Debug.Log("[GameUIManager] OnButtonResume()");
        ResumeGame();
    }

    public void OnButtonRestart()
    {
        Debug.Log("[GameUIManager] OnButtonRestart()");

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        HideAllOverlayUI();

        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }

    public void OnButtonMainMenu()
    {
        Debug.Log("[GameUIManager] OnButtonMainMenu()");

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        HideAllOverlayUI();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogError("[GameUIManager] mainMenuSceneName is not set.");
    }

    public void OnButtonRehatch()
    {
        Debug.Log("[GameUIManager] OnButtonRehatch()");

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayRehatchSequence();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No CutsceneManager found. Restarting scene instead.");
            OnButtonRestart();
        }
    }

    // ───────────────── HELPERS ─────────────────

    private void HideAllOverlayUI()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(false);
    }
}