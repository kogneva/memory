using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller für die Deck-Erstellungs-UI
/// Verwaltet den Workflow der vereinfachten Deck-Erstellung
/// </summary>
public class DeckBuilder : MonoBehaviour
{
    /// <summary>
    /// Aktueller Status der Deck-Erstellung
    /// </summary>
    public enum BuilderState
    {
        Configuration,  // Schritt 1: Name, Gruppenanzahl, Gruppengröße eingeben
        ImageAssignment // Schritt 2: Bilder den Gruppen zuweisen
    }

    private BuilderState currentState = BuilderState.Configuration;
    private ImageManager.SimplifiedDeckConfig currentConfig;
    private Dictionary<int, List<string>> imageAssignments;
    private int currentGroupIndex = 0;

    // ============== SCHRITT 1: KONFIGURATION ==============

    /// <summary>
    /// Startet den Deck-Erstellungs-Prozess mit den Basis-Parametern
    /// </summary>
    /// <param name="deckName">Name des Decks</param>
    /// <param name="groupCount">Anzahl der Gruppen</param>
    /// <param name="requiredForMatch">Karten pro Match (Gruppengröße)</param>
    /// <param name="useSameImages">Alle Gruppen verwenden die gleichen Bilder</param>
    /// <returns>True wenn Konfiguration gültig ist, sonst False</returns>
    public bool StartDeckConfiguration(string deckName, int groupCount, int requiredForMatch, bool useSameImages)
    {
        // Validiere Eingaben
        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogError("Deck-Name darf nicht leer sein");
            return false;
        }

        // Erstelle Konfiguration
        currentConfig = new ImageManager.SimplifiedDeckConfig(
            deckName,
            groupCount,
            requiredForMatch,
            useSameImages
        );

        // Validiere mit ImageManager
        ImageManager.DeckValidationResult validation = ImageManager.Instance.ValidateSimplifiedDeck(currentConfig);
        
        if (!validation.isValid)
        {
            Debug.LogError($"Deck-Konfiguration ungültig: {validation.errorMessage}");
            
            // Zeige dem Benutzer eine hilfreiche Fehlermeldung
            if (validation.requiredImages > validation.availableImages)
            {
                int missingImages = validation.requiredImages - validation.availableImages;
                Debug.LogWarning($"Es fehlen {missingImages} Bilder. Bitte füge mehr Bilder zum Pool hinzu.");
            }
            
            return false;
        }

        // Initialisiere Bild-Zuweisungen
        imageAssignments = new Dictionary<int, List<string>>();
        for (int i = 0; i < groupCount; i++)
        {
            imageAssignments[i] = new List<string>();
        }

        // Wechsle zu Bild-Zuweisungs-Schritt
        currentState = BuilderState.ImageAssignment;
        currentGroupIndex = 0;

