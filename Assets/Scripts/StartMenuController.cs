using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    [Header("Deck Selection")]
    [SerializeField] private GameObject deckSelectionPanel;
    [SerializeField] private Transform deckButtonContainer;
    [SerializeField] private GameObject deckButtonPrefab;
    [SerializeField] private TMP_Text currentDeckText;

    void Awake()
    {
        if (ImageManager.Instance == null)
            new GameObject("ImageManager").AddComponent<ImageManager>();
    }

    void Start()
    {
        UpdateCurrentDeckDisplay();
        if (deckSelectionPanel != null)
            deckSelectionPanel.SetActive(false);
    }

    void UpdateCurrentDeckDisplay()
    {
        if (currentDeckText == null) return;

        var deck = ImageManager.Instance?.GetSelectedDeck();
        currentDeckText.text = deck != null
            ? $"Aktuelles Deck: {deck.deckName}"
            : "Kein Deck ausgewählt";
    }

    public void OnStartClick()
    {
        SceneManager.LoadScene("MemoryGame");
    }

    public void OnSelectDeckClick()
    {
        ShowDeckSelection();
    }

    public void ShowDeckSelection()
    {
        if (deckSelectionPanel == null || deckButtonContainer == null || deckButtonPrefab == null) 
            return;

        deckSelectionPanel.SetActive(true);

        foreach (Transform child in deckButtonContainer)
            Destroy(child.gameObject);

        var decks = ImageManager.Instance?.memoryDecks;
        if (decks == null || decks.Count == 0) 
        {
            Debug.Log("Keine Decks vorhanden");
            return;
        }

        string selectedId = ImageManager.Instance.GetSelectedDeckId();

        foreach (var deck in decks)
        {
            var btn = Instantiate(deckButtonPrefab, deckButtonContainer);
            var text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                string marker = deck.deckId == selectedId ? " ✓" : "";
                text.text = $"{deck.deckName}{marker}\n<size=70%>{deck.groups.Count} Gruppen</size>";
            }

            string deckId = deck.deckId;
            var button = btn.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => {
                    ImageManager.Instance.SetSelectedDeck(deckId);
                    deckSelectionPanel.SetActive(false);
                    UpdateCurrentDeckDisplay();
                });
            }
        }
    }

    public void OnUploadImagesClick()
    {
#if !UNITY_EDITOR
        ImageManager.Instance.AddImagesToPool((ids) => Debug.Log($"{ids?.Count ?? 0} Bilder hinzugefügt"));
#else
        Debug.LogWarning("NativeGallery funktioniert nicht im Editor");
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
