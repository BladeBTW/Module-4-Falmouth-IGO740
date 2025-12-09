using UnityEngine;

public class DeathCounter : MonoBehaviour
{
    public static DeathCounter Instance { get; private set; }

    [Tooltip("Total number of deaths during this scene/session.")]
    public int TotalDeaths { get; private set; } = 0;

    private void Awake()
    {
        // Simple singleton, no persistence across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterDeath()
    {
        TotalDeaths++;
    }
}
