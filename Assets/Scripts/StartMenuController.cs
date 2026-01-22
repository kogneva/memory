using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuController : MonoBehaviour
{
    [Header("Optional: UI Feedback")]
    [Tooltip("Text der die Anzahl Bilder im Pool anzeigt (z.B. 'Pool: 15 Bilder')")]
    public Text imagePoolCountText;

    [Tooltip("Optional: Feedback-Text für Upload-Bestätigung")]
    public Text uploadFeedbackText;

    void Start()
    {
        UpdateImagePoolCount();

        // Verstecke Feedback-Text am Anfang
        if (uploadFeedbackText != null)
        {
            uploadFeedbackText.gameObject.SetActive(false);
        }
    }

    public void OnStartClick()
    {
        SceneManager.LoadScene("MemoryGame");
    }

    /// <summary>
    /// Öffnet die Galerie und lädt mehrere Bilder in den Pool
    /// </summary>
    public void OnUploadImagesClick()
    {
        if (ImageManager.Instance == null)
        {
            Debug.LogError("ImageManager nicht gefunden!");
            return;
        }

        ImageManager.Instance.AddImagesToPool((addedImageIds) =>
        {
            if (addedImageIds != null && addedImageIds.Count > 0)
            {
                Debug.Log($"{addedImageIds.Count} Bilder zum Pool hinzugefügt");
                UpdateImagePoolCount();
                ShowUploadFeedback($"{addedImageIds.Count} Bilder hinzugefügt!");
            }
            else
            {
                Debug.Log("Keine Bilder ausgewählt");
            }
        });
    }

    /// <summary>
    /// Aktualisiert die Anzeige der Anzahl Bilder im Pool
    /// </summary>
    void UpdateImagePoolCount()
    {
        if (imagePoolCountText != null && ImageManager.Instance != null)
        {
            int count = ImageManager.Instance.imagePool != null
                ? ImageManager.Instance.imagePool.Count
                : 0;

            imagePoolCountText.text = $"Pool: {count} Bilder";
        }
    }

    /// <summary>
    /// Zeigt eine kurze Bestätigungsmeldung an
    /// </summary>
    void ShowUploadFeedback(string message)
    {
        if (uploadFeedbackText != null)
        {
            uploadFeedbackText.text = message;
            uploadFeedbackText.gameObject.SetActive(true);

            // Verstecke nach 3 Sekunden
            Invoke(nameof(HideUploadFeedback), 3f);
        }
    }

    void HideUploadFeedback()
    {
        if (uploadFeedbackText != null)
        {
            uploadFeedbackText.gameObject.SetActive(false);
        }
    }

    public void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
