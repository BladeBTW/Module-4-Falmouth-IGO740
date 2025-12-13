using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ChickenAmbientCluck : MonoBehaviour
{
    [Header("Player Detection")]
    [Tooltip("Tag of the player object.")]
    public string playerTag = "Player";

    [Tooltip("Max distance at which you can hear this chicken clucking.")]
    public float activeRange = 10f;

    [Header("Cluck Sound")]
    [Tooltip("Looping cluck / idle sound for this chicken.")]
    public AudioClip cluckLoopClip;

    [Tooltip("1 = normal, 2 = loud, 5 = very loud, 10 = extreme.")]
    [Range(0f, 10f)]
    public float cluckVolume = 1f;

    private Transform _player;
    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();

        // Basic 3D audio setup
        _audio.spatialBlend = 1f;   // 3D sound
        _audio.playOnAwake = false;
        _audio.loop = true;
        _audio.clip = cluckLoopClip;
        _audio.volume = cluckVolume;
    }

    private void Update()
    {
        // Lazy-resolve player once
        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null)
                _player = p.transform;
            else
                return; // no player yet
        }

        float sqrRange = activeRange * activeRange;
        float sqrDist = (transform.position - _player.position).sqrMagnitude;

        bool playerInRange = sqrDist <= sqrRange;

        if (playerInRange)
        {
            if (!_audio.isPlaying && cluckLoopClip != null)
            {
                _audio.clip = cluckLoopClip;
                _audio.volume = cluckVolume;
                _audio.Play();
            }
        }
        else
        {
            if (_audio.isPlaying)
            {
                _audio.Stop();
            }
        }
    }

    // If you tweak volume at runtime in inspector, keep it synced
    private void OnValidate()
    {
        if (_audio != null)
            _audio.volume = cluckVolume;
    }
}
