using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene loaded when pressing MainMenu buttons.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Main Menu Panels")]
    [Tooltip("Optional. Usually your MainMenu Canvas object.")]
    public GameObject mainMenuCanvas;

    [Tooltip("Optional. A child panel that contains logo/buttons, if you use one.")]
    public GameObject mainMenuPanel;

    [Header("Main Menu Buttons")]
    public Button hatchButton;
    public Button quitButton;

    [Header("Hatch Video Sequence")]
    [Tooltip("Optional extra UnityEvent. You can leave this empty if using Hatch Video Target below.")]
    public UnityEvent onHatchVideoSequenceRequested;

    [Tooltip("Drag CutsceneManager_Menu here.")]
    public GameObject hatchVideoTarget;

    [Tooltip("Exact public method name on CutsceneManager_Menu that starts the hatch videos.")]
    public string hatchVideoMethodName = "OnButtonHatch";

    [Tooltip("If true, prints possible public method names from Hatch Video Target if the method name is wrong.")]
    public bool logAvailableTargetMethods = true;

    [Header("Auto Re-Hatch")]
    [Tooltip("When Re-Hatch loads MainMenu, this auto-starts the hatch video sequence.")]
    public bool allowAutoStartFromReHatch = true;

    [Tooltip("Use 0 to avoid menu flicker before videos.")]
    public float autoStartDelay = 0f;

    [Header("Optional UI Cleanup")]
    [Tooltip("Assign MenuVideoCanvas. Keep this canvas ON; only its children get hidden.")]
    public GameObject menuVideoCanvas;

    [Tooltip("Assign MenuVideoCanvas > BlackBackground.")]
    public GameObject blackBackground;

    [Tooltip("Assign MenuVideoCanvas > VideoFrame.")]
    public GameObject videoFrame;

    [Tooltip("Assign MenuVideoCanvas > MenuVideoPanel.")]
    public GameObject menuVideoPanel;

    [Header("Quit")]
    public bool quitWorksInEditor = true;

    [Header("Debug")]
    public bool logActions = true;

    private bool hatchAlreadyStarted;

    private void Awake()
    {
        AutoFindReferencesIfMissing();

        bool shouldAutoStartHatch =
            allowAutoStartFromReHatch &&
            ReHatchRequest.autoStartHatchVideos;

        if (shouldAutoStartHatch)
        {
            // Hide immediately before the first visible frame.
            HideMainMenuForAutoHatch();
            KeepMenuVideoCanvasReadyButHidden();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        bool shouldAutoStartHatch =
            allowAutoStartFromReHatch &&
            ReHatchRequest.autoStartHatchVideos;

        if (shouldAutoStartHatch)
        {
            ReHatchRequest.autoStartHatchVideos = false;

            HideMainMenuForAutoHatch();
            KeepMenuVideoCanvasReadyButHidden();

            StartCoroutine(AutoStartHatchAfterDelay());
            return;
        }

        ShowFreshMainMenuState();
    }

    private IEnumerator AutoStartHatchAfterDelay()
    {
        if (autoStartDelay > 0f)
            yield return new WaitForSecondsRealtime(autoStartDelay);

        if (logActions)
            Debug.Log("[MainMenuUI] Auto-starting hatch videos from Re-Hatch request.", this);

        OnButtonHatch();
    }

    // ------------------------------------------------------------
    // HATCH / RE-HATCH
    // ------------------------------------------------------------

    public void OnButtonHatch()
    {
        Hatch();
    }

    public void OnHatchClicked()
    {
        Hatch();
    }

    public void Hatch()
    {
        if (hatchAlreadyStarted)
            return;

        hatchAlreadyStarted = true;

        // Hide menu visuals immediately so they never sit over video.
        HideMainMenuForAutoHatch();

        // Keep MenuVideoCanvas active so CutsceneManager can show videos.
        KeepMenuVideoCanvasReadyButHidden();

        bool startedVideo = TryStartHatchVideoSequence();

        if (!startedVideo)
        {
            hatchAlreadyStarted = false;
            ShowFreshMainMenuState();

            Debug.LogError(
                "[MainMenuUI] Hatch button was pressed, but no hatch video method was called. " +
                "Check Hatch Video Target and Hatch Video Method Name.",
                this
            );
        }
    }

    private bool TryStartHatchVideoSequence()
    {
        bool didStartSomething = false;

        if (onHatchVideoSequenceRequested != null &&
            onHatchVideoSequenceRequested.GetPersistentEventCount() > 0)
        {
            if (logActions)
                Debug.Log("[MainMenuUI] Invoking On Hatch Video Sequence Requested event.", this);

            onHatchVideoSequenceRequested.Invoke();
            didStartSomething = true;
        }

        if (hatchVideoTarget != null &&
            !string.IsNullOrWhiteSpace(hatchVideoMethodName))
        {
            bool calledMethod =
                TryCallMethodOnTarget(hatchVideoTarget, hatchVideoMethodName);

            if (calledMethod)
                didStartSomething = true;
        }

        return didStartSomething;
    }

    private bool TryCallMethodOnTarget(GameObject target, string methodName)
    {
        if (target == null)
            return false;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            MethodInfo method = behaviour.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                System.Type.EmptyTypes,
                null
            );

            if (method == null)
                continue;

            if (logActions)
            {
                Debug.Log(
                    $"[MainMenuUI] Calling hatch video method: {behaviour.GetType().Name}.{methodName}()",
                    behaviour
                );
            }

            method.Invoke(behaviour, null);
            return true;
        }

        Debug.LogError(
            $"[MainMenuUI] Could not find method '{methodName}()' on Hatch Video Target '{target.name}'.",
            target
        );

        if (logAvailableTargetMethods)
            LogAvailableMethods(target);

        return false;
    }

    private void LogAvailableMethods(GameObject target)
    {
        if (target == null)
            return;

        MonoBehaviour[] behaviours = target.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            MethodInfo[] methods = behaviour.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            string methodList = "";

            foreach (MethodInfo method in methods)
            {
                if (method.GetParameters().Length == 0 &&
                    method.DeclaringType == behaviour.GetType())
                {
                    methodList += method.Name + "()\n";
                }
            }

            Debug.Log(
                $"[MainMenuUI] Available no-argument methods on {behaviour.GetType().Name}:\n{methodList}",
                behaviour
            );
        }
    }

    // ------------------------------------------------------------
    // MAIN MENU BUTTON
    // ------------------------------------------------------------

    public void OnButtonMainMenu()
    {
        if (logActions)
            Debug.Log("[MainMenuUI] Main Menu clicked. Reloading MainMenu scene.", this);

        Time.timeScale = 1f;
        hatchAlreadyStarted = false;
        ReHatchRequest.autoStartHatchVideos = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnMainMenuClicked()
    {
        OnButtonMainMenu();
    }

    public void OnButtonMainMenuClicked()
    {
        OnButtonMainMenu();
    }

    public void MainMenu()
    {
        OnButtonMainMenu();
    }

    // ------------------------------------------------------------
    // FRESH MAIN MENU STATE
    // ------------------------------------------------------------

    private void ShowFreshMainMenuState()
    {
        hatchAlreadyStarted = false;

        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        RestoreMainMenuVisualChildren();
        SetMainButtonsVisible(true);
        KeepMenuVideoCanvasReadyButHidden();
    }

    private void HideMainMenuForAutoHatch()
    {
        if (hatchButton != null)
            hatchButton.gameObject.SetActive(false);

        if (quitButton != null)
            quitButton.gameObject.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        // Hide direct visual children of the MainMenu Canvas.
        // Do NOT disable mainMenuCanvas itself if this script lives on it.
        if (mainMenuCanvas != null)
        {
            for (int i = 0; i < mainMenuCanvas.transform.childCount; i++)
            {
                Transform child = mainMenuCanvas.transform.GetChild(i);

                if (child == null)
                    continue;

                child.gameObject.SetActive(false);
            }
        }
    }

    private void RestoreMainMenuVisualChildren()
    {
        if (mainMenuCanvas == null)
            return;

        for (int i = 0; i < mainMenuCanvas.transform.childCount; i++)
        {
            Transform child = mainMenuCanvas.transform.GetChild(i);

            if (child == null)
                continue;

            child.gameObject.SetActive(true);
        }
    }

    private void KeepMenuVideoCanvasReadyButHidden()
    {
        // Important:
        // Keep the root MenuVideoCanvas ON so CutsceneManager can activate/use it.
        // Only hide the visual blockers inside it.

        if (menuVideoCanvas != null)
            menuVideoCanvas.SetActive(true);

        if (menuVideoPanel != null)
            menuVideoPanel.SetActive(false);

        if (videoFrame != null)
            videoFrame.SetActive(false);

        if (blackBackground != null)
            blackBackground.SetActive(false);
    }

    private void SetMainButtonsVisible(bool visible)
    {
        if (hatchButton != null)
            hatchButton.gameObject.SetActive(visible);

        if (quitButton != null)
            quitButton.gameObject.SetActive(visible);
    }

    // ------------------------------------------------------------
    // QUIT
    // ------------------------------------------------------------

    public void OnButtonQuit()
    {
        Quit();
    }

    public void OnQuitClicked()
    {
        Quit();
    }

    public void Quit()
    {
        if (logActions)
            Debug.Log("[MainMenuUI] Quit clicked.", this);

        Time.timeScale = 1f;

#if UNITY_EDITOR
        if (quitWorksInEditor)
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------
    // OLD METHOD NAME COMPATIBILITY
    // ------------------------------------------------------------

    public void StartHatch()
    {
        Hatch();
    }

    public void StartGame()
    {
        Hatch();
    }

    public void OnButtonReHatch()
    {
        Hatch();
    }

    public void OnButtonRestart()
    {
        Hatch();
    }

    public void Restart()
    {
        Hatch();
    }

    public void ReHatch()
    {
        Hatch();
    }

    public void Rehatch()
    {
        Hatch();
    }

    // ------------------------------------------------------------
    // AUTO-FIND HELPERS
    // ------------------------------------------------------------

    private void AutoFindReferencesIfMissing()
    {
        if (mainMenuCanvas == null)
            mainMenuCanvas = gameObject;

        if (hatchButton == null)
        {
            Transform hatch =
                transform.Find("2 Buttons/Button_Hatch/Button_Prefab");

            if (hatch == null)
                hatch = transform.Find("2 Buttons/Button_Hatch");

            if (hatch != null)
                hatchButton = hatch.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            Transform quit =
                transform.Find("2 Buttons/Button_Quit/Button_Prefab");

            if (quit == null)
                quit = transform.Find("2 Buttons/Button_Quit");

            if (quit != null)
                quitButton = quit.GetComponent<Button>();
        }

        if (hatchVideoTarget == null)
        {
            GameObject found = GameObject.Find("CutsceneManager_Menu");

            if (found != null)
                hatchVideoTarget = found;
        }

        // These only auto-find if the objects are active.
        // If they are inactive, assign them manually in the Inspector.
        if (menuVideoCanvas == null)
        {
            GameObject found = GameObject.Find("MenuVideoCanvas");

            if (found != null)
                menuVideoCanvas = found;
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
    }
}