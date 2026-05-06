using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineFramingTransposer framingTransposer;
    private Vector3 originalTrackedOffset;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        Instance = this;

        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

        originalTrackedOffset = framingTransposer.m_TrackedObjectOffset;
    }

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float fade = 1f - Mathf.Clamp01(timer / duration);
            Vector2 random = Random.insideUnitCircle * strength * fade;

            framingTransposer.m_TrackedObjectOffset =
                originalTrackedOffset + new Vector3(random.x, random.y, 0f);

            yield return null;
        }

        framingTransposer.m_TrackedObjectOffset = originalTrackedOffset;
        shakeRoutine = null;
    }
}