using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.EventSystems;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("Video System")]
    public GameObject videoCanvas;
    public VideoPlayer videoPlayer;
    public RenderTexture videoRenderTexture;

    [Header("Video Blocking Objects")]
    [Tooltip("The black background used behind videos. Should be ON during videos, OFF during result panels.")]
    public GameObject blackBackground;

    [Tooltip("The RawImage / frame object that displays the video. Should be ON during videos, OFF during result panels.")]
    public GameObject videoFrame;

    [Tooltip("Optional parent panel for the video frame. Should be ON during videos, OFF during result panels.")]
    public GameObject menuVideoPanel;

    [Header("StreamingAssets Video File Names")]
    public string hatchVideoFile = "1Hatch.mp4";
    public string femaleVideoFile = "2Female.mp4";
    public string maleVideoFile = "3Male.mp4";
    public string gatherVideoFile = "4Gather.mp4";
    public string luckyChickenVideoFile = "5LuckyChicken.mp4";

    [Header("Main Menu Cleanup")]
    [Tooltip("Assign MainMenu Canvas here. This gets hidden during cutscenes and before loading GameScene to prevent menu flicker.")]
    public GameObject mainMenuCanvas;

    [Header("UI References")]
    public GameObject gameHudCanvas;
    public GameObject gameOverPanel;
    public GameObject maleChickPanel;
    public GameObject endGamePlayAgainPanel;
    public GameObject pausePanel;

    [Header("Skip Button Visuals")]
    public GameObject skipButtonNormal;
    public GameObject skipButtonPressed;

    [Header("Skip SFX")]
    [Tooltip("Optional AudioSource for skip sounds. If empty, a temporary 2D AudioSource is created.")]
    public AudioSource skipAudioSource;

    [Tooltip("SFX played on the first click, when skip becomes armed.")]
    public AudioClip skipFirstClickSfx;

    [Tooltip("SFX played on the second click, when the video actually skips.")]
    public AudioClip skipSecondClickSfx;

    [Range(0f, 10f)]
    public float skipFirstClickVolume = 1f;

    [Range(0f, 10f)]
    public float skipSecondClickVolume = 1f;

    [Header("Scenes")]
    public string gameplaySceneName = "GameScene";

    [Header("Skip Settings")]
    public bool allowDoublePressSkip = true;

    [Header("Debug")]
    public bool logDebug = true;

    private bool _sequenceRunning;
    private bool _videoIsPlaying;
    private bool _skipArmed;
    private bool _skipRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoFindReferencesIfMissing();

        HideCutscenePanels();
        HideVideoBlockers();

        if (videoCanvas != null)
            videoCanvas.SetActive(false);

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(true);

        SetSkipVisual(false, false);

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.source = VideoSource.Url;
        }

        ClearSelection();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_videoIsPlaying || !allowDoublePressSkip)
            return;

        bool pressed =
            Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (!pressed)
            return;

        if (!_skipArmed)
        {
            _skipArmed = true;
            SetSkipVisual(true, true);
            PlaySkipSFX(skipFirstClickSfx, skipFirstClickVolume);

            if (logDebug)
                Debug.Log("[CutsceneManager] Skip armed. Press again to skip video.", this);
        }
        else
        {
            _skipRequested = true;
            PlaySkipSFX(skipSecondClickSfx, skipSecondClickVolume);

            if (logDebug)
                Debug.Log("[CutsceneManager] Skip requested.", this);
        }

        ClearSelection();
    }

    // ------------------------------------------------------------
    // PUBLIC BUTTON / COMPATIBILITY METHODS
    // ------------------------------------------------------------

    public void PlayRehatchSequence()
    {
        StartHatchSequence();
    }

    public void OnButtonHatch()
    {
        StartHatchSequence();
    }

    public void StartHatchVideos()
    {
        StartHatchSequence();
    }

    public void StartHatchVideoSequence()
    {
        StartHatchSequence();
    }

    public void PlayHatchVideos()
    {
        StartHatchSequence();
    }

    public void PlayHatchVideoSequence()
    {
        StartHatchSequence();
    }

    public void Hatch()
    {
        StartHatchSequence();
    }

    public void StartGame()
    {
        StartHatchSequence();
    }

    private void StartHatchSequence()
    {
        if (_sequenceRunning)
        {
            if (logDebug)
                Debug.Log("[CutsceneManager] Hatch sequence already running.", this);

            return;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[CutsceneManager] Missing VideoPlayer.", this);
            return;
        }

        StartCoroutine(RehatchSequenceRoutine());
    }

    public void PlaySuccessSequence()
    {
        if (_sequenceRunning)
            return;

        if (videoPlayer == null)
        {
            Debug.LogError("[CutsceneManager] Missing VideoPlayer.", this);
            return;
        }

        StartCoroutine(SuccessSequenceRoutine());
    }

    // ------------------------------------------------------------
    // HATCH / RE-HATCH SEQUENCE
    // ------------------------------------------------------------

    private IEnumerator RehatchSequenceRoutine()
    {
        _sequenceRunning = true;

        Time.timeScale = 0f;

        HideMainMenuBeforeVideo();
        HideCutscenePanels();

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        ShowVideoOverlay();
        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = MuteNonVideoAudio(allAudio);

        yield return PlayVideoFromStreamingAssets(hatchVideoFile);

        bool isMale = Random.value < 0.5f;

        if (isMale)
        {
            if (logDebug)
                Debug.Log("[CutsceneManager] Male chick path.", this);

            yield return PlayVideoFromStreamingAssets(maleVideoFile);

            if (DeathCounter.Instance != null)
                DeathCounter.Instance.RegisterDeath();

            RestoreAudio(allAudio, previousMute);

            Time.timeScale = 0f;

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowMaleChickGameOver();
            }
            else if (maleChickPanel != null)
            {
                ShowResultOverlayWithoutVideoBlockers(maleChickPanel);
            }
            else if (gameOverPanel != null)
            {
                Debug.LogWarning(
                    "[CutsceneManager] Male Chick Panel missing. Falling back to Game Over Panel.",
                    this
                );

                ShowResultOverlayWithoutVideoBlockers(gameOverPanel);
            }
            else
            {
                Debug.LogError(
                    "[CutsceneManager] No GameUIManager, no Male Chick Panel, and no Game Over Panel found.",
                    this
                );
            }

            _sequenceRunning = false;
            yield break;
        }

        if (logDebug)
            Debug.Log("[CutsceneManager] Female chick path.", this);

        yield return PlayVideoFromStreamingAssets(femaleVideoFile);

        RestoreAudio(allAudio, previousMute);

        PrepareImmediateGameplayLoad();

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            RunReset.StartNewRun();
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError("[CutsceneManager] Gameplay Scene Name is empty.", this);
        }

        _sequenceRunning = false;
    }

    // ------------------------------------------------------------
    // SUCCESS SEQUENCE
    // ------------------------------------------------------------

    private IEnumerator SuccessSequenceRoutine()
    {
        _sequenceRunning = true;

        Time.timeScale = 0f;

        HideMainMenuBeforeVideo();
        HideCutscenePanels();

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        ShowVideoOverlay();
        ClearVideoRenderTexture();

        AudioSource[] allAudio = FindObjectsOfType<AudioSource>();
        bool[] previousMute = MuteNonVideoAudio(allAudio);

        yield return PlayVideoFromStreamingAssets(gatherVideoFile);
        yield return PlayVideoFromStreamingAssets(luckyChickenVideoFile);

        RestoreAudio(allAudio, previousMute);

        Time.timeScale = 0f;

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowGameFinished();
        }
        else if (endGamePlayAgainPanel != null)
        {
            ShowResultOverlayWithoutVideoBlockers(endGamePlayAgainPanel);
        }
        else
        {
            Debug.LogError("[CutsceneManager] No GameUIManager and End Game Panel is not assigned.", this);
        }

        _sequenceRunning = false;
    }

    // ------------------------------------------------------------
    // VIDEO PLAYBACK
    // ------------------------------------------------------------

    private IEnumerator PlayVideoFromStreamingAssets(string fileName)
    {
        if (videoPlayer == null)
            yield break;

        _videoIsPlaying = false;
        _skipArmed = false;
        _skipRequested = false;
        SetSkipVisual(false, false);

        ShowVideoOverlay();
        ClearVideoRenderTexture();

        string url = GetStreamingAssetsUrl(fileName);

        if (logDebug)
            Debug.Log("[CutsceneManager] Preparing video: " + url, this);

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
            Debug.LogError("[CutsceneManager] Video error: " + message, this);
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
            Debug.LogError("[CutsceneManager] Failed to prepare video: " + fileName, this);

            CleanupVideoEvents(prepareHandler, completeHandler, errorHandler);

            _videoIsPlaying = false;
            _skipArmed = false;
            _skipRequested = false;
            SetSkipVisual(false, false);

            yield break;
        }

        videoPlayer.Play();

        _videoIsPlaying = true;
        _skipArmed = false;
        _skipRequested = false;
        SetSkipVisual(true, false);

        while (!completed && !failed && !_skipRequested)
            yield return null;

        if (_skipRequested && videoPlayer != null)
        {
            if (logDebug)
                Debug.Log("[CutsceneManager] Skipping video: " + fileName, this);

            videoPlayer.Stop();
        }

        _videoIsPlaying = false;
        _skipArmed = false;
        _skipRequested = false;
        SetSkipVisual(false, false);

        CleanupVideoEvents(prepareHandler, completeHandler, errorHandler);
        ClearSelection();
    }

    private void CleanupVideoEvents(
        VideoPlayer.EventHandler prepareHandler,
        VideoPlayer.EventHandler completeHandler,
        VideoPlayer.ErrorEventHandler errorHandler)
    {
        if (videoPlayer == null)
            return;

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

    // ------------------------------------------------------------
    // UI CLEANUP / DISPLAY
    // ------------------------------------------------------------

    private void HideMainMenuBeforeVideo()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);
    }

    private void PrepareImmediateGameplayLoad()
    {
        HideCutscenePanels();

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        if (gameHudCanvas != null)
            gameHudCanvas.SetActive(false);

        // Keep video cover visible until scene load begins.
        if (videoCanvas != null)
            videoCanvas.SetActive(true);

        if (blackBackground != null)
            blackBackground.SetActive(true);

        if (menuVideoPanel != null)
            menuVideoPanel.SetActive(true);

        if (videoFrame != null)
            videoFrame.SetActive(true);

        if (videoPlayer != null)
            videoPlayer.Stop();

        SetSkipVisual(false, false);
        ClearSelection();
    }

    private void ShowVideoOverlay()
    {
        if (videoCanvas != null)
        {
            ActivateParents(videoCanvas.transform);
            videoCanvas.SetActive(true);
        }

        if (blackBackground != null)
        {
            ActivateParents(blackBackground.transform);
            blackBackground.SetActive(true);
        }

        if (menuVideoPanel != null)
        {
            ActivateParents(menuVideoPanel.transform);
            menuVideoPanel.SetActive(true);
        }

        if (videoFrame != null)
        {
            ActivateParents(videoFrame.transform);
            videoFrame.SetActive(true);
        }
    }

    private void ShowResultOverlayWithoutVideoBlockers(GameObject panelToShow)
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoCanvas != null)
        {
            ActivateParents(videoCanvas.transform);
            videoCanvas.SetActive(true);
        }

        HideVideoBlockers();
        HideCutscenePanels();

        if (panelToShow != null)
        {
            ActivateParents(panelToShow.transform);
            panelToShow.SetActive(true);
        }

        SetSkipVisual(false, false);
        ClearVideoRenderTexture();
        ClearSelectionNextFrame();
    }

    private void HideVideoBlockers()
    {
        if (blackBackground != null)
            blackBackground.SetActive(false);

        if (videoFrame != null)
            videoFrame.SetActive(false);

        if (menuVideoPanel != null)
            menuVideoPanel.SetActive(false);
    }

    private void HideCutscenePanels()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (maleChickPanel != null)
            maleChickPanel.SetActive(false);

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

    private void ActivateParents(Transform child)
    {
        if (child == null)
            return;

        Transform current = child;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    // ------------------------------------------------------------
    // SKIP UI / SFX
    // ------------------------------------------------------------

    private void SetSkipVisual(bool visible, bool pressed)
    {
        if (skipButtonNormal != null)
            skipButtonNormal.SetActive(visible && !pressed);

        if (skipButtonPressed != null)
            skipButtonPressed.SetActive(visible && pressed);
    }

    private void PlaySkipSFX(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (volume <= 0f)
            return;

        if (skipAudioSource != null)
        {
            skipAudioSource.PlayOneShot(clip, volume);
            return;
        }

        GameObject tempAudio = new GameObject("Temp Skip SFX");
        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();

        tempSource.playOnAwake = false;
        tempSource.loop = false;
        tempSource.spatialBlend = 0f;
        tempSource.volume = volume;

        tempSource.PlayOneShot(clip, 1f);

        Destroy(tempAudio, clip.length + 0.1f);
    }

    // ------------------------------------------------------------
    // AUDIO HELPERS
    // ------------------------------------------------------------

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
                (
                    allAudio[i].gameObject == videoPlayer.gameObject ||
                    allAudio[i].transform.IsChildOf(videoPlayer.transform)
                );

            if (!isVideoSource)
                allAudio[i].mute = true;
        }

        return previousMute;
    }

    private void RestoreAudio(AudioSource[] allAudio, bool[] previousMute)
    {
        if (allAudio == null || previousMute == null)
            return;

        int count = Mathf.Min(allAudio.Length, previousMute.Length);

        for (int i = 0; i < count; i++)
        {
            if (allAudio[i] != null)
                allAudio[i].mute = previousMute[i];
        }
    }

    // ------------------------------------------------------------
    // EVENT SYSTEM HELPERS
    // ------------------------------------------------------------

    private void ClearSelection()
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
        ClearSelection();
    }

    // ------------------------------------------------------------
    // AUTO FIND
    // ------------------------------------------------------------

    private void AutoFindReferencesIfMissing()
    {
        if (mainMenuCanvas == null)
        {
            GameObject found = GameObject.Find("MainMenu Canvas");

            if (found != null)
                mainMenuCanvas = found;
        }

        if (videoCanvas == null)
        {
            GameObject found = GameObject.Find("MenuVideoCanvas");

            if (found != null)
                videoCanvas = found;
        }

        if (blackBackground == null)
        {
            GameObject found = GameObject.Find("BlackBackground");

            if (found != null)
                blackBackground = found;
        }

        if (videoFrame == null)
        {
            GameObject found = GameObject.Find("VideoFrame");

            if (found != null)
                videoFrame = found;
        }

        if (menuVideoPanel == null)
        {
            GameObject found = GameObject.Find("MenuVideoPanel");

            if (found != null)
                menuVideoPanel = found;
        }

        if (maleChickPanel == null)
        {
            GameObject found = GameObject.Find("MALE CHICK");

            if (found != null)
                maleChickPanel = found;
        }

        if (gameOverPanel == null)
        {
            GameObject found = GameObject.Find("YOU DIED");

            if (found != null)
                gameOverPanel = found;
        }

        if (endGamePlayAgainPanel == null)
        {
            GameObject found = GameObject.Find("GAME IS FINISHED");

            if (found != null)
                endGamePlayAgainPanel = found;
        }

        if (pausePanel == null)
        {
            GameObject found = GameObject.Find("Pause");

            if (found != null)
                pausePanel = found;
        }
    }
}