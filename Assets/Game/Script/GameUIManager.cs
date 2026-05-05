using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Overlay Canvas")]
    public GameObject menuVideoCanvas;
    public GameObject blackBackground;

    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
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
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Time.timeScale = 1f;
        _isPaused = false;
        _gameEnded = false;

        HideAllOverlayUI();
        ClearSelectionNextFrame();
    }

    private void Update()
    {
        if (_gameEnded)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (_gameEnded)
            return;

        _isPaused = true;
        Time.timeScale = 0f;

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameFinishedPanel != null)
            gameFinishedPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        ClearSelectionNextFrame();
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        HideAllOverlayUI();
        ClearSelectionNextFrame();
    }

    public void ShowGameOver()
    {
        _gameEnded = true;
        _isPaused = false;

        Time.timeScale = 0f;

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
            Debug.LogError("[GameUIManager] Game Over Panel is not assigned.");

        ClearSelectionNextFrame();
    }

    public void ShowGameFinished()
    {
        _gameEnded = true;
        _isPaused = false;

        Time.timeScale = 0f;

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
            Debug.LogError("[GameUIManager] Game Finished Panel is not assigned.");

        ClearSelectionNextFrame();
    }

    public void OnButtonResume()
    {
        ResumeGame();
    }

    public void OnButtonRestart()
    {
        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        HideAllOverlayUI();
        ClearSelectionNow();

        if (!string.IsNullOrEmpty(gameplaySceneName))
            SceneManager.LoadScene(gameplaySceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnButtonMainMenu()
    {
        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        HideAllOverlayUI();
        ClearSelectionNow();

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogError("[GameUIManager] Main Menu Scene Name is empty.");
    }

    public void OnButtonRehatch()
    {
        Time.timeScale = 1f;
        _gameEnded = false;
        _isPaused = false;

        HideAllOverlayUI();
        ClearSelectionNow();

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayRehatchSequence();
        }
        else
        {
            Debug.LogWarning("[GameUIManager] No CutsceneManager found. Reloading gameplay scene as fallback.");

            if (!string.IsNullOrEmpty(gameplaySceneName))
                SceneManager.LoadScene(gameplaySceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

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

    private void ClearSelectionNow()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ClearSelectionNextFrame()
    {
        StartCoroutine(ClearSelectionRoutine());
    }

    private IEnumerator ClearSelectionRoutine()
    {
        yield return null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}