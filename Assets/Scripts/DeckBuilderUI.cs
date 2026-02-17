using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class DeckBuilderUI : MonoBehaviour
{
    [Header("Config Panel - Inputs")]
    public TMP_InputField deckNameInput;           
    public TMP_InputField groupCountInput;         
    public TMP_InputField groupSizeInput;          
    public TMP_InputField requiredForMatchInput;   
    public Toggle useSameImagesToggle;

    [Header("Config Panel - Display")]
    public TMP_Text modeDescriptionText;           
    public TMP_Text errorText;                     

    [Header("Assignment Panel - Display")]
    public TMP_Text progressText;                  
    public Slider progressBar;
    public TMP_Text groupInfoText;                 

    [Header("Assignment Panel - Containers")]
    public Transform selectedImagesContainer;
    public Transform availableImagesContainer;

    [Header("Prefabs")]
    public GameObject imageButtonPrefab;

    [Header("Panels")]
    public GameObject configPanel;
    public GameObject assignmentPanel;

    private DeckBuilder deckBuilder;
    private bool isClassicMode = true;

    void Awake()
    {
        if (deckBuilder == null)
        {
            deckBuilder = gameObject.AddComponent<DeckBuilder>();
            Debug.Log("DeckBuilder in Awake initialisiert");
        }
    }

    void Start()
    {
        if (ImageManager.Instance == null)
        {
            Debug.LogError("ImageManager.Instance ist null! Bitte stelle sicher, dass ImageManager in der Szene existiert.");
        }

        if (groupCountInput != null) groupCountInput.text = "5";
        if (groupSizeInput != null) groupSizeInput.text = "2";
        if (requiredForMatchInput != null) requiredForMatchInput.text = "2";

        if (useSameImagesToggle != null)
        {
            useSameImagesToggle.onValueChanged.AddListener((value) => {
                isClassicMode = value;
                UpdateModeDescription();
            });
        }

        UpdateModeDescription();
        ShowConfigPanel();
    }

    void ShowConfigPanel()
    {
        if (configPanel != null) configPanel.SetActive(true);
        if (assignmentPanel != null) assignmentPanel.SetActive(false);
    }

    void ShowAssignmentPanel()
    {
        if (configPanel != null) configPanel.SetActive(false);
        if (assignmentPanel != null) assignmentPanel.SetActive(true);
    }

    void UpdateModeDescription()
    {
        if (modeDescriptionText == null) return;

        if (isClassicMode)
            modeDescriptionText.text = "Klassisch: Das gleiche Bild für ein Paar/eine Gruppe";
        else
            modeDescriptionText.text = "Thematisch: Verschiedene Bilder bilden ein Paar/eine Gruppe";
    }

    public void OnStartClick()
    {
        Debug.Log("OnStartClick aufgerufen");

        if (deckBuilder == null)
        {
            Debug.LogError("DeckBuilder ist null!");
            if (errorText != null) errorText.text = "Fehler: DeckBuilder nicht initialisiert";
            return;
        }

        if (ImageManager.Instance == null)
        {
            Debug.LogError("ImageManager.Instance ist null!");
            if (errorText != null) errorText.text = "Fehler: ImageManager nicht gefunden";
            return;
        }

        if (deckNameInput == null)
        {
            Debug.LogError("deckNameInput ist null!");
            return;
        }

        if (string.IsNullOrWhiteSpace(deckNameInput.text))
        {
            if (errorText != null) errorText.text = "Bitte Deck-Namen eingeben";
            return;
        }

        if (!int.TryParse(groupCountInput.text, out int groupCount))
        {
            if (errorText != null) errorText.text = "Ungültige Gruppen-Anzahl";
            return;
        }

        if (!int.TryParse(groupSizeInput.text, out int groupSize))
        {
            if (errorText != null) errorText.text = "Ungültige Gruppen-Größe";
            return;
        }

        if (!int.TryParse(requiredForMatchInput.text, out int requiredForMatch))
        {
            if (errorText != null) errorText.text = "Ungültiger Match-Wert";
            return;
        }

        Debug.Log($"Starte Deck-Konfiguration: {deckNameInput.text}, Groups={groupCount}, Size={groupSize}, Match={requiredForMatch}");

        if (deckBuilder.StartDeckConfiguration(deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode))
        {
            Debug.Log("Deck-Konfiguration erfolgreich gestartet");
            ShowAssignmentPanel(); 
            UpdateProgressBar();
        }
        else
        {
            Debug.LogWarning("Deck-Konfiguration fehlgeschlagen");
            var config = new ImageManager.SimplifiedDeckConfig(
                deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode
            );
            var validation = ImageManager.Instance.ValidateSimplifiedDeck(config);
            if (errorText != null) errorText.text = validation.errorMessage;
        }
    }

    public void OnCancelClick()
    {
        if (deckBuilder != null)
        {
            deckBuilder.CancelDeckCreation();
        }
        ShowConfigPanel();
        if (errorText != null) errorText.text = "";
    }

    void UpdateProgressBar()
    {
        if (deckBuilder == null)
        {
            Debug.LogError("DeckBuilder ist null in UpdateProgressBar!");
            return;
        }

        var config = deckBuilder.GetCurrentConfig();
        if (config == null) 
        {
            Debug.LogWarning("Config ist null in UpdateProgressBar");
            return;
        }
        
        int currentIndex = deckBuilder.GetCurrentGroupIndex();

        if (progressText != null)
        {
            progressText.text = deckBuilder.GetProgressDescription();
            Debug.Log($"Progress Text aktualisiert: {progressText.text}");
        }
        else
        {
            Debug.LogWarning("progressText ist null!");
        }

        if (progressBar != null)
        {
            progressBar.value = deckBuilder.GetProgress();
        }

        if (groupInfoText != null)
        {
            if (isClassicMode)
            {
                groupInfoText.text = $"Wähle 1 Bild für Gruppe {currentIndex + 1}";
            }
            else
            {
                int selected = deckBuilder.GetCurrentGroupImages().Count;
                groupInfoText.text = $"Bilder: {selected}/{config.groupSize}";
            }
        }

        DisplayAvailableImages();
        DisplaySelectedImages();
    }

    void DisplayAvailableImages()
    {
        if (availableImagesContainer == null) return;
        if (deckBuilder == null) return;

        foreach (Transform child in availableImagesContainer)
        {
            Destroy(child.gameObject);
        }

        var availableImages = deckBuilder.GetAvailableImages();
        if (availableImages == null) return;

        foreach (var poolImage in availableImages)
        {
            CreateImageButton(poolImage, availableImagesContainer, true);
        }
    }

    void DisplaySelectedImages()
    {
        if (selectedImagesContainer == null) return;
        if (deckBuilder == null) return;

        foreach (Transform child in selectedImagesContainer)
        {
            Destroy(child.gameObject);
        }

        var selectedImageIds = deckBuilder.GetCurrentGroupImages();

        foreach (string imageId in selectedImageIds)
        {
            var poolImage = ImageManager.Instance?.GetPoolImage(imageId);
            if (poolImage != null)
            {
                CreateImageButton(poolImage, selectedImagesContainer, false);
            }
        }
    }

    void CreateImageButton(ImageManager.PoolImage poolImage, Transform parent, bool isAvailable)
    {
        if (imageButtonPrefab == null) return;

        GameObject item = Instantiate(imageButtonPrefab, parent);

        ImageButton imgBtn = item.GetComponent<ImageButton>();
        if (imgBtn != null)
        {
            Sprite sprite = ImageManager.Instance?.LoadPoolImageSprite(poolImage.imageId);
            imgBtn.Initialize(poolImage.imageId, sprite);

            string imageId = poolImage.imageId;

            if (isAvailable)
            {
                imgBtn.onImageClicked.AddListener((id) => {
                    if (deckBuilder != null && deckBuilder.AddImageToCurrentGroup(id))
                    {
                        UpdateProgressBar();
                    }
                });
            }
            else
            {
                imgBtn.onImageClicked.AddListener((id) => {
                    if (deckBuilder != null && deckBuilder.RemoveImageFromCurrentGroup(id))
                    {
                        UpdateProgressBar();
                    }
                });
            }
        }
    }

    public void OnBackClick()
    {
        if (deckBuilder != null && deckBuilder.PreviousGroup())
        {
            UpdateProgressBar();
        }
    }

    public void OnNextClick()
    {
        if (deckBuilder != null && deckBuilder.IsCurrentGroupComplete())
        {
            if (deckBuilder.NextGroup())
            {
                UpdateProgressBar();
            }
        }
    }

    public void OnFinishClick()
    {
        if (deckBuilder == null) return;

        string deckId = deckBuilder.FinalizeDeck();

        if (deckId != null)
        {
            Debug.Log($"Deck erstellt: {deckId}");
            ShowConfigPanel();
        }
    }
}