using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    void Awake()
    {
        // Stelle sicher, dass ImageManager VOR Start() existiert
        EnsureImageManagerExists();
    }

    /// <summary>
    /// Stellt sicher, dass der ImageManager existiert
    /// </summary>
    void EnsureImageManagerExists()
    {
        if (ImageManager.Instance == null)
        {
            // Erstelle ImageManager GameObject wenn nicht vorhanden
            GameObject imageManagerObj = new GameObject("ImageManager");
            imageManagerObj.AddComponent<ImageManager>();
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
        // Stelle sicher dass ImageManager existiert
        if (ImageManager.Instance == null)
        {
            EnsureImageManagerExists();
            
            if (ImageManager.Instance == null)
            {
                Debug.LogError("ImageManager konnte nicht erstellt werden!");
                return;
            }
        }

#if UNITY_EDITOR
        // Im Unity Editor funktioniert NativeGallery nicht
        Debug.LogWarning("NativeGallery funktioniert nicht im Unity Editor!");
        Debug.LogWarning("Bitte baue für Android/iOS und teste auf einem Gerät.");
#else
        // Nur auf echten Geräten (Android/iOS) ausführen
        ImageManager.Instance.AddImagesToPool((addedImageIds) =>
        {
            if (addedImageIds != null && addedImageIds.Count > 0)
            {
                Debug.Log($"{addedImageIds.Count} Bilder zum Pool hinzugefügt");
            }
            else
            {
                Debug.Log("Keine Bilder ausgewählt");
            }
        });
#endif
    }


    public void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
