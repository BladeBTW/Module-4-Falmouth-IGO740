using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Panels")]
    [Tooltip("Pause menu panel (shown when pressing ESC).")]
    public GameObject pausePanel;

    [Tooltip("Game Over panel (Re-Hatch / Chicken Out / Main Menu).")]
    public GameObject gameOverPanel;

    [Tooltip("Optional: Game Finished panel, only used as a fallback if no CutsceneManager is present.")]
    public GameObject gameFinishedPanel;

    [Header("Scenes")]
    [Tooltip("Name of your main menu scene.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Optional: name of your gameplay scene. If empty, Restart reloads current scene.")]
    public string gameplaySceneName = "";

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
        Debug.Log("[GameUIManager] Awake, instance set.");

        if (pausePanel != null)        pausePanel.SetActive(false);
        if (gameOverPanel != null)     gameOverPanel.SetActive(false);
        if (gameFinishedPanel != null) gameFinishedPanel.SetActive(false);

        Time.timeScale = 1f;
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

    // ───── PAUSE LOGIC ─────

    public void PauseGame()
    {
        if (_gameEnded)
            return;

        Debug.Log("[GameUIManager] PauseGame()");

        _isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            Debug.Log($"[GameUIManager] Activating pausePanel: {pausePanel.name}");
            pausePanel.SetActive(true);

            // Make sure its parent canvas is active
            Canvas parentCanvas = pausePanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                if (!parentCanvas.gameObject.activeSelf)
                {
                    Debug.Log($"[GameUIManager] Enabling parent canvas for pause: {parentCanvas.name}");
                    parentCanvas.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning("[GameUIManager] pausePanel has NO parent Canvas. " +
                                 "You may have assigned a prefab instead of a scene object.");
            }
        }
        else
        {
            Debug.LogWarning("[GameUIManager] pausePanel is not assigned.");
        }

        // Safety net: turn on all canvases so nothing is hidden
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in allCanvases)
        {
            if (!c.gameObject.activeSelf)
            {
                Debug.Log($"[GameUIManager] Forcing Canvas ON (pause): {c.name}");
                c.gameObject.SetActive(true);
            }
        }
    }

    public void ResumeGame()
    {
        Debug.Log("[GameUIManager] ResumeGame()");

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            Debug.Log("[GameUIManager] Hiding pausePanel.");
            pausePanel.SetActive(false);
        }
    }

    // ───── GAME OVER / FINISHED ─────

    public void ShowGameOver()
    {
        Debug.Log("[GameUIManager] ShowGameOver() called.");

        _gameEnded = true;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            Debug.Log("[GameUIManager] Hiding pausePanel (on Game Over).");
            pausePanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            Debug.Log($"[GameUIManager] Activating gameOverPanel: {gameOverPanel.name}");
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[GameUIManager] gameOverPanel is NOT assigned!");
        }

        // Safety net: turn on ALL canvases so nothing is hidden
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in allCanvases)
        {
            if (!c.gameObject.activeSelf)
            {
                Debug.Log($"[GameUIManager] Forcing Canvas ON (game over): {c.name}");
                c.gameObject.SetActive(true);
            }
        }

        // We do NOT change Time.timeScale here, to avoid freezing the game.
    }

    public void ShowGameFinished()
    {
        Debug.Log("[GameUIManager] ShowGameFinished() called.");

        _gameEnded = true;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            Debug.Log("[GameUIManager] Hiding pausePanel (on finished).");
            pausePanel.SetActive(false);
        }

        if (gameFinishedPanel != null)
        {
            Debug.Log($"[GameUIManager] Activating gameFinishedPanel: {gameFinishedPanel.name}");
            gameFinishedPanel.SetActive(true);
        }
        else if (gameOverPanel != null)
        {
            Debug.LogWarning("[GameUIManager] No gameFinishedPanel set, falling back to gameOverPanel.");
            gameOverPanel.SetActive(true);
        }

        // CutsceneManager handles pausing during videos; no Time.timeScale change here.
    }

    // ───── BUTTON HANDLERS ─────

    /// <summary>
    /// Pause screen: RESUME button.
    /// </summary>
    public void OnButtonResume()
    {
        Debug.Log("[GameUIManager] OnButtonResume()");
        ResumeGame();
    }

    /// <summary>
    /// Restart button (Pause / Game Over / Finished).
    /// Reloads the gameplay scene.
    /// </summary>
    public void OnButtonRestart()
    {
        Debug.Log("[GameUIManager] OnButtonRestart()");

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

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

    /// <summary>
    /// Main Menu button (Pause / Game Over / Finished).
    /// </summary>
    public void OnButtonMainMenu()
    {
        Debug.Log("[GameUIManager] OnButtonMainMenu()");

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[GameUIManager] mainMenuSceneName is not set.");
        }
    }

    /// <summary>
    /// Re-Hatch button on the Game Over screen.
    /// Plays the 1Hatch → Male/Female cutscene flow via CutsceneManager.
    /// </summary>
    public void OnButtonRehatch()
    {
        Debug.Log("[GameUIManager] OnButtonRehatch()");

        // Hide the normal GAME OVER UI before starting the hatch cutscene
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            Debug.Log("[GameUIManager] Hiding gameOverPanel before re-hatch.");
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayRehatchSequence();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No CutsceneManager found, falling back to restart.");
            OnButtonRestart();
        }
    }
}
