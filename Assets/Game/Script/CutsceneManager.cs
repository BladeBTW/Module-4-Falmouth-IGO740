using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("Video System")]
    public GameObject videoCanvas;
    public VideoPlayer videoPlayer;

    [Header("Render Texture")]
    public RenderTexture videoRenderTexture;

    [Header("Success Clips")]
    public VideoClip clipGather;
    public VideoClip clipLuckyChicken;

    [Header("Re-Hatch Clips")]
    public VideoClip clipHatch;
    public VideoClip clipFemale;
    public VideoClip clipMale;

    [Header("UI")]
    [Tooltip("The main in-game HUD canvas (or root Game object) to hide while videos are playing.")]
    public GameObject gameHudCanvas;          // <-- this should be Game Canvas/ Game
    public GameObject gameOverPanel;
    public GameObject endGamePlayAgainPanel;

    [Header("Scenes")]
    public string gameplaySceneName = "";

    private bool _sequenceRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Ensure the video canvas is hidden at start
        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        // Ensure the normal HUD is visible at start
        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(true);    // <--- IMPORTANT

        // Finished panel starts hidden
        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);
    }

    // ───────────────── SUCCESS SEQUENCE ─────────────────

    public void PlaySuccessSequence()
    {
        if (_sequenceRunning)
            return;

        if (videoPlayer == null || clipGather == null || clipLuckyChicken == null)
        {
            Debug.LogError("[CutsceneManager] Missing clips or VideoPlayer for success sequence.");
            return;
        }

        StartCoroutine(SuccessSequenceRoutine());
    }

    private IEnumerator SuccessSequenceRoutine()
    {
        _sequenceRunning = true;

        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Hide HUD during video
        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = new bool[allAudio.Length];

        for (int i = 0; i < allAudio.Length; i++)
        {
            previousMute[i] = allAudio[i].mute;

            bool isVideoSource = (videoPlayer != null &&
                                  (allAudio[i].gameObject == videoPlayer.gameObject ||
                                   allAudio[i].transform.IsChildOf(videoPlayer.transform)));

            if (!isVideoSource)
                allAudio[i].mute = true;
        }

        yield return PlayClipSequential(clipGather);
        yield return PlayClipSequential(clipLuckyChicken);

        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] != null)
                allAudio[i].mute = previousMute[i];
        }

        Time.timeScale = 0f;

        // Show "GAME IS FINISHED" buttons over video
        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(true);

        _sequenceRunning = false;
    }

    // ───────────────── RE-HATCH SEQUENCE ─────────────────

    public void PlayRehatchSequence()
    {
        if (_sequenceRunning)
            return;

        if (videoPlayer == null || clipHatch == null || clipFemale == null || clipMale == null)
        {
            Debug.LogError("[CutsceneManager] Missing clips or VideoPlayer for re-hatch sequence.");
            return;
        }

        StartCoroutine(RehatchSequenceRoutine());
    }

    private IEnumerator RehatchSequenceRoutine()
    {
        _sequenceRunning = true;

        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = new bool[allAudio.Length];

        for (int i = 0; i < allAudio.Length; i++)
        {
            previousMute[i] = allAudio[i].mute;

            bool isVideoSource = (videoPlayer != null &&
                                  (allAudio[i].gameObject == videoPlayer.gameObject ||
                                   allAudio[i].transform.IsChildOf(videoPlayer.transform)));

            if (!isVideoSource)
                allAudio[i].mute = true;
        }

        // 1) 1Hatch
        yield return PlayClipSequential(clipHatch);

        // 2) 50/50 Male / Female
        bool isMale = Random.value < 0.5f;

        if (isMale)
        {
            // 3Male: unlucky
            yield return PlayClipSequential(clipMale);

            if (DeathCounter.Instance != null)
                DeathCounter.Instance.RegisterDeath();

            for (int i = 0; i < allAudio.Length; i++)
                if (allAudio[i] != null) allAudio[i].mute = previousMute[i];

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
        else
        {
            // 2Female: lucky – go to game scene
            yield return PlayClipSequential(clipFemale);

            for (int i = 0; i < allAudio.Length; i++)
                if (allAudio[i] != null) allAudio[i].mute = previousMute[i];

            if (videoCanvas != null)
                videoCanvas.SetActive(false);

            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(gameplaySceneName))
                SceneManager.LoadScene(gameplaySceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        _sequenceRunning = false;
    }

    // ───────────────── HELPERS ─────────────────

    private IEnumerator PlayClipSequential(VideoClip clip)
    {
        if (clip == null || videoPlayer == null)
            yield break;

        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        while (!videoPlayer.isPlaying)
            yield return null;

        while (videoPlayer.isPlaying)
            yield return null;
    }

    private void ClearVideoRenderTexture()
    {
        if (videoRenderTexture == null)
            return;

        RenderTexture active = RenderTexture.active;
        RenderTexture.active = videoRenderTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = active;
    }
}
