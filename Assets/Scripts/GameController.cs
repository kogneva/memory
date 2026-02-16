using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Verwaltet das Memory-Spiel mit Decks, Gruppen und Karten
/// </summary>
/// 
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [SerializeField]
    [Tooltip("Sprite für die Kartenrückseite")]
    public Sprite backSprite; // Punkt 6: defaultBackImage entfernt, nur noch backSprite

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

    [Header("Auto Deck Generation (when no user decks)")]
    [Tooltip("If >0 forces number of groups; otherwise groups = cards.Count / defaultRequiredForMatch")]
    [SerializeField]
    private int defaultGroupCount;

    [Tooltip("How many cards required per match (e.g. 2 for pairs)")]
    [SerializeField]
    private int defaultRequiredForMatch = 2;

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

        // Punkt 6: Vereinfacht - nur backSprite laden wenn nicht gesetzt
        if (backSprite == null)
        {
            backSprite = Resources.Load<Sprite>("Sprites/photo_5389104988137058662_y");
            if (backSprite == null)
            {
                Debug.LogWarning("Couldn't load default BackSprite");
            }
        }

        // Ensure game over UI is hidden at start
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void Start()
    {
        GetCards();

        // If ImageManager exists, ensure default deck is created when no user decks present
        if (ImageManager.Instance != null)
        {
            if (ImageManager.Instance.memoryDecks == null || ImageManager.Instance.memoryDecks.Count == 0)
            {
                int cardsCount = cards.Count;
                int groupsNeeded = defaultGroupCount > 0 ? defaultGroupCount : Mathf.Max(1, cardsCount / Mathf.Max(1, defaultRequiredForMatch));

                Debug.Log($"No user decks found - creating default deck with {groupsNeeded} groups (requiredForMatch={defaultRequiredForMatch})");
                string newDeckId = ImageManager.Instance.CreateDefaultDeck(groupsNeeded, defaultRequiredForMatch, "AutoDefaultDeck");

                InitializeGame(newDeckId);
                return;
            }

            if (currentDeck == null && ImageManager.Instance.memoryDecks != null && ImageManager.Instance.memoryDecks.Count > 0)
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

        // Fallback wenn kein Deck gesetzt
        if (currentDeck == null)
        {
            int matchSize = 2;
            int cardsCount = cards.Count;
            if (cardsCount <= 0)
            {
                return;
            }

            int groupsNeeded = cardsCount / matchSize;
            int cardIdx = 0;

            for (int g = 0; g < groupsNeeded; g++)
            {
                // Punkt 5: Nutze direkt ImageManager.GetDefaultSpriteById
                Sprite s = imageManager?.GetDefaultSpriteById(g);

                for (int m = 0; m < matchSize && cardIdx < cardsCount; m++)
                {
                    Card card = cards[cardIdx];
                    card.groupId = g.ToString();
                    card.SetFrontSprite(s);
                    assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, sprite={(s != null ? s.name : "null")}");
                    cardIdx++;
                }
            }

            while (cardIdx < cardsCount)
            {
                Sprite s = imageManager?.GetDefaultSpriteById(groupsNeeded);
                Card card = cards[cardIdx];
                card.groupId = groupsNeeded.ToString();
                card.SetFrontSprite(s);
                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, sprite={(s != null ? s.name : "null")}");
                cardIdx++;
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
                    Debug.LogWarning($"Sprite für Bild {imageId} konnte nicht geladen werden - verwende Default für Gruppe {deckGroup.groupId}");
                    // Punkt 5: Direkt ImageManager nutzen
                    groupSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                    groupSourceDesc += ", fallback";
                }
            }
            else
            {
                // Punkt 5: Direkt ImageManager nutzen
                groupSprite = imageManager?.GetDefaultSpriteById(groupIndex);
                groupSourceDesc = $"group-default(groupIndex {groupIndex})";
            }

            for (int i = 0; i < deckGroup.requiredForMatch && cardIndex < cards.Count; i++)
            {
                Card card = cards[cardIndex];
                card.groupId = deckGroup.groupId;
                card.SetFrontSprite(groupSprite);

                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source={groupSourceDesc}, sprite={(groupSprite != null ? groupSprite.name : "null")}");

                if (card.frontSprite == null)
                {
                    Debug.LogWarning($"Sprite für Karte an Index {cardIndex} konnte nicht gesetzt werden");
                }

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
        {
            return;
        }

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

        if (allSameGroup && group != null && revealedCards.Count == group.requiredForMatch)
        {
            Debug.Log($"Match gefunden für Gruppe {group.groupName}!");
            string matchedNames = string.Join(", ", revealedCards.Select(c => c.gameObject.name));
            Debug.Log($"Match result: SUCCESS. Cards: {matchedNames} (groupId={groupId})");

            // Punkt 4: MarkAsMatched nur EINMAL aufrufen (hier in CheckForMatch)
            foreach (var c in revealedCards)
            {
                c.MarkAsMatched();
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
            sb.AppendLine($"\tGruppe {group.groupId}: {group.requiredForMatch} Karten benötigt");
        }

        Debug.Log(sb.ToString());
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Spiel wird neu gestartet");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("Spiel wird beendet");
    }

    public void ToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Hauptmenü wird geladen");
    }
}

