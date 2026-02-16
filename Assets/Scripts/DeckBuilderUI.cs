using UnityEngine;
using UnityEngine.UI;
using TMPro; 

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

    private DeckBuilder deckBuilder;
    private bool isClassicMode = true;

    void Start()
    {
        deckBuilder = gameObject.AddComponent<DeckBuilder>();

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
        if (string.IsNullOrWhiteSpace(deckNameInput.text))
        {
            if (errorText != null) errorText.text = "Bitte Deck-Namen eingeben";
            return;
        }

        int groupCount = int.Parse(groupCountInput.text);
        int groupSize = int.Parse(groupSizeInput.text);
        int requiredForMatch = int.Parse(requiredForMatchInput.text);

        if (deckBuilder.StartDeckConfiguration(deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode))
        {
            UpdateProgressBar();
        }
        else
        {
            var config = new ImageManager.SimplifiedDeckConfig(
                deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode
            );
            var validation = ImageManager.Instance.ValidateSimplifiedDeck(config);
            if (errorText != null) errorText.text = validation.errorMessage;
        }
    }

    public void OnCancelClick()
    {
        if (errorText != null) errorText.text = "";
    }

    void UpdateProgressBar()
    {
        var config = deckBuilder.GetCurrentConfig();
        int currentIndex = deckBuilder.GetCurrentGroupIndex();

        if (progressText != null)
        {
            progressText.text = deckBuilder.GetProgressDescription();
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

        foreach (Transform child in selectedImagesContainer)
        {
            Destroy(child.gameObject);
        }

        var selectedImageIds = deckBuilder.GetCurrentGroupImages();

        foreach (string imageId in selectedImageIds)
        {
            var poolImage = ImageManager.Instance.GetPoolImage(imageId);
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

        Image img = item.GetComponent<Image>();
        if (img != null)
        {
            Sprite sprite = ImageManager.Instance.LoadPoolImageSprite(poolImage.imageId);
            if (sprite != null) img.sprite = sprite;
        }

        Button btn = item.GetComponent<Button>();
        if (btn != null)
        {
            string imageId = poolImage.imageId;

            if (isAvailable)
            {
                btn.onClick.AddListener(() => {
                    if (deckBuilder.AddImageToCurrentGroup(imageId))
                    {
                        UpdateProgressBar();
                    }
                });
            }
            else
            {
                btn.onClick.AddListener(() => {
                    if (deckBuilder.RemoveImageFromCurrentGroup(imageId))
                    {
                        UpdateProgressBar();
                    }
                });
            }
        }
    }

    public void OnBackClick()
    {
        if (deckBuilder.PreviousGroup())
        {
            UpdateProgressBar();
        }
    }

    public void OnNextClick()
    {
        if (deckBuilder.IsCurrentGroupComplete())
        {
            if (deckBuilder.NextGroup())
            {
                UpdateProgressBar();
            }
        }
    }

    public void OnFinishClick()
    {
        string deckId = deckBuilder.FinalizeDeck();

        if (deckId != null)
        {
            Debug.Log($"Deck erstellt: {deckId}");
        }
    }
}