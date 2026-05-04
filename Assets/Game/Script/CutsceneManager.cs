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
    public RenderTexture videoRenderTexture;

    [Header("StreamingAssets Video File Names")]
    public string hatchVideoFile = "1Hatch.mp4";
    public string femaleVideoFile = "2Female.mp4";
    public string maleVideoFile = "3Male.mp4";
    public string gatherVideoFile = "4Gather.mp4";
    public string luckyChickenVideoFile = "5LuckyChicken.mp4";

    [Header("UI References")]
    public GameObject gameHudCanvas;
    public GameObject gameOverPanel;
    public GameObject endGamePlayAgainPanel;
    public GameObject pausePanel;
    public GameObject blackBackground;

    [Header("Scenes")]
    public string gameplaySceneName = "GameScene";

    private bool _sequenceRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        HideCutscenePanels();

        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.source = VideoSource.Url;
        }
    }

    public void PlayRehatchSequence()
    {
        if (_sequenceRunning)
            return;

        if (videoPlayer == null)
        {
            Debug.LogError("[CutsceneManager] Missing VideoPlayer.");
            return;
        }

        StartCoroutine(RehatchSequenceRoutine());
    }

    private IEnumerator RehatchSequenceRoutine()
    {
        _sequenceRunning = true;

        Time.timeScale = 0f;

        HideCutscenePanels();

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = MuteNonVideoAudio(allAudio);

        yield return PlayVideoFromStreamingAssets(hatchVideoFile);

        bool isMale = Random.value < 0.5f;

        if (isMale)
        {
            Debug.Log("[CutsceneManager] Male chick path.");

            yield return PlayVideoFromStreamingAssets(maleVideoFile);

            if (DeathCounter.Instance != null)
                DeathCounter.Instance.RegisterDeath();

            RestoreAudio(allAudio, previousMute);

            Time.timeScale = 0f;

            if (videoCanvas != null)
                videoCanvas.SetActive(true);

            if (blackBackground != null)
                blackBackground.SetActive(true);

            HideCutscenePanels();

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            else
                Debug.LogError("[CutsceneManager] Game Over Panel is not assigned.");

            _sequenceRunning = false;
        }
        else
        {
            Debug.Log("[CutsceneManager] Female chick path.");

            yield return PlayVideoFromStreamingAssets(femaleVideoFile);

            RestoreAudio(allAudio, previousMute);

            HideCutscenePanels();

            // IMPORTANT:
            // Keep the black video canvas active while loading the gameplay scene.
            // This prevents the main menu from flashing for one frame.
            if (videoCanvas != null)
                videoCanvas.SetActive(true);

            if (blackBackground != null)
                blackBackground.SetActive(true);

            ClearVideoRenderTexture();

            Time.timeScale = 1f;

            yield return null;

            if (!string.IsNullOrEmpty(gameplaySceneName))
                SceneManager.LoadScene(gameplaySceneName);
            else
                Debug.LogError("[CutsceneManager] Gameplay Scene Name is empty.");

            _sequenceRunning = false;
        }
    }

    public void PlaySuccessSequence()
    {
        if (_sequenceRunning)
            return;

        if (videoPlayer == null)
        {
            Debug.LogError("[CutsceneManager] Missing VideoPlayer.");
            return;
        }

        StartCoroutine(SuccessSequenceRoutine());
    }

    private IEnumerator SuccessSequenceRoutine()
    {
        _sequenceRunning = true;

        Time.timeScale = 0f;

        HideCutscenePanels();

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = MuteNonVideoAudio(allAudio);

        yield return PlayVideoFromStreamingAssets(gatherVideoFile);
        yield return PlayVideoFromStreamingAssets(luckyChickenVideoFile);

        RestoreAudio(allAudio, previousMute);

        Time.timeScale = 0f;

        HideCutscenePanels();

        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(true);
        else
            Debug.LogError("[CutsceneManager] End Game Play Again Panel is not assigned.");

        _sequenceRunning = false;
    }

    private IEnumerator PlayVideoFromStreamingAssets(string fileName)
    {
        if (videoPlayer == null)
            yield break;

        string url = GetStreamingAssetsUrl(fileName);

        Debug.Log("[CutsceneManager] Preparing video: " + url);

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.isLooping = false;

        bool prepared = false;
        bool completed = false;
        bool failed = false;

        VideoPlayer.EventHandler prepareHandler = null;
        VideoPlayer.EventHandler completeHandler = null;
        VideoPlayer.ErrorEventHandler errorHandler = null;

        prepareHandler = (VideoPlayer vp) =>
        {
            prepared = true;
        };

        completeHandler = (VideoPlayer vp) =>
        {
            completed = true;
        };

        errorHandler = (VideoPlayer vp, string message) =>
        {
            failed = true;
            Debug.LogError("[CutsceneManager] Video error: " + message);
        };

        videoPlayer.prepareCompleted += prepareHandler;
        videoPlayer.loopPointReached += completeHandler;
        videoPlayer.errorReceived += errorHandler;

        videoPlayer.Prepare();

        float timer = 0f;
        float timeout = 15f;

        while (!prepared && !failed && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!prepared || failed)
        {
            Debug.LogError("[CutsceneManager] Failed to prepare video: " + fileName);
            CleanupVideoEvents(prepareHandler, completeHandler, errorHandler);
            yield break;
        }

        videoPlayer.Play();

        while (!completed && !failed)
            yield return null;

        CleanupVideoEvents(prepareHandler, completeHandler, errorHandler);
    }

    private void CleanupVideoEvents(
        VideoPlayer.EventHandler prepareHandler,
        VideoPlayer.EventHandler completeHandler,
        VideoPlayer.ErrorEventHandler errorHandler)
    {
        videoPlayer.prepareCompleted -= prepareHandler;
        videoPlayer.loopPointReached -= completeHandler;
        videoPlayer.errorReceived -= errorHandler;
    }

    private string GetStreamingAssetsUrl(string fileName)
    {
        string basePath = Application.streamingAssetsPath;

        if (!basePath.EndsWith("/"))
            basePath += "/";

        return basePath + fileName;
    }

    private void HideCutscenePanels()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (endGamePlayAgainPanel != null)
            endGamePlayAgainPanel.SetActive(false);
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

    private bool[] MuteNonVideoAudio(AudioSource[] allAudio)
    {
        bool[] previousMute = new bool[allAudio.Length];

        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] == null)
                continue;

            previousMute[i] = allAudio[i].mute;

            bool isVideoSource =
                videoPlayer != null &&
                (allAudio[i].gameObject == videoPlayer.gameObject ||
                 allAudio[i].transform.IsChildOf(videoPlayer.transform));

            if (!isVideoSource)
                allAudio[i].mute = true;
        }

        return previousMute;
    }

    private void RestoreAudio(AudioSource[] allAudio, bool[] previousMute)
    {
        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] != null)
                allAudio[i].mute = previousMute[i];
        }
    }
}