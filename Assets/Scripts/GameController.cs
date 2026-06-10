using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Verwaltet das Memory-Spiel mit Decks, Gruppen und Karten
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [SerializeField]
    public Timer timer;

    [SerializeField]
    public GameAnalytics analytics;

    [Tooltip("Sprite für die Kartenrückseite")]
    public Sprite backSprite;

    [Header("UI")]
    [Tooltip("Optional: Panel (oder GameObject), das die Game Over-Benutzeroberfläche enthält)")]
    [SerializeField]
    private GameObject gameOverPanel;

    [Header("Deck Selection")]
    [SerializeField] private GameObject deckSelectionPanel;
    [SerializeField] private Transform deckButtonContainer;
    [SerializeField] private GameObject deckButtonPrefab;

    public List<Card> cards = new List<Card>();
    
    private ImageManager.MemoryDeck currentDeck;
    private readonly List<Card> revealedCards = new List<Card>();
    private bool checkingMatch;
    private int matchesFound;
    private int totalMatches;

    [Header("Auto Deck Generation")]
    [Tooltip("Anzahl der Gruppen/Paare")]
    [SerializeField]
    private int defaultGroupCount = 6;

    [Tooltip("Anzahl der Karten pro Gruppe (z.B. 2 für klassische Paare, 4 für Vierer-Gruppen)")]
    [SerializeField]
    private int defaultGroupSize = 2;

    [Tooltip("Wie viele Karten müssen für einen Match aufgedeckt werden")]
    [SerializeField]
    private int defaultRequiredForMatch = 2;

    public int DefaultGroupCount => defaultGroupCount;
    public int DefaultGroupSize => defaultGroupSize;
    public int DefaultRequiredForMatch => defaultRequiredForMatch;

    // Aktuelle Spielkonfiguration (wird aus Deck oder Defaults geladen)
    private int currentGroupCount;
    private int currentGroupSize;
    private int currentRequiredForMatch;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (backSprite == null)
        {
            backSprite = Resources.Load<Sprite>("Sprites/photo_5389104988137058662_y");
            if (backSprite == null)
            {
                Debug.LogWarning("Couldn't load default BackSprite");
            }
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Start()
    {
        GetCards();

        if (ImageManager.Instance == null)
        {
            Debug.LogError("ImageManager.Instance ist null!");
            LoadGameConfigFromDefaults();
            SetupCardImages();
            return;
        }

        var selectedDeck = ImageManager.Instance.GetSelectedDeck();
        
        if (selectedDeck != null)
        {
            Debug.Log($"Starte mit ausgewähltem Deck: {selectedDeck.deckName}");
            InitializeGame(selectedDeck.deckId);
        }
        else if (ImageManager.Instance.memoryDecks?.Count > 0)
        {
            var firstDeck = ImageManager.Instance.memoryDecks[0];
            ImageManager.Instance.SetSelectedDeck(firstDeck.deckId);
            InitializeGame(firstDeck.deckId);
        }
        else
        {
            CreateAndStartAutoDeck();
        }
    }

    /// <summary>
    /// Lädt die Spielkonfiguration in folgender Priorität:
    /// 1. Aus dem ausgewählten Deck des Spielers
    /// 2. Aus irgendeinem vorhandenen Deck
    /// 3. Aus den Default-Werten
    /// </summary>
    void LoadGameConfig()
    {
        ImageManager imageManager = ImageManager.Instance;
        
        if (imageManager == null)
        {
            LoadGameConfigFromDefaults();
            return;
        }

        // 1. Priorität: Ausgewähltes Deck
        var selectedDeck = imageManager.GetSelectedDeck();
        if (selectedDeck != null && selectedDeck.groups?.Count > 0)
        {
            LoadGameConfigFromDeck(selectedDeck, "ausgewähltes Deck");
            return;
        }

        // 2. Priorität: Irgendein vorhandenes Deck
        if (imageManager.memoryDecks?.Count > 0)
        {
            var anyDeck = imageManager.memoryDecks[0];
            if (anyDeck.groups?.Count > 0)
            {
                LoadGameConfigFromDeck(anyDeck, "erstes verfügbares Deck");
                return;
            }
        }

        // 3. Priorität: Default-Werte
        LoadGameConfigFromDefaults();
    }

    void LoadGameConfigFromDeck(ImageManager.MemoryDeck deck, string source)
    {
        currentGroupCount = deck.GroupCount;
        currentGroupSize = deck.groupSize;
        currentRequiredForMatch = deck.requiredForMatch;
        
        Debug.Log($"Spielkonfiguration geladen aus {source} '{deck.deckName}': " +
                  $"GroupCount={currentGroupCount}, GroupSize={currentGroupSize}, RequiredForMatch={currentRequiredForMatch}");
    }

    void LoadGameConfigFromDefaults()
    {
        currentGroupCount = defaultGroupCount;
        currentGroupSize = defaultGroupSize;
        currentRequiredForMatch = defaultRequiredForMatch;
        
        Debug.Log($"Spielkonfiguration aus Default-Werten: " +
                  $"GroupCount={currentGroupCount}, GroupSize={currentGroupSize}, RequiredForMatch={currentRequiredForMatch}");
    }

    void CreateAndStartAutoDeck()
    {
        // Lade Konfiguration bevor das Auto-Deck erstellt wird
        LoadGameConfig();
        
        RemoveAutoDecks();
        string newDeckId = ImageManager.Instance.CreateDefaultDeck(
            currentGroupCount, currentGroupSize, currentRequiredForMatch, "AutoDefaultDeck");
        ImageManager.Instance.SetSelectedDeck(newDeckId);
        InitializeGame(newDeckId);
    }

    void RemoveAutoDecks()
    {
        if (ImageManager.Instance.memoryDecks == null)
            return;

        var autoDecks = ImageManager.Instance.memoryDecks
            .Where(d => d.deckName == "AutoDefaultDeck")
            .ToList();

        foreach (var deck in autoDecks)
        {
            ImageManager.Instance.RemoveDeck(deck.deckId);
            Debug.Log($"Altes Auto-Deck entfernt: {deck.deckId}");
        }
    }

    void GetCards()
    {
        cards.Clear();
        GameObject[] objects = GameObject.FindGameObjectsWithTag("MemoryCard");
        foreach (GameObject obj in objects)
        {
            Card card = obj.GetComponent<Card>();
            if (card != null)
            {
                cards.Add(card);
            }
        }
    }

    public void InitializeGame(string deckId)
    {
        ImageManager imageManager = ImageManager.Instance;
        currentDeck = imageManager.GetDeck(deckId);
        Debug.Log($"currentDeck: {JsonUtility.ToJson(currentDeck, true)}");
        if (currentDeck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return;
        }

        // Lade Konfiguration aus dem aktuellen Deck
        LoadGameConfigFromDeck(currentDeck, "initialisiertes Deck");

        LogCurrentDeck();

        matchesFound = 0;
        revealedCards.Clear();
        checkingMatch = false;

        // Analytics zurücksetzen
        analytics.StartAnalytics(currentDeck);

        SetupCardImages();
        CalculateTotalMatches();
        timer.StartTimer();
    }

    void CalculateTotalMatches()
    {
        totalMatches = currentDeck.groups.Count;
        analytics.TotalPossibleCorrectGuesses = totalMatches;
    }

    void SetupCardImages()
    {
        ImageManager imageManager = ImageManager.Instance;
        int cardIndex = 0;

        StringBuilder assignmentLog = new StringBuilder();
        int groupIndex = 0;

        if (cards != null && cards.Count > 1)
        {
            ShuffleCards();
        }

        if (currentDeck == null)
        {
            int cardsCount = cards.Count;
            if (cardsCount <= 0) return;

            int groupsNeeded = cardsCount / currentGroupSize;
            int cardIdx = 0;

            for (int g = 0; g < groupsNeeded; g++)
            {
                Sprite s = imageManager?.GetDefaultSpriteById(g);

                for (int m = 0; m < currentGroupSize && cardIdx < cardsCount; m++)
                {
                    Card card = cards[cardIdx];
                    card.groupId = g.ToString();
                    card.SetFrontSprite(s);
                    assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, sprite={(s != null ? s.name : "null")}");
                    cardIdx++;
                }
            }

            Debug.Log(assignmentLog.ToString());
            return;
        }

        foreach (ImageManager.DeckGroup deckGroup in currentDeck.groups)
        {
            for (int i = 0; i < deckGroup.groupSize && cardIndex < cards.Count; i++)
            {
                Card card = cards[cardIndex];
                card.groupId = deckGroup.groupId;
                
                Sprite cardSprite = null;
                string sourceDesc = "";

                string imageId = currentDeck.GetImageIdForCard(deckGroup, i);

                if (!string.IsNullOrEmpty(imageId))
                {
                    card.imageId = imageId;
                    cardSprite = imageManager.LoadPoolImageSprite(imageId);
                    sourceDesc = $"pool:{imageId}";

                    if (cardSprite == null)
                    {
                        card.imageId = null;
                        Debug.LogWarning($"Sprite für Bild {imageId} konnte nicht geladen werden - verwende Default");
                        cardSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                        sourceDesc += ", fallback";
                    }
                }
                else
                {
                    card.imageId = null;
                    cardSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                    sourceDesc = $"group-default(groupIndex {groupIndex})";
                }

                card.SetFrontSprite(cardSprite);
                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source={sourceDesc}, sprite={(cardSprite != null ? cardSprite.name : "null")}");

                cardIndex++;
            }

            groupIndex++;
        }

        Debug.Log(assignmentLog.ToString());
    }

    public void CardRevealed(Card revealedCard)
    {
        if (checkingMatch || revealedCards.Contains(revealedCard))
            return;

        if (revealedCard.frontSprite == null)
        {
            Debug.LogWarning($"Karte {revealedCard.gameObject.name} hat kein frontSprite - Ignoriere Klick");
            return;
        }

        revealedCard.Reveal();
        revealedCards.Add(revealedCard);

        if (currentDeck == null)
            return;

        ImageManager.DeckGroup group = currentDeck.groups.Find(g => g.groupId == revealedCard.groupId);
        if (group != null && revealedCards.Count > 1)
        {
            StartCoroutine(CheckForMatch());
        }
    }

    private IEnumerator CheckForMatch()
    {
        checkingMatch = true;

        yield return new WaitForSeconds(0.5f);

        if (revealedCards.Count == 0)
        {
            checkingMatch = false;
            yield break;
        }

        string groupId = revealedCards[0].groupId;

        ImageManager.DeckGroup group = currentDeck.groups
            .FirstOrDefault(g => g.groupId == groupId);

        bool allSameGroup = revealedCards.All(card => card.groupId == groupId);

        if (allSameGroup && group != null)
        {
            int missingCardCount = group.requiredForMatch - revealedCards.Count;
            bool hasMissingCards = missingCardCount > 0;

            if (hasMissingCards)
            {
                Debug.Log(
                    $"Match gefunden für Gruppe {group.groupName}, " +
                    $"für die Gruppe sind noch {missingCardCount} Karten zu finden!"
                );

                checkingMatch = false;
                yield break;
            }

            Debug.Log($"Match gefunden für Gruppe {group.groupName}!");

            foreach (Card card in cards.Where(card => card.groupId == groupId))
            {
                card.MarkAsMatched();
            }

            matchesFound++;

            if (matchesFound >= totalMatches)
            {
                OnGameOver();
            }
        }
        else
        {
            string revealedNames = string.Join(
                ", ",
                revealedCards.Select(card => card.gameObject.name)
            );

            Debug.Log($"Match result: FAIL. Revealed: {revealedNames}");

            yield return new WaitForSeconds(0.5f);

            foreach (Card card in revealedCards)
            {
                // Analytics für die einzelne Karte erfassen
                analytics.RecordIncorrectGuessForCard(card.imageId);
                Debug.Log($"Versteckte Karte {card.imageId}");

                card.Hide();
            }

            analytics.IncorrectMatchGuesses++;

            Debug.Log($"incorrectGuesses: {analytics.IncorrectMatchGuesses}");
        }

        revealedCards.Clear();
        checkingMatch = false;
    }

    void OnGameOver()
    {
        Debug.Log("Spiel vorbei!");
        timer.StopTimer();
        analytics.GameFinished = true;
        
        analytics.SaveToJson(timer.timer); 
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    void ShuffleCards()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    void LogCurrentDeck()
    {
        if (currentDeck == null)
        {
            Debug.Log("Kein Deck ist derzeit aktiviert.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Aktuelles Deck: {currentDeck.deckId} - {currentDeck.deckName}");
        
        foreach (var group in currentDeck.groups)
        {
            sb.AppendLine($"\tGruppe {group.groupId}: {group.groupSize} Karten, {group.requiredForMatch} für Match");
        }

        Debug.Log(sb.ToString());
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    [ContextMenu("Clear All Saved Decks (PlayerPrefs)")]
    void ClearSavedDecks()
    {
        PlayerPrefs.DeleteKey("IMAGE_POOL");
        PlayerPrefs.DeleteKey("MEMORY_DECKS");
        PlayerPrefs.Save();
        Debug.Log("Alle gespeicherten Decks wurden gelöscht. Starte Szene neu.");
    }
}

