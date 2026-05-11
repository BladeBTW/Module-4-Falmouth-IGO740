using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class UIButtonVisualSFX : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;
    public Sprite pressedSprite;
    public Sprite disabledSprite;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip highlightedSfx;
    public AudioClip pressedSfx;

    [Range(0f, 10f)]
    public float highlightedVolume = 1f;

    [Range(0f, 10f)]
    public float pressedVolume = 1f;

    [Header("Behavior")]
    public bool playHoverSoundOnlyOnceUntilExit = true;
    public bool useSelectedAsHighlighted = true;

    private Image image;
    private Button button;

    private bool pointerInside;
    private bool isPressed;
    private bool hoverSoundPlayed;

    private void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();

        if (normalSprite == null && image != null)
            normalSprite = image.sprite;

        ApplyCurrentSprite();
    }

    private void OnEnable()
    {
        pointerInside = false;
        isPressed = false;
        hoverSoundPlayed = false;

        ApplyCurrentSprite();
    }

    private void OnDisable()
    {
        pointerInside = false;
        isPressed = false;
        hoverSoundPlayed = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        pointerInside = true;
        isPressed = false;

        PlayHoverSfx();
        ApplyCurrentSprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        isPressed = false;
        hoverSoundPlayed = false;

        ApplyCurrentSprite();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isPressed = true;

        PlayPressedSfx();
        ApplyCurrentSprite();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        isPressed = false;
        ApplyCurrentSprite();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!useSelectedAsHighlighted)
            return;

        if (!IsInteractable())
            return;

        pointerInside = true;

        PlayHoverSfx();
        ApplyCurrentSprite();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!useSelectedAsHighlighted)
            return;

        pointerInside = false;
        isPressed = false;
        hoverSoundPlayed = false;

        ApplyCurrentSprite();
    }

    private void ApplyCurrentSprite()
    {
        if (image == null)
            return;

        if (!IsInteractable())
        {
            if (disabledSprite != null)
                image.sprite = disabledSprite;
            else if (normalSprite != null)
                image.sprite = normalSprite;

            return;
        }

        if (isPressed && pressedSprite != null)
        {
            image.sprite = pressedSprite;
            return;
        }

        if (pointerInside && highlightedSprite != null)
        {
            image.sprite = highlightedSprite;
            return;
        }

        if (normalSprite != null)
            image.sprite = normalSprite;
    }

    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private void PlayHoverSfx()
    {
        if (highlightedSfx == null)
            return;

        if (playHoverSoundOnlyOnceUntilExit && hoverSoundPlayed)
            return;

        PlaySound(highlightedSfx, highlightedVolume);
        hoverSoundPlayed = true;
    }

    private void PlayPressedSfx()
    {
        if (pressedSfx == null)
            return;

        PlaySound(pressedSfx, pressedVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (volume <= 0f)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
            return;
        }

        GameObject tempAudio = new GameObject("Temp UI SFX");
        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();

        tempSource.playOnAwake = false;
        tempSource.loop = false;
        tempSource.spatialBlend = 0f;
        tempSource.volume = volume;

        tempSource.PlayOneShot(clip, 1f);

        Destroy(tempAudio, clip.length + 0.1f);
    }
}