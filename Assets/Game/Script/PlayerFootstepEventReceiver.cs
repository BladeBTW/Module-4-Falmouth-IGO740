using UnityEngine;

public class PlayerFootstepEventReceiver : MonoBehaviour
{
    [Header("Spawned Footstep Particle")]
    public ParticleSystem footStepPrefab;
    public Transform spawnPoint;

    [Header("Settings")]
    public float destroyAfterSeconds = 2f;
    public Vector3 localSpawnOffset = new Vector3(0f, 0.2f, 0.5f);

    public void BurstFootStep()
    {
        if (footStepPrefab == null)
            return;

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Vector3 pos = origin.position + origin.TransformDirection(localSpawnOffset);

        ParticleSystem ps = Instantiate(footStepPrefab, pos, Quaternion.identity);
        ps.Play();

        Destroy(ps.gameObject, destroyAfterSeconds);
    }
}