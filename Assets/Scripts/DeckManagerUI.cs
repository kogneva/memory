using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Verwaltet die Deck-Übersicht mit Kacheln für jedes Deck.
/// Nur ein Deck kann aktiv sein (Radio-Button-Logik).
/// </summary>
public class DeckManagerUI : MonoBehaviour
{
    [Header("UI Referenzen")]
    [Tooltip("Container für die Deck-Kacheln (z.B. ein GridLayoutGroup)")]
    [SerializeField]
    private Transform deckTilesContainer;

    [Tooltip("Prefab für eine einzelne Deck-Kachel")]
    [SerializeField]
    private GameObject deckTilePrefab;

    [Header("Panels")]
    [SerializeField]
    private GameObject deckManagerPanel;

    [SerializeField]
    private GameObject mainMenuPanel;

    // Liste aller aktiven Toggle-Referenzen für Radio-Button-Logik
    private List<DeckTileData> deckTiles = new List<DeckTileData>();

    private class DeckTileData
    {
        public string deckId;
        public Toggle toggle;
        public GameObject tileObject;
    }

    private void OnEnable()
    {
        RefreshDeckList();
    }

    /// <summary>
    /// Aktualisiert die Deck-Liste und erstellt Kacheln für alle Decks.
    /// </summary>
    public void RefreshDeckList()
    {
        ClearDeckTiles();

        if (ImageManager.Instance == null)
        {
            Debug.LogWarning("DeckManagerUI: ImageManager.Instance ist null");
            return;
        }

        string selectedDeckId = ImageManager.Instance.GetSelectedDeckId();

        foreach (var deck in ImageManager.Instance.memoryDecks)
        {
            CreateDeckTile(deck, deck.deckId == selectedDeckId);
        }
    }

    private void ClearDeckTiles()
    {
        foreach (var tileData in deckTiles)
        {
            if (tileData.tileObject != null)
            {
                Destroy(tileData.tileObject);
            }
        }
        deckTiles.Clear();
    }

    private void CreateDeckTile(ImageManager.MemoryDeck deck, bool isSelected)
    {
        if (deckTilePrefab == null || deckTilesContainer == null)
        {
            Debug.LogError("DeckManagerUI: deckTilePrefab oder deckTilesContainer nicht zugewiesen");
            return;
        }

        GameObject tile = Instantiate(deckTilePrefab, deckTilesContainer);
        tile.name = $"DeckTile_{deck.deckName}";

        // Deck-Name setzen
        var nameText = tile.transform.Find("DeckNameText")?.GetComponent<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = deck.deckName;
        }

        // Konfiguration anzeigen
        var configText = tile.transform.Find("DeckConfigText")?.GetComponent<TMP_Text>();
        if (configText != null)
        {
            string modeText = deck.useSameImages ? "Klassisch" : "Thematisch";
            configText.text = $"{deck.GroupCount} Gruppen × {deck.groupSize} Karten | Match: {deck.requiredForMatch} | {modeText}";
        }

        // Toggle für Auswahl
        var toggle = tile.GetComponentInChildren<Toggle>();
        if (toggle != null)
        {
            // Ereignis erst entfernen, dann Wert setzen, dann Ereignis hinzufügen
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = isSelected;

            string deckId = deck.deckId; // Lokale Kopie für Closure
            toggle.onValueChanged.AddListener((isOn) => OnDeckToggleChanged(deckId, isOn));
        }

        // Löschen-Button (optional)
        var deleteButton = tile.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (deleteButton != null)
        {
            string deckId = deck.deckId;
            deleteButton.onClick.AddListener(() => OnDeleteDeck(deckId));
        }

        deckTiles.Add(new DeckTileData
        {
            deckId = deck.deckId,
            toggle = toggle,
            tileObject = tile
        });
    }

    private void OnDeckToggleChanged(string deckId, bool isOn)
    {
        if (!isOn)
        {
            // Wenn das aktive Deck abgewählt wird, prüfen ob es das einzige war
            string currentSelected = ImageManager.Instance?.GetSelectedDeckId();
            if (currentSelected == deckId)
            {
                // Auswahl aufheben
                ImageManager.Instance?.SetSelectedDeck(null);
                Debug.Log("Kein Deck mehr ausgewählt");
            }
            return;
        }

        // Neues Deck ausgewählt - alle anderen deaktivieren
        foreach (var tileData in deckTiles)
        {
            if (tileData.deckId != deckId && tileData.toggle != null)
            {
                // Listener temporär entfernen um Rekursion zu vermeiden
                tileData.toggle.onValueChanged.RemoveAllListeners();
                tileData.toggle.isOn = false;
                
                // Listener wieder hinzufügen
                string otherId = tileData.deckId;
                tileData.toggle.onValueChanged.AddListener((on) => OnDeckToggleChanged(otherId, on));
            }
        }

        // Ausgewähltes Deck speichern
        ImageManager.Instance?.SetSelectedDeck(deckId);
        Debug.Log($"Deck '{deckId}' ausgewählt");
    }

    private void OnDeleteDeck(string deckId)
    {
        if (ImageManager.Instance == null) return;

        // Prüfen ob das zu löschende Deck das aktive ist
        if (ImageManager.Instance.GetSelectedDeckId() == deckId)
        {
            ImageManager.Instance.SetSelectedDeck(null);
        }

        ImageManager.Instance.RemoveDeck(deckId);
        RefreshDeckList();
        Debug.Log($"Deck '{deckId}' gelöscht");
    }

    /// <summary>
    /// Schließt die Deck-Übersicht und kehrt zum Hauptmenü zurück.
    /// </summary>
    public void OnBackClick()
    {
        if (deckManagerPanel != null)
            deckManagerPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
}