        Debug.Log($"Deck-Konfiguration gestartet: {deckName} mit {groupCount} Gruppen à {requiredForMatch} Karten");
        return true;
    }

    // ============== SCHRITT 2: BILD-ZUWEISUNG ==============

    /// <summary>
    /// Fügt ein Bild zur aktuellen Gruppe hinzu
    /// Validiert, dass das Bild noch nicht verwendet wurde (außer bei useSameImages=true)
    /// </summary>
    public bool AddImageToCurrentGroup(string imageId)
    {
        if (currentState != BuilderState.ImageAssignment)
        {
            Debug.LogError("Nicht im Bild-Zuweisungs-Modus");
            return false;
        }

        if (currentGroupIndex >= currentConfig.groupCount)
        {
            Debug.LogError("Alle Gruppen sind bereits vollständig");
            return false;
        }

        // Prüfe, ob das Bild existiert
        if (ImageManager.Instance.GetPoolImage(imageId) == null)
        {
            Debug.LogError($"Bild {imageId} existiert nicht im Pool");
            return false;
        }

        List<string> currentGroupImages = imageAssignments[currentGroupIndex];

        // Prüfe, ob die Gruppe bereits voll ist
        if (currentGroupImages.Count >= currentConfig.requiredForMatch)
        {
            Debug.LogError($"Gruppe {currentGroupIndex + 1} ist bereits vollständig ({currentConfig.requiredForMatch} Bilder)");
            return false;
        }

        // Prüfe, ob das Bild bereits in dieser Gruppe ist
        if (currentGroupImages.Contains(imageId))
        {
            Debug.LogWarning($"Bild {imageId} ist bereits in Gruppe {currentGroupIndex + 1}");
            return false;
        }

        // Wenn nicht "useSameImages", prüfe ob das Bild in anderen Gruppen verwendet wird
        if (!currentConfig.useSameImages)
        {
            foreach (KeyValuePair<int, List<string>> kvp in imageAssignments)
            {
                if (kvp.Key != currentGroupIndex && kvp.Value.Contains(imageId))
                {
                    Debug.LogError($"Bild {imageId} wird bereits in Gruppe {kvp.Key + 1} verwendet");
                    return false;
                }
            }
        }

        // Füge Bild hinzu
        currentGroupImages.Add(imageId);
        Debug.Log($"Bild {imageId} zu Gruppe {currentGroupIndex + 1} hinzugefügt ({currentGroupImages.Count}/{currentConfig.requiredForMatch})");

        return true;
    }

    /// <summary>
    /// Entfernt ein Bild aus der aktuellen Gruppe
    /// </summary>
    public bool RemoveImageFromCurrentGroup(string imageId)
    {
        if (currentState != BuilderState.ImageAssignment)
        {
            Debug.LogError("Nicht im Bild-Zuweisungs-Modus");
            return false;
        }

        if (currentGroupIndex >= currentConfig.groupCount)
        {
            return false;
        }

        List<string> currentGroupImages = imageAssignments[currentGroupIndex];
        
        if (currentGroupImages.Remove(imageId))
        {
            Debug.Log($"Bild {imageId} aus Gruppe {currentGroupIndex + 1} entfernt");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gibt die Bilder der aktuellen Gruppe zurück
    /// </summary>
    public List<string> GetCurrentGroupImages()
    {
        if (currentGroupIndex >= currentConfig.groupCount)
        {
            return new List<string>();
        }

        return new List<string>(imageAssignments[currentGroupIndex]);
    }

    /// <summary>
    /// Prüft, ob die aktuelle Gruppe vollständig ist
    /// </summary>
    public bool IsCurrentGroupComplete()
    {
        if (currentGroupIndex >= currentConfig.groupCount)
        {
            return false;
        }

        return imageAssignments[currentGroupIndex].Count == currentConfig.requiredForMatch;
    }

    /// <summary>
    /// Wechselt zur nächsten Gruppe
    /// </summary>
    public bool NextGroup()
    {
        if (!IsCurrentGroupComplete())
        {
            Debug.LogError($"Gruppe {currentGroupIndex + 1} ist noch nicht vollständig");
            return false;
        }

        currentGroupIndex++;
        
        if (currentGroupIndex >= currentConfig.groupCount)
        {
            Debug.Log("Alle Gruppen sind vollständig - bereit zum Erstellen");
            return true;
        }

        Debug.Log($"Wechsle zu Gruppe {currentGroupIndex + 1}");
        return true;
    }

    /// <summary>
    /// Geht zur vorherigen Gruppe zurück
    /// </summary>
    public bool PreviousGroup()
    {
        if (currentGroupIndex <= 0)
        {
            Debug.LogWarning("Bereits bei der ersten Gruppe");
            return false;
        }

        currentGroupIndex--;
        Debug.Log($"Zurück zu Gruppe {currentGroupIndex + 1}");
        return true;
    }

    /// <summary>
    /// Gibt alle verfügbaren Bilder zurück, die noch nicht verwendet wurden
    /// </summary>
    public List<ImageManager.PoolImage> GetAvailableImages()
    {
        if (currentConfig.useSameImages)
        {
            // Wenn alle Gruppen die gleichen Bilder verwenden, sind alle Pool-Bilder verfügbar
            return ImageManager.Instance.imagePool;
        }

        // Sammle alle bereits verwendeten Bilder
        HashSet<string> usedImages = new HashSet<string>();
        foreach (var kvp in imageAssignments)
        {
            foreach (string imageId in kvp.Value)
            {
                usedImages.Add(imageId);
            }
        }

        // Filtere verfügbare Bilder
        List<ImageManager.PoolImage> availableImages = new List<ImageManager.PoolImage>();
        foreach (var poolImage in ImageManager.Instance.imagePool)
        {
            if (!usedImages.Contains(poolImage.imageId))
            {
                availableImages.Add(poolImage);
            }
        }

        return availableImages;
    }

    /// <summary>
    /// Wenn "useSameImages" aktiviert ist, kopiert die Bilder der ersten Gruppe
    /// automatisch in alle anderen Gruppen
    /// </summary>
    public void AutoFillWithSameImages()
    {
        if (!currentConfig.useSameImages)
        {
            Debug.LogWarning("Auto-Fill nur möglich wenn 'useSameImages' aktiviert ist");
            return;
        }

        if (imageAssignments[0].Count != currentConfig.requiredForMatch)
        {
            Debug.LogError("Erste Gruppe muss erst vollständig ausgefüllt werden");
            return;
        }

        // Kopiere Bilder der ersten Gruppe in alle anderen
        List<string> firstGroupImages = imageAssignments[0];
        
        for (int i = 1; i < currentConfig.groupCount; i++)
        {
            imageAssignments[i] = new List<string>(firstGroupImages);
        }

        currentGroupIndex = currentConfig.groupCount; // Markiere als fertig
        Debug.Log($"Alle {currentConfig.groupCount} Gruppen mit den gleichen {firstGroupImages.Count} Bildern gefüllt");
    }

    /// <summary>
    /// Erstellt das Deck mit den konfigurierten Einstellungen
    /// </summary>
    public string FinalizeDeck()
    {
        if (currentState != BuilderState.ImageAssignment)
        {
            Debug.LogError("Deck-Konfiguration noch nicht abgeschlossen");
            return null;
        }

        // Prüfe, ob alle Gruppen vollständig sind
        for (int i = 0; i < currentConfig.groupCount; i++)
        {
            if (imageAssignments[i].Count != currentConfig.requiredForMatch)
            {
                Debug.LogError($"Gruppe {i + 1} ist nicht vollständig ({imageAssignments[i].Count}/{currentConfig.requiredForMatch})");
                return null;
            }
        }

        // Erstelle Deck
        string deckId = ImageManager.Instance.CreateSimplifiedDeck(currentConfig, imageAssignments);
        
        if (deckId != null)
        {
            Debug.Log($"Deck '{currentConfig.deckName}' erfolgreich erstellt mit ID {deckId}");
            Reset();
        }

        return deckId;
    }

    /// <summary>
    /// Bricht die aktuelle Deck-Erstellung ab
    /// </summary>
    public void CancelDeckCreation()
    {
        Debug.Log("Deck-Erstellung abgebrochen");
        Reset();
    }

    /// <summary>
    /// Setzt den Builder zurück
    /// </summary>
    private void Reset()
    {
        currentState = BuilderState.Configuration;
        currentConfig = null;
        imageAssignments = null;
        currentGroupIndex = 0;
    }

    // ============== HILFS-METHODEN FÜR UI ==============

    /// <summary>
    /// Gibt den aktuellen Status zurück
    /// </summary>
    public BuilderState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Gibt den aktuellen Gruppen-Index zurück (0-basiert)
    /// </summary>
    public int GetCurrentGroupIndex()
    {
        return currentGroupIndex;
    }

    /// <summary>
    /// Gibt die aktuelle Konfiguration zurück
    /// </summary>
    public ImageManager.SimplifiedDeckConfig GetCurrentConfig()
    {
        return currentConfig;
    }

    /// <summary>
    /// Gibt den Fortschritt zurück (wie viele Gruppen bereits vollständig sind)
    /// </summary>
    public float GetProgress()
    {
        if (currentConfig == null || currentConfig.groupCount == 0)
        {
            return 0f;
        }

        int completeGroups = 0;
        for (int i = 0; i < currentConfig.groupCount; i++)
        {
            if (imageAssignments[i].Count == currentConfig.requiredForMatch)
            {
                completeGroups++;
            }
        }

        return (float)completeGroups / currentConfig.groupCount;
    }

    /// <summary>
    /// Gibt eine Fortschritts-Beschreibung zurück (z.B. "Gruppe 2 von 5")
    /// </summary>
    public string GetProgressDescription()
    {
        if (currentConfig == null)
        {
            return "Keine Konfiguration";
        }

        if (currentState == BuilderState.Configuration)
        {
            return "Konfiguration";
        }

        return $"Gruppe {currentGroupIndex + 1} von {currentConfig.groupCount}";
    }
}
