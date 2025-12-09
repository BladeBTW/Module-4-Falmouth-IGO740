using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Name of the gameplay scene to load if no cutscene is used.")]
    public string gameSceneName = "GameScene";

    [Header("UI")]
    [Tooltip("Text element that shows how many times you've died this playthrough.")]
    public TMP_Text deathCounterText;

    private void Start()
    {
        UpdateDeathCounterUI();
    }

    private void OnEnable()
    {
        UpdateDeathCounterUI();
    }

    private void UpdateDeathCounterUI()
    {
        if (deathCounterText == null)
            return;

        int deaths = (DeathCounter.Instance != null) ? DeathCounter.Instance.TotalDeaths : 0;
        deathCounterText.text = deaths.ToString();
    }

    /// <summary>
    /// Hatch button on main menu.
    /// Plays 1Hatch -> 2Female/3Male flow if CutsceneManager exists.
    /// If female: loads game scene; if male: menu game-over panel is shown.
    /// </summary>
    public void OnButtonHatch()
    {
        Time.timeScale = 1f;

        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayRehatchSequence();
        }
        else
        {
            // Fallback: no cutscene manager in this scene, just start game
            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("MainMenuUI: gameSceneName is not set.");
            }
        }
    }

    /// <summary>
    /// Quit / Chicken Out button.
    /// </summary>
    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
