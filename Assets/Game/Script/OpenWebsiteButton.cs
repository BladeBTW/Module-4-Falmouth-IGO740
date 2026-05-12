using UnityEngine;

public class OpenWebsiteButton : MonoBehaviour
{
    [Header("Website")]
    [Tooltip("The website opened when this button is clicked.")]
    public string websiteUrl = "https://example.com";

    [Header("Debug")]
    public bool logOpen = true;

    public void OpenWebsite()
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            Debug.LogWarning("[OpenWebsiteButton] Website URL is empty.", this);
            return;
        }

        if (logOpen)
            Debug.Log("[OpenWebsiteButton] Opening website: " + websiteUrl, this);

        Application.OpenURL(websiteUrl);
    }
}