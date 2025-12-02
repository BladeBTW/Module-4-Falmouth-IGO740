using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum SimpleEndType
{
    Death,
    NotHealthyEnough,
    NotHeavyEnough,
    BothFailed
}

public class SimpleGameOverUI : MonoBehaviour
{
    public static SimpleGameOverUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panelRoot;   // whole panel for the end screen
    public Image endImage;         // image shown on end screen

    [Header("Sprites")]
    public Sprite deathSprite;
    public Sprite notHealthySprite;
    public Sprite notHeavySprite;
    public Sprite bothFailedSprite; // optional, can reuse others if null

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowEnd(SimpleEndType endType)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (endImage != null)
        {
            switch (endType)
            {
                case SimpleEndType.Death:
                    endImage.sprite = deathSprite;
                    break;

                case SimpleEndType.NotHealthyEnough:
                    endImage.sprite = notHealthySprite;
                    break;

                case SimpleEndType.NotHeavyEnough:
                    endImage.sprite = notHeavySprite;
                    break;

                case SimpleEndType.BothFailed:
                    endImage.sprite = bothFailedSprite != null
                        ? bothFailedSprite
                        : notHealthySprite; // fallback
                    break;
            }
        }

        Time.timeScale = 0f; // pause game
    }

    // Button: Quit / Chicken Out
    public void OnQuitClicked()
    {
        // For editor you can comment this and load a menu scene instead
        Application.Quit();
    }

    // Button: Re-Hatch / Restart
    public void OnReHatchClicked()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
