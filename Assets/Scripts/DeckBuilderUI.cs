using UnityEngine;
using UnityEngine.UI;

public class DeckBuilderUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject configPanel;
    public GameObject assignmentPanel;

    [Header("Config Panel")]
    public InputField deckNameInput;
    public InputField groupCountInput;
    public InputField groupSizeInput;  
    public InputField requiredForMatchInput; 
    public Toggle useSameImagesToggle;
    public Text modeDescriptionText;
    public Text errorText;

    [Header("Assignment Panel")]
    public Text progressText;
    public Slider progressBar;
    public Text groupInfoText;
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

        ShowConfigPanel();
        UpdateModeDescription();
    }

    void UpdateModeDescription()
    {
        if (modeDescriptionText == null) return;

        if (isClassicMode)
            modeDescriptionText.text = "Klassisch: 1 Bild pro Gruppe\n(mehrfach verwendet)";
        else
            modeDescriptionText.text = "Thematisch: Verschiedene Bilder pro Gruppe";
    }

    void ShowConfigPanel()
    {
        if (configPanel != null) configPanel.SetActive(true);
        if (assignmentPanel != null) assignmentPanel.SetActive(false);
        if (errorText != null) errorText.text = "";
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

        // KORRIGIERT: Alle 5 Parameter übergeben
        if (deckBuilder.StartDeckConfiguration(deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode))
        {
            if (configPanel != null) configPanel.SetActive(false);
            if (assignmentPanel != null) assignmentPanel.SetActive(true);
            UpdateProgressBar();
        }
        else
        {
            // KORRIGIERT: Alle 5 Parameter übergeben
            var config = new ImageManager.SimplifiedDeckConfig(
                deckNameInput.text, groupCount, groupSize, requiredForMatch, isClassicMode
            );
            var validation = ImageManager.Instance.ValidateSimplifiedDeck(config);
            if (errorText != null) errorText.text = validation.errorMessage;
        }
    }

    public void OnCancelConfigClick()
    {
        gameObject.SetActive(false);
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
                groupInfoText.text = $"Bilder: {selected}/{config.groupSize}";  // KORRIGIERT: groupSize statt requiredForMatch
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
        else
        {
            ShowConfigPanel();
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
            gameObject.SetActive(false);
        }
    }

    public void OnCancelAssignmentClick()
    {
        deckBuilder.CancelDeckCreation();
        ShowConfigPanel();
    }
}