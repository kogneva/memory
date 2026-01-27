using System.Collections;
using System.Collections.Generic;
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
    public Sprite backSprite;

    [SerializeField]
    private Sprite defaultBackImage;

    [Header("UI")]
    [Tooltip("Optional: Panel (oder GameObject), das die Game Over-Benutzeroberfläche enthält)")]
    [SerializeField]
    private GameObject gameOverPanel;

    public List<Card> cards = new List<Card>();
    
    private ImageManager.MemoryDeck currentDeck;
    private readonly List<Card> revealedCards = new List<Card>();
    private bool checkingMatch = false;
    private int matchesFound = 0;
    private int totalMatches = 0;

    [Header("Auto Deck Generation (when no user decks)")]
    [Tooltip("If >0 forces number of groups; otherwise groups = cards.Count / defaultRequiredForMatch")]
    [SerializeField]
    private int defaultGroupCount = 0;

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

        // If no defaultBackImage, load from Resources
        if (defaultBackImage == null)
        {
            Sprite loaded = Resources.Load<Sprite>("Sprites/photo_5389104988137058662_y");
            if (loaded != null) {
                defaultBackImage = loaded;
            }
            else {
                Debug.LogWarning("Couldn't load default BackSprite");
            }
        }

        backSprite = defaultBackImage;

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
                // Determine number of groups based on card count and required-for-match
                int cardsCount = cards.Count;
                int groupsNeeded = defaultGroupCount > 0 ? defaultGroupCount : Mathf.Max(1, cardsCount / Mathf.Max(1, defaultRequiredForMatch));

                Debug.Log($"No user decks found - creating default deck with {groupsNeeded} groups (requiredForMatch={defaultRequiredForMatch})");
                string newDeckId = ImageManager.Instance.CreateDefaultDeck(groupsNeeded, defaultRequiredForMatch, "AutoDefaultDeck");

                InitializeGame(newDeckId);
                return;
            }

            // If decks exist and no current deck, start first deck
            if (currentDeck == null && ImageManager.Instance.memoryDecks != null && ImageManager.Instance.memoryDecks.Count > 0)
            {
                Debug.Log("Kein Deck initialisiert - starte automatisch Deck 0");
                InitializeGame(ImageManager.Instance.memoryDecks[0].deckId);
                return;
            }
        }

        // Wenn kein Deck vorhanden, trotzdem Front-Sprites als Fallback zuweisen
        if (currentDeck == null)
        {
            SetupCardImages();
        }
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
    public void InitializeGame(string deckId)
    {
        ImageManager imageManager = ImageManager.Instance;
        currentDeck = imageManager.GetDeck(deckId);

        if (currentDeck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return;
        }

        // Log the current deck to the console
        LogCurrentDeck();

        matchesFound = 0;
        revealedCards.Clear();
        checkingMatch = false;

        // Verteile Bilder auf Karten
        SetupCardImages();
        CalculateTotalMatches();
    }

    ///// <summary>
    ///// Berechnet die Gesamtzahl der Matches basierend auf dem aktuellen Deck
    ///// </summary>
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

        // Prepare a combined log to help diagnose sprite assignments
        StringBuilder assignmentLog = new StringBuilder();

        // groupIndex used so fallback sprites (when group has no imageIds) are stable per group
        int groupIndex = 0;

        // Shuffle the card list before assigning sprites so images are placed at random positions
        if (cards != null && cards.Count > 1)
        {
            ShuffleCards();
        }

        // Wenn kein Deck gesetzt ist, weise Default-Sprites als Vorderseiten zu (Fallback)
        if (currentDeck == null)
        {
            // Standard: Paare (2 Karten pro Gruppe)
            int matchSize = 2;
            int cardsCount = cards.Count;
            if (cardsCount <= 0)
            {
                ShuffleCards();
                return;
            }

            int groupsNeeded = cardsCount / matchSize;
            int cardIdx = 0;

            for (int g = 0; g < groupsNeeded; g++)
            {
                Sprite s = null;
                if (imageManager != null)
                {
                    s = image_manager_fallback(imageManager, g);
                }

                for (int m = 0; m < matchSize && cardIdx < cardsCount; m++)
                {
                    Card card = cards[cardIdx];
                    card.groupId = g.ToString();
                    card.SetFrontSprite(s);

                    assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source=fallback(index {g}), sprite={(s != null ? s.name : "null")}");

                    cardIdx++;
                }
            }

            // Falls ungerade Anzahl Karten: weise verbleibende Karten mit dem letzten Sprite
            while (cardIdx < cardsCount)
            {
                Sprite s = null;
                if (imageManager != null)
                    s = image_manager_fallback(imageManager, groupsNeeded);

                Card card = cards[cardIdx];
                card.groupId = groupsNeeded.ToString();
                card.SetFrontSprite(s);

                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source=fallback(index {groupsNeeded}), sprite={(s != null ? s.name : "null")}");

                cardIdx++;
            }

            // Ausgabe des kombinierten Logs
            Debug.Log(assignmentLog.ToString());
             return;
         }

        foreach (ImageManager.DeckGroup deckGroup in currentDeck.groups)
        {
            // Determine a single sprite for the whole group (so all cards in the group share the same image)
            Sprite groupSprite = null;
            string groupSourceDesc = "";

            if (deckGroup.imageIds != null && deckGroup.imageIds.Count > 0)
            {
                // Prefer the first imageId for the group so cards in the same group share the same pool image
                string imageId = deckGroup.imageIds[0];
                groupSprite = imageManager.LoadPoolImageSprite(imageId);
                groupSourceDesc = $"pool:{imageId}";

                if (groupSprite == null)
                {
                    Debug.LogWarning($"Sprite für Bild {imageId} konnte nicht geladen werden - verwende Default für Gruppe {deckGroup.groupId}");
                    if (imageManager != null)
                        groupSprite = image_manager_fallback(imageManager, groupIndex);
                    groupSourceDesc += ", fallback";
                }
            }
            else
            {
                // Gruppe hat keine Bilder -> nutze Default (ein Sprite pro Gruppe)
                if (imageManager != null)
                    groupSprite = image_manager_fallback(imageManager, groupIndex);
                groupSourceDesc = $"group-default(groupIndex {groupIndex})";
            }

            // Weise dieselbe Sprite-Instanz allen Karten dieser Gruppe zu
            for (int i = 0; i < deckGroup.requiredForMatch && cardIndex < cards.Count; i++)
            {
                if (cardIndex >= cards.Count)
                    break;

                Card card = cards[cardIndex];
                card.groupId = deckGroup.groupId; // deckGroup.groupId is string
                card.SetFrontSprite(groupSprite);

                assignmentLog.AppendLine($"Card {card.gameObject.name}: group={card.groupId}, source={groupSourceDesc}, sprite={(groupSprite != null ? groupSprite.name : "null")}");

                if (card.frontSprite == null)
                {
                    Debug.LogWarning($"Sprite für Karte an Index {cardIndex} konnte nicht gesetzt werden");
                }

                cardIndex++;
            }

            // move to next group index after assigning this group's cards
            groupIndex++;
         }

        // Ausgabe des kombinierten Logs
        Debug.Log(assignmentLog.ToString());
    }

    // Helper to get default sprite safely
    private Sprite image_manager_fallback(ImageManager imageManager, int index)
    {
        // Try ImageManager first
        if (imageManager != null)
        {
            try
            {
                Sprite s = imageManager.GetDefaultSpriteById(index);
                if (s != null)
                {
                    // Debug log removed to reduce noise when loading fallback sprites
                    return s;
                }
                else
                {
                    Debug.Log($"Fallback: ImageManager returned null for index {index}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Fallback: ImageManager.GetDefaultSpriteById threw: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Fallback: ImageManager.Instance is null");
        }

        // Fallback: load only the specific sprite by name to avoid loading the whole folder
        string basePath = "Sprites/diamond-pearl";
        // Try numeric naming: e.g. '1', '2', ... assume index 0 -> '1'
        string[] tryNames = new string[] { (index + 1).ToString(), index.ToString() };
        foreach (var name in tryNames)
        {
            try
            {
                Sprite rs = Resources.Load<Sprite>($"{basePath}/{name}");
                if (rs != null)
                {
                    Debug.Log($"Fallback: loaded single Resources sprite '{basePath}/{name}' for card index {index}");
                    return rs;
                }
                else
                {
                    Debug.Log($"Fallback: Resources.Load<Sprite> returned null for '{basePath}/{name}'");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Fallback: Resources.Load<Sprite> threw for '{basePath}/{name}': {e.Message}");
            }
        }

        Debug.LogWarning($"Fallback: No default sprite found for index {index}");
        return null;
    }

    /// <summary>
    /// Wird aufgerufen, wenn eine Karte aufgedeckt wird
    /// </summary>
    public void CardRevealed(Card revealedCard)
    {
        if (checkingMatch || revealedCards.Contains(revealedCard))
            return;

        // Wenn das Front-Sprite nicht gesetzt ist, ignoriere Klicks
        if (revealedCard.frontSprite == null)
        {
            Debug.LogWarning($"Karte {revealedCard.gameObject.name} hat kein frontSprite - Ignoriere Klick");
            return;
        }

        revealedCard.Reveal();
        revealedCards.Add(revealedCard);

        // Wenn kein Deck initialisiert ist, keine automatische Verbergung mehr
        if (currentDeck == null)
        {
            // previously would start HideRevealedFallback; now do nothing and wait for deck initialization
            return;
        }

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
        string groupId = revealedCards[0].groupId;
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

            // Log match result
            string matchedNames = string.Join(", ", revealedCards.ConvertAll(c => c.gameObject.name).ToArray());
            Debug.Log($"Match result: SUCCESS. Cards: {matchedNames} (groupId={groupId})");

            // Markiere die passenden Karten als gefunden (nicht interaktiv + transparent)
            foreach (var c in revealedCards)
            {
                c.MarkAsMatched();
            }

            // Entferne die Karten aus der internen Liste und deaktiviere GameObjects
            RemoveMatchingCards(groupId);

            matchesFound++;

            if (matchesFound >= totalMatches)
            {
                OnGameOver();
            }
        }
        else
        {
            // Log mismatch result
            string revealedNames = string.Join(", ", revealedCards.ConvertAll(c => c.gameObject.name).ToArray());
            Debug.Log($"Match result: FAIL. Revealed: {revealedNames}");

            // Kein Match - Karten zurück verbergen NACHDEM der Vergleich abgeschlossen ist
            yield return new WaitForSeconds(0.5f);
            foreach (Card card in revealedCards)
            {
                card.Hide();
            }
        }

        revealedCards.Clear();
        checkingMatch = false;
    }

    /// <summary>
    /// Entfernt alle Karten einer Gruppe vom Spielfeld
    /// </summary>
    void RemoveMatchingCards(string groupId)
    {
        // Instead of removing or deactivating the GameObjects (which makes GridLayoutGroup or other
        // layout components reflow and shift the remaining cards), keep the card GameObjects in place
        // and mark them as matched. MarkAsMatched() should make them non-interactive/transparent.
        // This preserves visual layout while still treating the group as found.

        foreach (Card card in cards)
        {
            if (card.groupId == groupId)
            {
                // mark as matched (already called earlier in flow, but safe to ensure here)
                try
                {
                    card.MarkAsMatched();
                }
                catch (System.Exception)
                {
                    // If Card doesn't implement MarkAsMatched, try to at least disable colliders to prevent interaction
                    var col2D = card.GetComponent<Collider2D>();
                    if (col2D != null) col2D.enabled = false;
                    var col3D = card.GetComponent<Collider>();
                    if (col3D != null) col3D.enabled = false;
                }

                // Additionally, disable any Button component if present to be safe
                var btn = card.GetComponent<UnityEngine.UI.Button>();
                if (btn != null) btn.interactable = false;

                // Optionally hide visuals but keep object active so layout doesn't change.
                // If Card provides a method to hide only visuals, prefer that. Otherwise leave as-is.
            }
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn das Spiel vorbei ist
    /// </summary>
    void OnGameOver()
    {
        Debug.Log("Spiel vorbei!");
        // Show game over UI if assigned
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

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

    /// <summary>
    /// Protokolliert die Details des aktuellen Decks in der Konsole
    /// </summary>
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

    public void quit()
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

