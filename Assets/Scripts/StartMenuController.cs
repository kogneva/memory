using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    void Awake()
    {
        // Punkt 8: Vereinfacht - ImageManager Singleton erstellt sich selbst in Awake()
        // Nur erstellen wenn wirklich nicht vorhanden
        if (ImageManager.Instance == null)
        {
            new GameObject("ImageManager").AddComponent<ImageManager>();
        }
    }

    public void OnStartClick()
    {
        SceneManager.LoadScene("MemoryGame");
    }

    public void OnUploadImagesClick()
    {
        // Punkt 8: Redundante Prüfung entfernt - Awake() garantiert dass ImageManager existiert
#if UNITY_EDITOR
        Debug.LogWarning("NativeGallery funktioniert nicht im Unity Editor!");
        Debug.LogWarning("Bitte baue für Android/iOS und teste auf einem Gerät.");
#else
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
