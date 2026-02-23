using System.Collections.Generic;
using UnityEngine;

public class DeckBuilder : MonoBehaviour
{
    public enum BuilderState
    {
        Configuration,
        ImageAssignment
    }

    private BuilderState currentState = BuilderState.Configuration;
    private ImageManager.SimplifiedDeckConfig currentConfig;
    private Dictionary<int, List<string>> imageAssignments;
    private int currentGroupIndex;

    public bool StartDeckConfiguration(string deckName, int groupCount, int groupSize, int requiredForMatch, bool useSameImages)
    {
        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogError("Deck-Name darf nicht leer sein");
            return false;
        }

        currentConfig = new ImageManager.SimplifiedDeckConfig(
            deckName,
            groupCount,
            groupSize,
            requiredForMatch,
            useSameImages
        );

        ImageManager.DeckValidationResult validation = ImageManager.Instance.ValidateSimplifiedDeck(currentConfig);

        if (!validation.isValid)
        {
            Debug.LogError($"Deck-Konfiguration ungültig: {validation.errorMessage}");

            if (validation.requiredImages > validation.availableImages)
            {
                int missingImages = validation.requiredImages - validation.availableImages;
                Debug.LogWarning($"Es fehlen {missingImages} Bilder. Bitte füge mehr Bilder zum Pool hinzu.");
            }

            return false;
        }

        imageAssignments = new Dictionary<int, List<string>>();
        for (int i = 0; i < groupCount; i++)
        {
            imageAssignments[i] = new List<string>();
        }

        currentState = BuilderState.ImageAssignment;
        currentGroupIndex = 0;

        Debug.Log($"Deck-Konfiguration gestartet: {deckName} mit {groupCount} Gruppen à {groupSize} Karten (requiredForMatch={requiredForMatch})");
        return true;
    }

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

        if (ImageManager.Instance.GetPoolImage(imageId) == null)
        {
            Debug.LogError($"Bild {imageId} existiert nicht im Pool");
            return false;
        }

        List<string> currentGroupImages = imageAssignments[currentGroupIndex];

        // Im klassischen Modus nur 1 Bild pro Gruppe erlauben
        int maxImagesPerGroup = currentConfig.useSameImages ? 1 : currentConfig.groupSize;

        if (currentGroupImages.Count >= maxImagesPerGroup)
        {
            Debug.LogError($"Gruppe {currentGroupIndex + 1} ist bereits vollständig ({maxImagesPerGroup} Bild(er))");
            return false;
        }

        if (currentGroupImages.Contains(imageId))
        {
            Debug.LogWarning($"Bild {imageId} ist bereits in Gruppe {currentGroupIndex + 1}");
            return false;
        }

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

        currentGroupImages.Add(imageId);
        Debug.Log($"Bild {imageId} zu Gruppe {currentGroupIndex + 1} hinzugefügt ({currentGroupImages.Count}/{maxImagesPerGroup})");

        return true;
    }

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

    public List<string> GetCurrentGroupImages()
    {
        if (currentGroupIndex >= currentConfig.groupCount)
        {
            return new List<string>();
        }

        return new List<string>(imageAssignments[currentGroupIndex]);
    }

    public bool IsCurrentGroupComplete()
    {
        if (currentGroupIndex >= currentConfig.groupCount)
        {
            return false;
        }

        // Im klassischen Modus nur 1 Bild pro Gruppe erforderlich
        int requiredImages = currentConfig.useSameImages ? 1 : currentConfig.groupSize;
        return imageAssignments[currentGroupIndex].Count >= requiredImages;
    }

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

    public List<ImageManager.PoolImage> GetAvailableImages()
    {
        if (currentConfig.useSameImages)
        {
            return ImageManager.Instance.imagePool;
        }

        HashSet<string> usedImages = new HashSet<string>();
        foreach (var kvp in imageAssignments)
        {
            foreach (string imageId in kvp.Value)
            {
                usedImages.Add(imageId);
            }
        }

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

    public void AutoFillWithSameImages()
    {
        if (!currentConfig.useSameImages)
        {
            Debug.LogWarning("Auto-Fill nur möglich wenn 'useSameImages' aktiviert ist");
            return;
        }

        if (imageAssignments[0].Count != currentConfig.groupSize)
        {
            Debug.LogError("Erste Gruppe muss erst vollständig ausgefüllt werden");
            return;
        }

        List<string> firstGroupImages = imageAssignments[0];

        for (int i = 1; i < currentConfig.groupCount; i++)
        {
            imageAssignments[i] = new List<string>(firstGroupImages);
        }

        currentGroupIndex = currentConfig.groupCount;
        Debug.Log($"Alle {currentConfig.groupCount} Gruppen mit den gleichen {firstGroupImages.Count} Bildern gefüllt");
    }

    public string FinalizeDeck()
    {
        if (currentState != BuilderState.ImageAssignment)
        {
            Debug.LogError("Deck-Konfiguration noch nicht abgeschlossen");
            return null;
        }

        // Im klassischen Modus nur 1 Bild pro Gruppe erforderlich
        int requiredImages = currentConfig.useSameImages ? 1 : currentConfig.groupSize;

        for (int i = 0; i < currentConfig.groupCount; i++)
        {
            if (imageAssignments[i].Count != requiredImages)
            {
                Debug.LogError($"Gruppe {i + 1} ist nicht vollständig ({imageAssignments[i].Count}/{requiredImages})");
                return null;
            }
        }

        string deckId = ImageManager.Instance.CreateSimplifiedDeck(currentConfig, imageAssignments);

        if (deckId != null)
        {
            Debug.Log($"Deck '{currentConfig.deckName}' erfolgreich erstellt mit ID {deckId}");
            
            // NEU: Automatisch als aktives Deck setzen
            ImageManager.Instance.SetSelectedDeck(deckId);
            
            Reset();
        }

        return deckId;
    }

    public void CancelDeckCreation()
    {
        Debug.Log("Deck-Erstellung abgebrochen");
        Reset();
    }

    private void Reset()
    {
        currentState = BuilderState.Configuration;
        currentConfig = null;
        imageAssignments = null;
        currentGroupIndex = 0;
    }

    public BuilderState GetCurrentState()
    {
        return currentState;
    }

    public int GetCurrentGroupIndex()
    {
        return currentGroupIndex;
    }

    public ImageManager.SimplifiedDeckConfig GetCurrentConfig()
    {
        return currentConfig;
    }

    public float GetProgress()
    {
        if (currentConfig == null || currentConfig.groupCount == 0)
        {
            return 0f;
        }

        // Im klassischen Modus nur 1 Bild pro Gruppe erforderlich
        int requiredImages = currentConfig.useSameImages ? 1 : currentConfig.groupSize;

        int completeGroups = 0;
        for (int i = 0; i < currentConfig.groupCount; i++)
        {
            if (imageAssignments[i].Count == requiredImages)
            {
                completeGroups++;
            }
        }

        return (float)completeGroups / currentConfig.groupCount;
    }

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