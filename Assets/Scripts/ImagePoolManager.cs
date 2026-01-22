using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Verwaltet die Bild-Pool Ansicht im Menü
/// Zeigt alle hochgeladenen Bilder an und ermöglicht Löschen
/// OPTIONAL: Nur wenn du ein Untermenü für die Pool-Verwaltung brauchst
/// </summary>
public class ImagePoolManager : MonoBehaviour
{
    [Header("UI Referenzen")]
    [Tooltip("Container für die Bild-Grid (sollte GridLayoutGroup haben)")]
    public Transform imageGridContainer;

    [Tooltip("Prefab für ein einzelnes Bild-Item (sollte Image + Delete-Button haben)")]
    public GameObject imageItemPrefab;

    [Tooltip("Text für Anzahl Bilder")]
    public Text imageCountText;

    [Header("Optional: Bestätigungsdialog")]
    [Tooltip("Panel für Lösch-Bestätigung")]
    public GameObject deleteConfirmationPanel;
    
    public Text deleteConfirmationText;
    private string pendingDeleteImageId;

    void OnEnable()
    {
        RefreshImageGrid();
    }

    /// <summary>
    /// Aktualisiert das Grid mit allen Bildern aus dem Pool
    /// </summary>
    public void RefreshImageGrid()
    {
        if (ImageManager.Instance == null)
        {
            Debug.LogError("ImageManager nicht gefunden!");
            return;
        }

        // Lösche alte Items
        foreach (Transform child in imageGridContainer)
        {
            Destroy(child.gameObject);
        }

        // Erstelle Items für jedes Bild
        List<ImageManager.PoolImage> images = ImageManager.Instance.imagePool;
        
        if (images == null || images.Count == 0)
        {
            UpdateImageCount(0);
            // Optional: Zeige "Keine Bilder vorhanden" Nachricht
            return;
        }

        foreach (ImageManager.PoolImage poolImage in images)
        {
            CreateImageItem(poolImage);
        }

        UpdateImageCount(images.Count);
    }

    /// <summary>
    /// Erstellt ein einzelnes Bild-Item im Grid
    /// </summary>
    void CreateImageItem(ImageManager.PoolImage poolImage)
    {
        if (imageItemPrefab == null)
        {
            Debug.LogError("imageItemPrefab ist nicht zugewiesen!");
            return;
        }

        GameObject item = Instantiate(imageItemPrefab, imageGridContainer);
        
        // Setze das Sprite
        Image imageComponent = item.GetComponent<Image>();
        if (imageComponent == null)
        {
            imageComponent = item.GetComponentInChildren<Image>();
        }

        if (imageComponent != null)
        {
            Sprite sprite = ImageManager.Instance.LoadPoolImageSprite(poolImage.imageId);
            if (sprite != null)
            {
                imageComponent.sprite = sprite;
            }
        }

        // Finde Delete-Button (sollte "DeleteButton" heißen)
        Button deleteButton = item.GetComponentInChildren<Button>();
        if (deleteButton == null)
        {
            Transform deleteButtonTransform = item.transform.Find("DeleteButton");
            if (deleteButtonTransform != null)
            {
                deleteButton = deleteButtonTransform.GetComponent<Button>();
            }
        }

        if (deleteButton != null)
        {
            string imageId = poolImage.imageId; // Capture für Lambda
            deleteButton.onClick.AddListener(() => OnDeleteImageClick(imageId));
        }

        // Optional: Setze Bild-Name als Text
        Text nameText = item.GetComponentInChildren<Text>();
        if (nameText != null)
        {
            nameText.text = poolImage.imageName;
        }
    }

    /// <summary>
    /// Wird aufgerufen wenn der Upload-Button geklickt wird
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
                Debug.Log($"{addedImageIds.Count} Bilder hinzugefügt");
                RefreshImageGrid();
            }
        });
    }

    /// <summary>
    /// Wird aufgerufen wenn ein Löschen-Button geklickt wird
    /// </summary>
    void OnDeleteImageClick(string imageId)
    {
        if (deleteConfirmationPanel != null)
        {
            // Zeige Bestätigungsdialog
            pendingDeleteImageId = imageId;
            deleteConfirmationPanel.SetActive(true);
            
            if (deleteConfirmationText != null)
            {
                ImageManager.PoolImage img = ImageManager.Instance.GetPoolImage(imageId);
                string imageName = img != null ? img.imageName : "dieses Bild";
                deleteConfirmationText.text = $"Möchtest du '{imageName}' wirklich löschen?";
            }
        }
        else
        {
            // Lösche direkt ohne Bestätigung
            DeleteImage(imageId);
        }
    }

    /// <summary>
    /// Bestätigt das Löschen (von Bestätigungsdialog)
    /// </summary>
    public void OnConfirmDelete()
    {
        if (!string.IsNullOrEmpty(pendingDeleteImageId))
        {
            DeleteImage(pendingDeleteImageId);
            pendingDeleteImageId = null;
        }

        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Bricht das Löschen ab
    /// </summary>
    public void OnCancelDelete()
    {
        pendingDeleteImageId = null;
        
        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Löscht ein Bild aus dem Pool
    /// </summary>
    void DeleteImage(string imageId)
    {
        if (ImageManager.Instance == null)
            return;

        ImageManager.Instance.RemoveImageFromPool(imageId);
        RefreshImageGrid();
        
        Debug.Log($"Bild {imageId} gelöscht");
    }

    /// <summary>
    /// Löscht ALLE Bilder aus dem Pool (mit Bestätigung!)
    /// </summary>
    public void OnClearAllImagesClick()
    {
        // TODO: Zeige Bestätigung "Wirklich ALLE Bilder löschen?"
        // Dann lösche alle:
        /*
        if (ImageManager.Instance != null && ImageManager.Instance.imagePool != null)
        {
            var imagesCopy = new List<ImageManager.PoolImage>(ImageManager.Instance.imagePool);
            foreach (var img in imagesCopy)
            {
                ImageManager.Instance.RemoveImageFromPool(img.imageId);
            }
            RefreshImageGrid();
        }
        */
        Debug.LogWarning("Clear All nicht implementiert - zu gefährlich ohne Bestätigung");
    }

    /// <summary>
    /// Aktualisiert die Anzahl-Anzeige
    /// </summary>
    void UpdateImageCount(int count)
    {
        if (imageCountText != null)
        {
            imageCountText.text = $"{count} Bilder im Pool";
        }
    }

    /// <summary>
    /// Schließt die Pool-Verwaltung
    /// </summary>
    public void OnCloseClick()
    {
        gameObject.SetActive(false);
    }
}
