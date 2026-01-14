using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verwaltet das Memory-Spiel mit Decks, Gruppen und Karten
/// </summary>
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [SerializeField]
    public Sprite backSprite;

    [SerializeField]
    private Sprite defaultBackImage;

    public List<Card> cards = new List<Card>();
    
    private ImageManager.MemoryDeck currentDeck;
    private List<Card> revealedCards = new List<Card>();
    private bool checkingMatch = false;
    private int matchesFound = 0;
    private int totalMatches = 0;

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

        backSprite = defaultBackImage;
    }

    void Start()
    {
        GetCards();
    }

    /// <summary>
    /// Sammelt alle Card-Komponenten aus dem Spielfeld
    /// </summary>
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

    /// <summary>
    /// Initialisiert das Spiel mit einem Deck
    /// </summary>
    public void InitializeGame(int deckId)
    {
        ImageManager imageManager = ImageManager.Instance;
        currentDeck = imageManager.GetDeck(deckId);

        if (currentDeck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return;
        }

        matchesFound = 0;
        revealedCards.Clear();
        checkingMatch = false;

        // Verteile Bilder auf Karten
        SetupCardImages();
        CalculateTotalMatches();
    }

    /// <summary>
    /// Berechnet die Gesamtzahl der Matches basierend auf dem aktuellen Deck
    /// </summary>
    void CalculateTotalMatches()
    {
        totalMatches = currentDeck.groups.Count;
    }

    /// <summary>
    /// Verteilt die Bilder der Gruppen auf die Karten
    /// </summary>
    void SetupCardImages()
    {
        ImageManager imageManager = ImageManager.Instance;
        int cardIndex = 0;

        foreach (ImageManager.DeckGroup deckGroup in currentDeck.groups)
        {
            // Weise Bilder dieser Gruppe den Karten zu (requiredForMatch Karten pro Gruppe)
            for (int i = 0; i < deckGroup.requiredForMatch && cardIndex < cards.Count; i++)
            {
                if (cardIndex >= cards.Count)
                    break;

                Card card = cards[cardIndex];
                int imageIndex = i % deckGroup.imageIds.Count;
                
                if (imageIndex < deckGroup.imageIds.Count)
                {
                    int imageId = deckGroup.imageIds[imageIndex];
                    card.groupId = deckGroup.groupId;
                    card.frontSprite = imageManager.LoadPoolImageSprite(imageId);
                    
                    if (card.frontSprite == null)
                    {
                        Debug.LogWarning($"Sprite für Bild {imageId} konnte nicht geladen werden");
                    }
                }

                cardIndex++;
            }
        }

        // Mische die Karten
        ShuffleCards();
    }

    /// <summary>
    /// Wird aufgerufen, wenn eine Karte aufgedeckt wird
    /// </summary>
    public void CardRevealed(Card revealedCard)
    {
        if (checkingMatch || revealedCards.Contains(revealedCard))
            return;

        revealedCard.Reveal();
        revealedCards.Add(revealedCard);

        // Wenn genug Karten aufgedeckt wurden, prüfe auf Match
        ImageManager.DeckGroup group = currentDeck.groups.Find(g => g.groupId == revealedCard.groupId);
        if (group != null && revealedCards.Count >= group.requiredForMatch)
        {
            StartCoroutine(CheckForMatch());
        }
    }

    /// <summary>
    /// Prüft, ob die aufgedeckten Karten einer Gruppe entsprechen
    /// </summary>
    IEnumerator CheckForMatch()
    {
        checkingMatch = true;
        yield return new WaitForSeconds(0.5f);

        // Prüfe, ob alle aufgedeckten Karten zur gleichen Gruppe gehören
        int groupId = revealedCards[0].groupId;
        bool allSameGroup = true;

        foreach (Card card in revealedCards)
        {
            if (card.groupId != groupId)
            {
                allSameGroup = false;
                break;
            }
        }

        ImageManager.DeckGroup group = currentDeck.groups.Find(g => g.groupId == groupId);

        if (allSameGroup && group != null && revealedCards.Count == group.requiredForMatch)
        {
            // Match gefunden!
            Debug.Log($"Match gefunden für Gruppe {group.groupName}!");
            RemoveMatchingCards(groupId);
            matchesFound++;

            if (matchesFound >= totalMatches)
            {
                OnGameOver();
            }
        }
        else
        {
            // Kein Match - Karten zurück
            foreach (Card card in revealedCards)
            {
                card.Hide();
            }
        }

        revealedCards.Clear();
        yield return new WaitForSeconds(0.5f);
        checkingMatch = false;
    }

    /// <summary>
    /// Entfernt alle Karten einer Gruppe vom Spielfeld
    /// </summary>
    void RemoveMatchingCards(int groupId)
    {
        List<Card> cardsToRemove = new List<Card>();

        foreach (Card card in cards)
        {
            if (card.groupId == groupId)
            {
                cardsToRemove.Add(card);
            }
        }

        foreach (Card card in cardsToRemove)
        {
            card.gameObject.SetActive(false);
            cards.Remove(card);
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn das Spiel vorbei ist
    /// </summary>
    void OnGameOver()
    {
        Debug.Log("Spiel vorbei!");
        // TODO: Game Over UI anzeigen, Score anzeigen, etc.
    }

    /// <summary>
    /// Mischt die Karten zufällig
    /// </summary>
    void ShuffleCards()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // Tausche
            Card temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}

