using System;
using UnityEngine;

public static class RunReset
{
    public static event Action OnNewRunStarted;

    public static int RunNumber { get; private set; }

    public static void StartNewRun()
    {
        RunNumber++;

        Debug.Log($"[RunReset] Starting new run #{RunNumber}. Resetting run-only state.");

        OnNewRunStarted?.Invoke();
    }
}