using UnityEngine;

public class DeathCounter : MonoBehaviour
{
    public static DeathCounter Instance { get; private set; }

    public int TotalDeaths { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterDeath()
    {
        TotalDeaths++;
        // Debug.Log($"[DeathCounter] TotalDeaths = {TotalDeaths}");
    }
}
