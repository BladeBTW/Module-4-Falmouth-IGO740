using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("Video System")]
    [Tooltip("Canvas that contains the RawImage + VideoPlayer.")]
    public GameObject videoCanvas;
    public VideoPlayer videoPlayer;

    [Header("Render Texture")]
    [Tooltip("RenderTexture used by the VideoPlayer & RawImage.")]
    public RenderTexture videoRenderTexture;

    [Header("Success Clips (end of game)")]
    public VideoClip clipGather;        // 4Gather
    public VideoClip clipLuckyChicken;  // 5LuckyChicken

    [Header("Re-Hatch Clips (intro / rehatch)")]
    public VideoClip clipHatch;   // 1Hatch
    public VideoClip clipFemale;  // 2Female
    public VideoClip clipMale;    // 3Male

    [Header("UI References")]
    [Tooltip("Root of the in-game HUD (health/weight). Can be empty in Main Menu.")]
    public GameObject gameHudCanvas;

    [Tooltip("Game Over panel specifically for MALE chick path in this scene.")]
    public GameObject gameOverPanel; // e.g. GAME OVER HATCH in menu or game

    [Tooltip("Panel with 'Game is finished' buttons (Restart / Main Menu), shown after success.")]
    public GameObject endGamePlayAgainPanel;

    [Header("Scenes")]
    [Tooltip("Gameplay scene to load after female path from re-hatch.")]
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

        // Start with video UI hidden
        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        // HUD should start visible if present
        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);
    }

    // ───────────────── SUCCESS SEQUENCE (4 + 5) ─────────────────

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

        // Hide HUD while video plays
        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        ClearVideoRenderTexture();

        // Mute all non-video audio sources
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

        // 4Gather -> 5LuckyChicken
        yield return PlayClipSequential(clipGather);
        yield return PlayClipSequential(clipLuckyChicken);

        // Restore audio mutes
        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] != null)
                allAudio[i].mute = previousMute[i];
        }

        // Keep game paused until buttons are clicked
        Time.timeScale = 0f;

        // Show "GAME IS FINISHED" buttons over the video
        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(true);

        _sequenceRunning = false;
    }

    // ───────────────── RE-HATCH SEQUENCE (1 → 2/3) ─────────────────

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

        // Hide HUD & any existing panels
        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        ClearVideoRenderTexture();

        // Mute all non-video audio sources
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

        // 1) 1Hatch intro
        yield return PlayClipSequential(clipHatch);

        // 2) Randomly choose Male/Female
        bool isMale = Random.value < 0.5f;

        if (isMale)
        {
            // 3Male: unlucky
            yield return PlayClipSequential(clipMale);

            // Count as death if DeathCounter exists
            if (DeathCounter.Instance != null)
                DeathCounter.Instance.RegisterDeath();

            // Restore audio
            for (int i = 0; i < allAudio.Length; i++)
            {
                if (allAudio[i] != null)
                    allAudio[i].mute = previousMute[i];
            }

            // Show MALE death screen (GAME OVER HATCH) for this scene
            Time.timeScale = 0f; // optional, you can keep game paused while this menu is up

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
        else
        {
            // 2Female: lucky – go to gameplay scene
            yield return PlayClipSequential(clipFemale);

            // Restore audio
            for (int i = 0; i < allAudio.Length; i++)
            {
                if (allAudio[i] != null)
                    allAudio[i].mute = previousMute[i];
            }

            // Hide video and clear last frame before loading new scene
            if (videoCanvas != null)
                videoCanvas.SetActive(false);

            ClearVideoRenderTexture();

            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
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

        // Wait for it to actually start
        while (!videoPlayer.isPlaying)
            yield return null;

        // Wait until it finishes
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
