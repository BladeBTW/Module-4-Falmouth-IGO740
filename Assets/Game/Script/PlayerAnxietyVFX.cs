using System.Collections.Generic;
using UnityEngine;

public class PlayerAnxietyVFX : MonoBehaviour
{
    [Header("VFX")]
    public GameObject anxietyVfxPrefab;
    public Transform spawnPoint;
    public Vector3 localOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 rotationEuler = Vector3.zero;
    public Vector3 vfxScale = Vector3.one;
    public bool parentToPlayer = true;

    private GameObject activeVfx;
    private readonly HashSet<object> activeSources = new HashSet<object>();

    public void SetAnxietyVFXActive(object source, bool active)
    {
        if (source == null)
            return;

        if (active)
            activeSources.Add(source);
        else
            activeSources.Remove(source);

        bool shouldShow = activeSources.Count > 0;

        if (shouldShow)
            StartVFX();
        else
            StopVFX();
    }

    private void StartVFX()
    {
        if (activeVfx != null)
            return;

        if (anxietyVfxPrefab == null)
            return;

        Transform origin = spawnPoint != null ? spawnPoint : transform;

        Vector3 pos = origin.position + origin.TransformDirection(localOffset);
        Quaternion rot = Quaternion.Euler(rotationEuler);

        Transform parent = parentToPlayer ? origin : null;

        activeVfx = Instantiate(anxietyVfxPrefab, pos, rot, parent);
        activeVfx.transform.localScale = vfxScale;

        ParticleSystem[] particles = activeVfx.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem ps in particles)
            ps.Play(true);
    }

    private void StopVFX()
    {
        if (activeVfx == null)
            return;

        ParticleSystem[] particles = activeVfx.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem ps in particles)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        Destroy(activeVfx, 2f);
        activeVfx = null;
    }

    private void OnDisable()
    {
        activeSources.Clear();

        if (activeVfx != null)
            Destroy(activeVfx);
    }
}