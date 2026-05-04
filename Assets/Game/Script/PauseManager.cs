using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuVideoCanvas;
    public GameObject blackBackground;
    public GameObject pausePanel;

    private bool isPaused = false;

    void Start()
    {
        Resume(); // make sure game starts unpaused
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;

        Time.timeScale = 0f;

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(false);
    }
}