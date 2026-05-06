using UnityEngine;

public class ParticleAutoPlayTest : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Start()
    {
        if (ps == null)
            return;

        ps.Clear(true);
        ps.Play(true);

        Debug.Log("[ParticleAutoPlayTest] Particle system started: " + gameObject.name);
    }
}