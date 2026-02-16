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
    [Tooltip("Sprite für die Kartenrückseite")]
    public Sprite backSprite;

    [Header("UI")]
    [Tooltip("Optional: Panel (oder GameObject), das die Game Over-Benutzeroberfläche enthält)")]
    [SerializeField]
    private GameObject gameOverPanel;

    public List<Card> cards = new List<Card>();
    
    private ImageManager.MemoryDeck currentDeck;
    private readonly List<Card> revealedCards = new List<Card>();
    private bool checkingMatch;
    private int matchesFound;
    private int totalMatches;

    [Header("Auto Deck Generation")]
    [Tooltip("Anzahl der Gruppen/Paare")]
    [SerializeField]
    private int defaultGroupCount = 4;

    [Tooltip("Anzahl der Karten pro Gruppe (z.B. 2 für klassische Paare, 4 für Vierer-Gruppen)")]
    [SerializeField]
    private int defaultGroupSize = 2;

    [Tooltip("Wie viele Karten müssen für einen Match aufgedeckt werden")]
    [SerializeField]
    private int defaultRequiredForMatch = 2;

    [Header("Debug")]
    [Tooltip("Wenn aktiviert, wird das Auto-Deck bei jedem Start neu erstellt (ignoriert gespeicherte Decks)")]
    [SerializeField]
    private bool alwaysRecreateAutoDeck = false;

    public int DefaultGroupCount => defaultGroupCount;
    public int DefaultGroupSize => defaultGroupSize;
    public int DefaultRequiredForMatch => defaultRequiredForMatch;

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

        if (ImageManager.Instance != null)
        {
            // NEU: Prüfe ob Auto-Deck neu erstellt werden soll
            bool needsNewDeck = ImageManager.Instance.memoryDecks == null || 
                                ImageManager.Instance.memoryDecks.Count == 0 ||
                                alwaysRecreateAutoDeck ||
                                ShouldRecreateAutoDeck();

            if (needsNewDeck)
            {
                // Entferne alte Auto-Decks
                RemoveAutoDecks();

                Debug.Log($"Creating new auto-deck: {defaultGroupCount} groups × {defaultGroupSize} cards (requiredForMatch={defaultRequiredForMatch})");
                
                string newDeckId = ImageManager.Instance.CreateDefaultDeck(
                    defaultGroupCount, 
                    defaultGroupSize,
                    defaultRequiredForMatch, 
                    "AutoDefaultDeck");

                InitializeGame(newDeckId);
                return;
            }

            if (currentDeck == null && ImageManager.Instance.memoryDecks.Count > 0)
            {
                Debug.Log("Kein Deck initialisiert - starte automatisch Deck 0");
                InitializeGame(ImageManager.Instance.memoryDecks[0].deckId);
                return;
            }
        }

        if (currentDeck == null)
        {
            SetupCardImages();
        }
    }

    /// <summary>
    /// Prüft ob das vorhandene Auto-Deck mit den aktuellen Einstellungen übereinstimmt
    /// </summary>
    bool ShouldRecreateAutoDeck()
    {
        if (ImageManager.Instance.memoryDecks == null || ImageManager.Instance.memoryDecks.Count == 0)
            return false;

        // Finde das Auto-Deck
        var autoDeck = ImageManager.Instance.memoryDecks.Find(d => d.deckName == "AutoDefaultDeck");
        if (autoDeck == null)
            return false; // Kein Auto-Deck vorhanden

        // Prüfe ob die Konfiguration übereinstimmt
        if (autoDeck.groups.Count != defaultGroupCount)
        {
            Debug.Log($"Auto-Deck hat {autoDeck.groups.Count} Gruppen, aber {defaultGroupCount} sind konfiguriert - erstelle neu");
            return true;
        }

        if (autoDeck.groups.Count > 0)
        {
            var firstGroup = autoDeck.groups[0];
            if (firstGroup.groupSize != defaultGroupSize || firstGroup.requiredForMatch != defaultRequiredForMatch)
            {
                Debug.Log($"Auto-Deck Konfiguration hat sich geändert - erstelle neu");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Entfernt alle Auto-Decks aus dem ImageManager
    /// </summary>
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

        if (currentDeck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return;
        }

        LogCurrentDeck();

        matchesFound = 0;
        revealedCards.Clear();
        checkingMatch = false;

        SetupCardImages();
        CalculateTotalMatches();
    }

    void CalculateTotalMatches()
    {
        totalMatches = currentDeck.groups.Count;
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

            int groupsNeeded = cardsCount / defaultGroupSize;
            int cardIdx = 0;

            for (int g = 0; g < groupsNeeded; g++)
            {
                Sprite s = imageManager?.GetDefaultSpriteById(g);

                for (int m = 0; m < defaultGroupSize && cardIdx < cardsCount; m++)
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
            Sprite groupSprite = null;
            string groupSourceDesc = "";

            if (deckGroup.imageIds != null && deckGroup.imageIds.Count > 0)
            {
                string imageId = deckGroup.imageIds[0];
                groupSprite = imageManager.LoadPoolImageSprite(imageId);
                groupSourceDesc = $"pool:{imageId}";

                if (groupSprite == null)
                {
                    Debug.LogWarning($"Sprite für Bild {imageId} konnte nicht geladen werden - verwende Default");
                    groupSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                    groupSourceDesc += ", fallback";
                }
            }
            else
            {
                groupSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                groupSourceDesc = $"group-default(groupIndex {groupIndex})";
            }

            for (int i = 0; i < deckGroup.groupSize && cardIndex < cards.Count; i++)
            {
                Card card = cards[cardIndex];
                card.groupId = deckGroup.groupId;
                card.SetFrontSprite(groupSprite);

                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source={groupSourceDesc}, sprite={(groupSprite != null ? groupSprite.name : "null")}");

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
        if (group != null && revealedCards.Count >= group.requiredForMatch)
        {
            StartCoroutine(CheckForMatch());
        }
    }

    IEnumerator CheckForMatch()
    {
        checkingMatch = true;
        yield return new WaitForSeconds(0.5f);

        string groupId = revealedCards[0].groupId;
        bool allSameGroup = revealedCards.All(c => c.groupId == groupId);

        ImageManager.DeckGroup group = currentDeck.groups.Find(g => g.groupId == groupId);

        if (allSameGroup && group != null && revealedCards.Count >= group.requiredForMatch)
        {
            Debug.Log($"Match gefunden für Gruppe {group.groupName}!");

            foreach (Card card in cards.Where(c => c.groupId == groupId))
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
            string revealedNames = string.Join(", ", revealedCards.Select(c => c.gameObject.name));
            Debug.Log($"Match result: FAIL. Revealed: {revealedNames}");

            yield return new WaitForSeconds(0.5f);
            foreach (Card card in revealedCards)
            {
                card.Hide();
            }
        }

        revealedCards.Clear();
        checkingMatch = false;
    }

    void OnGameOver()
    {
        Debug.Log("Spiel vorbei!");
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

    // NEU: Editor-Helfer zum Löschen gespeicherter Decks
    [ContextMenu("Clear All Saved Decks (PlayerPrefs)")]
    void ClearSavedDecks()
    {
        PlayerPrefs.DeleteKey("IMAGE_POOL");
        PlayerPrefs.DeleteKey("MEMORY_DECKS");
        PlayerPrefs.Save();
        Debug.Log("Alle gespeicherten Decks wurden gelöscht. Starte Szene neu.");
    }
}

