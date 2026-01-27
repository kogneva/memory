using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;

public class ImageManager : MonoBehaviour
{
    public static ImageManager Instance;

    /// <summary>
    /// Ein einzelnes Bild im Pool
    /// </summary>
    [System.Serializable]
    public class PoolImage
    {
        public string imageId; // GUID string
        public string imagePath;
        public string imageName;
        [NonSerialized] public Sprite sprite; // Cache für geladenes Sprite
    }

    /// <summary>
    /// Eine Gruppe innerhalb eines Memory-Decks
    /// Referenziert Bilder aus dem Pool
    /// </summary>
    [System.Serializable]
    public class DeckGroup
    {
        public string groupId; // GUID string
        public string groupName;
        public int requiredForMatch = 2;
        public List<string> imageIds = new List<string>(); // GUID strings der Bilder aus dem Pool
    }

    /// <summary>
    /// Ein komplettes Memory-Deck mit mehreren Gruppen
    /// </summary>
    [System.Serializable]
    public class MemoryDeck
    {
        public string deckId; // GUID string
        public string deckName;
        public List<DeckGroup> groups = new List<DeckGroup>();
    }

    /// <summary>
    /// Konfiguration für die vereinfachte Deck-Erstellung
    /// Alle Gruppen haben die gleiche Größe
    /// </summary>
    [System.Serializable]
    public class SimplifiedDeckConfig
    {
        public string deckName;
        public int groupCount;           // Anzahl der Gruppen
        public int requiredForMatch;     // Karten pro Match (Gruppengröße)
        public bool useSameImages;       // Alle Gruppen verwenden die gleichen Bilder
        
        public SimplifiedDeckConfig(string deckName, int groupCount, int requiredForMatch, bool useSameImages = false)
        {
            this.deckName = deckName;
            this.groupCount = groupCount;
            this.requiredForMatch = requiredForMatch;
            this.useSameImages = useSameImages;
        }
    }

    /// <summary>
    /// Validierungsergebnis für Deck-Erstellung
    /// </summary>
    public class DeckValidationResult
    {
        public bool isValid;
        public string errorMessage;
        public int requiredImages;
        public int availableImages;

        public DeckValidationResult(bool isValid, string errorMessage = "", int requiredImages = 0, int availableImages = 0)
        {
            this.isValid = isValid;
            this.errorMessage = errorMessage;
            this.requiredImages = requiredImages;
            this.availableImages = availableImages;
        }
    }

    // Zentrale Bild-Verwaltung
    public List<PoolImage> imagePool = new List<PoolImage>();
    public List<MemoryDeck> memoryDecks = new List<MemoryDeck>();

    private const string PLAYERPREFS_POOL_KEY = "IMAGE_POOL";
    private const string PLAYERPREFS_DECKS_KEY = "MEMORY_DECKS";

    // Default sprites loaded from Resources/diamond-pearl
    private const string DEFAULT_RESOURCE_FOLDER = "Sprites/diamond-pearl";
    private List<Sprite> defaultSprites = new List<Sprite>();

    void Awake()
    {
        Debug.Log("ImageManager.Awake() wird ausgeführt");
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ImageManager Instance gesetzt und DontDestroyOnLoad aktiviert");
            
            // Lade Default-Sprites zunächst, damit sie als Fallback verfügbar sind
            LoadDefaultSpritesFromResources();
            LoadLibrary();
            
            Debug.Log($"ImageManager initialisiert. Pool hat {imagePool.Count} Bilder, {memoryDecks.Count} Decks");
        }
        else
        {
            Debug.Log("ImageManager existiert bereits - zerstöre Duplikat");
            Destroy(gameObject);
        }
    }

    // ============== BILD-POOL VERWALTUNG ==============

    /// <summary>
    /// Lädt ein Bild aus der Galerie in den Pool (Einzel-Auswahl)
    /// </summary>
    public void AddImageToPool(Action<string> onImageAdded = null)
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            AddImageFileToPool(path, onImageAdded);
        });
    }

    /// <summary>
    /// Lädt mehrere Bilder aus der Galerie in den Pool (falls unterstützt)
    /// </summary>
    public void AddImagesToPool(Action<List<string>> onImagesAdded = null)
    {
        NativeGallery.GetImagesFromGallery((paths) =>
        {
            if (paths == null || paths.Length == 0)
            {
                return;
            }

            List<string> added = new List<string>();
            foreach (string path in paths)
            {
                string addedId = AddImageFileToPool(path, null);
                if (!string.IsNullOrEmpty(addedId)) added.Add(addedId);
            }

            if (added.Count > 0)
            {
                SaveLibrary();
                onImagesAdded?.Invoke(added);
            }
        });
    }

    // gemeinsame Logik zum Kopieren und Hinzufügen
    private string AddImageFileToPool(string sourcePath, Action<string> onImageAdded)
    {
        try
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return null;

            string fileName = Path.GetFileName(sourcePath);
            string targetFileName = $"pool_image_{Guid.NewGuid()}_{fileName}";
            string targetPath = Path.Combine(Application.persistentDataPath, targetFileName);

            File.Copy(sourcePath, targetPath, true);

            string newId = System.Guid.NewGuid().ToString();

            PoolImage poolImage = new PoolImage
            {
                imageId = newId,
                imagePath = targetPath,
                imageName = fileName
            };

            imagePool.Add(poolImage);

            SaveLibrary();
            onImageAdded?.Invoke(newId);
            Debug.Log($"Bild {fileName} mit ID {newId} zum Pool hinzugefügt");
            return newId;
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Hinzufügen des Bildes: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Entfernt ein Bild aus dem Pool
    /// </summary>
    public void RemoveImageFromPool(string imageId)
    {
        PoolImage poolImage = imagePool.Find(img => img.imageId == imageId);
        if (poolImage != null)
        {
            // Lösche aus allen Decks
            foreach (MemoryDeck deck in memoryDecks)
            {
                foreach (DeckGroup group in deck.groups)
                {
                    group.imageIds.Remove(imageId);
                }
            }

            // Lösche die physikalische Datei
            if (File.Exists(poolImage.imagePath))
            {
                try
                {
                    File.Delete(poolImage.imagePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Fehler beim Löschen der Datei: {e.Message}");
                }
            }

            imagePool.Remove(poolImage);
            SaveLibrary();
            Debug.Log($"Bild {imageId} aus Pool entfernt");
        }
    }

    /// <summary>
    /// Gibt ein Bild aus dem Pool zurück
    /// </summary>
    public PoolImage GetPoolImage(string imageId)
    {
        return imagePool.Find(img => img.imageId == imageId);
    }

    /// <summary>
    /// Lädt das Sprite für ein Bild aus dem Pool
    /// </summary>
    public Sprite LoadPoolImageSprite(string imageId)
    {
        PoolImage poolImage = GetPoolImage(imageId);
        if (poolImage == null)
        {
            Debug.LogWarning($"Bild mit ID {imageId} nicht gefunden, verwende Default-Sprite");
            return GetDefaultSpriteByGuid(imageId);
        }

        // Verwende Cache wenn verfügbar
        if (poolImage.sprite != null)
            return poolImage.sprite;

        Sprite s = LoadSpriteFromPath(poolImage.imagePath);
        if (s == null)
        {
            Debug.LogWarning($"Konnte Sprite von Pfad {poolImage.imagePath} nicht laden, verwende Default-Sprite");
            return GetDefaultSpriteByGuid(poolImage.imageId);
        }

        poolImage.sprite = s;
        return s;
    }

    private Sprite LoadSpriteFromPath(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogWarning($"Bilddatei nicht gefunden: {imagePath}");
            return null;
        }

        try
        {
            byte[] data = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            texture.LoadImage(data);

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Laden des Sprites: {e.Message}");
            return null;
        }
    }

    public Sprite GetDefaultSpriteById(int imageId)
    {
        if (defaultSprites == null || defaultSprites.Count == 0)
            return null;

        int idx = Mathf.Abs(imageId) % defaultSprites.Count;
        return defaultSprites[idx];
    }

    // New helper: map GUID string to a default sprite deterministically
    public Sprite GetDefaultSpriteByGuid(string guidString)
    {
        if (defaultSprites == null || defaultSprites.Count == 0)
            return null;

        int idx = Math.Abs(guidString.GetHashCode()) % defaultSprites.Count;
        return defaultSprites[idx];
    }

    private void LoadDefaultSpritesFromResources()
    {
        try
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(DEFAULT_RESOURCE_FOLDER);
            if (sprites != null && sprites.Length > 0)
            {
                defaultSprites = new List<Sprite>(sprites);
                // Silent: do not log per-sprite or count information to reduce console noise in production.
            }
            else
            {
                // Keep warnings when nothing is found
                Debug.LogWarning($"Keine Default-Sprites in Resources/{DEFAULT_RESOURCE_FOLDER} gefunden");
                Debug.LogWarning($"Bitte prüfen: Datei(en) unter Assets/Resources/{DEFAULT_RESOURCE_FOLDER}/ vorhanden und Import-Typ = 'Sprite (2D and UI)'");
                defaultSprites = new List<Sprite>();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Fehler beim Laden der Default-Sprites: {e.Message}");
            defaultSprites = new List<Sprite>();
        }
    }

    // ============== VEREINFACHTE DECK-ERSTELLUNG ==============

    /// <summary>
    /// Validiert die Konfiguration für ein vereinfachtes Deck
    /// Prüft, ob genug Bilder im Pool vorhanden sind
    /// </summary>
    public DeckValidationResult ValidateSimplifiedDeck(SimplifiedDeckConfig config)
    {
        // Mindestanzahl Gruppen: 1
        if (config.groupCount < 1)
        {
            return new DeckValidationResult(false, "Mindestens 1 Gruppe erforderlich");
        }

        // Mindestgröße pro Gruppe: 2
        if (config.requiredForMatch < 2)
        {
            return new DeckValidationResult(false, "Mindestens 2 Karten pro Match erforderlich");
        }

        int requiredImages;
        if (config.useSameImages)
        {
            // Wenn alle Gruppen die gleichen Bilder verwenden
            // benötigen wir nur requiredForMatch Bilder
            requiredImages = config.requiredForMatch;
        }
        else
        {
            // Jede Gruppe braucht unterschiedliche Bilder
            // Innerhalb eines Decks darf ein Bild nur in einer Gruppe vorkommen
            requiredImages = config.groupCount * config.requiredForMatch;
        }

        int availableImages = imagePool != null ? imagePool.Count : 0;

        if (availableImages < requiredImages)
        {
            return new DeckValidationResult(
                false,
                $"Nicht genug Bilder im Pool. Benötigt: {requiredImages}, Verfügbar: {availableImages}",
                requiredImages,
                availableImages
            );
        }

        return new DeckValidationResult(true, "", requiredImages, availableImages);
    }

    /// <summary>
    /// Erstellt ein vereinfachtes Deck mit gleich großen Gruppen
    /// Alle Gruppen haben die gleiche requiredForMatch-Anzahl
    /// Jedes Bild kommt nur in einer Gruppe vor (außer bei useSameImages=true)
    /// </summary>
    /// <param name="config">Konfiguration für das Deck</param>
    /// <param name="imageAssignments">Optionale manuelle Zuordnung von Bildern zu Gruppen. 
    /// Dictionary key = groupIndex (0-basiert), value = Liste von imageIds für diese Gruppe.
    /// Wenn null, werden Bilder automatisch aus dem Pool zugewiesen.</param>
    /// <returns>Deck-ID bei Erfolg, null bei Fehler</returns>
    public string CreateSimplifiedDeck(SimplifiedDeckConfig config, Dictionary<int, List<string>> imageAssignments = null)
    {
        // Validierung
        DeckValidationResult validation = ValidateSimplifiedDeck(config);
        if (!validation.isValid)
        {
            Debug.LogError($"Deck-Validierung fehlgeschlagen: {validation.errorMessage}");
            return null;
        }

        // Erstelle neues Deck
        string deckId = Guid.NewGuid().ToString();
        MemoryDeck deck = new MemoryDeck
        {
            deckId = deckId,
            deckName = config.deckName,
            groups = new List<DeckGroup>()
        };

        // Wenn manuelle Zuweisungen vorhanden sind, validiere sie
        if (imageAssignments != null)
        {
            if (!ValidateImageAssignments(config, imageAssignments))
            {
                Debug.LogError("Ungültige Bild-Zuweisungen");
                return null;
            }
        }

        // Erstelle Gruppen
        for (int i = 0; i < config.groupCount; i++)
        {
            DeckGroup group = new DeckGroup
            {
                groupId = Guid.NewGuid().ToString(),
                groupName = $"Gruppe {i + 1}",
                requiredForMatch = config.requiredForMatch,
                imageIds = new List<string>()
            };

            // Füge Bilder zur Gruppe hinzu
            if (imageAssignments != null && imageAssignments.ContainsKey(i))
            {
                // Verwende manuelle Zuweisungen
                group.imageIds.AddRange(imageAssignments[i]);
            }
            else
            {
                // Automatische Zuweisung (wird später gemacht)
                group.imageIds = new List<string>();
            }

            deck.groups.Add(group);
        }

        // Wenn keine manuellen Zuweisungen, füge automatisch Bilder hinzu
        if (imageAssignments == null)
        {
            AutoAssignImagesToGroups(deck, config);
        }

        // Speichere Deck
        memoryDecks.Add(deck);
        SaveLibrary();
        
        Debug.Log($"Vereinfachtes Deck '{config.deckName}' erstellt mit {config.groupCount} Gruppen (je {config.requiredForMatch} Karten)");
        return deckId;
    }

    /// <summary>
    /// Validiert manuelle Bild-Zuweisungen
    /// </summary>
    private bool ValidateImageAssignments(SimplifiedDeckConfig config, Dictionary<int, List<string>> imageAssignments)
    {
        HashSet<string> usedImages = new HashSet<string>();

        for (int i = 0; i < config.groupCount; i++)
        {
            if (!imageAssignments.ContainsKey(i))
            {
                Debug.LogError($"Gruppe {i} fehlt in den Bild-Zuweisungen");
                return false;
            }

            List<string> groupImages = imageAssignments[i];
            
            // Prüfe Anzahl der Bilder
            if (groupImages.Count != config.requiredForMatch)
            {
                Debug.LogError($"Gruppe {i} hat {groupImages.Count} Bilder, benötigt aber {config.requiredForMatch}");
                return false;
            }

            // Prüfe, ob alle Bilder im Pool existieren
            foreach (string imageId in groupImages)
            {
                if (!imagePool.Exists(img => img.imageId == imageId))
                {
                    Debug.LogError($"Bild {imageId} existiert nicht im Pool");
                    return false;
                }

                // Wenn nicht "useSameImages", prüfe auf Duplikate innerhalb des Decks
                if (!config.useSameImages)
                {
                    if (usedImages.Contains(imageId))
                    {
                        Debug.LogError($"Bild {imageId} wird in mehreren Gruppen verwendet (useSameImages=false)");
                        return false;
                    }
                    usedImages.Add(imageId);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Weist automatisch Bilder den Gruppen zu
    /// </summary>
    private void AutoAssignImagesToGroups(MemoryDeck deck, SimplifiedDeckConfig config)
    {
        if (imagePool == null || imagePool.Count == 0)
        {
            Debug.LogWarning("Kein Bild im Pool - Gruppen bleiben leer (Default-Sprites werden verwendet)");
            return;
        }

        List<string> availableImageIds = imagePool.Select(img => img.imageId).ToList();
        int imageIndex = 0;

        if (config.useSameImages)
        {
            // Alle Gruppen verwenden die gleichen Bilder
            List<string> sharedImages = availableImageIds.Take(config.requiredForMatch).ToList();
            
            foreach (DeckGroup group in deck.groups)
            {
                group.imageIds.AddRange(sharedImages);
            }
            
            Debug.Log($"Alle {deck.groups.Count} Gruppen verwenden die gleichen {sharedImages.Count} Bilder");
        }
        else
        {
            // Jede Gruppe bekommt unterschiedliche Bilder
            foreach (DeckGroup group in deck.groups)
            {
                for (int i = 0; i < config.requiredForMatch && imageIndex < availableImageIds.Count; i++)
                {
                    group.imageIds.Add(availableImageIds[imageIndex]);
                    imageIndex++;
                }
            }
            
            Debug.Log($"Bilder automatisch auf {deck.groups.Count} Gruppen verteilt (jedes Bild einmalig verwendet)");
        }
    }

    /// <summary>
    /// Fügt ein Bild zu einer Gruppe in einem Deck hinzu
    /// Validiert, dass das Bild nicht bereits in einer anderen Gruppe des Decks vorkommt
    /// </summary>
    public bool AddImageToGroupSafe(string deckId, string groupId, string imageId)
    {
        MemoryDeck deck = GetDeck(deckId);
        if (deck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return false;
        }

        DeckGroup targetGroup = deck.groups.Find(g => g.groupId == groupId);
        if (targetGroup == null)
        {
            Debug.LogError($"Gruppe mit ID {groupId} nicht gefunden");
            return false;
        }

        if (!imagePool.Exists(img => img.imageId == imageId))
        {
            Debug.LogError($"Bild mit ID {imageId} nicht im Pool vorhanden");
            return false;
        }

        // Prüfe, ob das Bild bereits in einer anderen Gruppe dieses Decks verwendet wird
        foreach (DeckGroup group in deck.groups)
        {
            if (group.groupId != groupId && group.imageIds.Contains(imageId))
            {
                Debug.LogError($"Bild {imageId} wird bereits in Gruppe {group.groupName} verwendet");
                return false;
            }
        }

        // Prüfe, ob das Bild bereits in dieser Gruppe ist
        if (targetGroup.imageIds.Contains(imageId))
        {
            Debug.LogWarning($"Bild {imageId} ist bereits in Gruppe {targetGroup.groupName}");
            return false;
        }

        targetGroup.imageIds.Add(imageId);
        SaveLibrary();
        Debug.Log($"Bild {imageId} zu Gruppe {targetGroup.groupName} hinzugefügt");
        return true;
    }

    /// <summary>
    /// Gibt die verwendeten Bilder in einem Deck zurück
    /// </summary>
    public HashSet<string> GetUsedImagesInDeck(string deckId)
    {
        HashSet<string> usedImages = new HashSet<string>();
        MemoryDeck deck = GetDeck(deckId);
        
        if (deck != null)
        {
            foreach (DeckGroup group in deck.groups)
            {
                foreach (string imageId in group.imageIds)
                {
                    usedImages.Add(imageId);
                }
            }
        }

        return usedImages;
    }

    /// <summary>
    /// Gibt alle verfügbaren Bilder zurück, die noch nicht in einem bestimmten Deck verwendet werden
    /// </summary>
    public List<PoolImage> GetAvailableImagesForDeck(string deckId)
    {
        HashSet<string> usedImages = GetUsedImagesInDeck(deckId);
        
        return imagePool.Where(img => !usedImages.Contains(img.imageId)).ToList();
    }

    // ============== ALTE MEMORY-DECK VERWALTUNG (Für Rückwärtskompatibilität) ==============

    /// <summary>
    /// Erstellt ein neues leeres Memory-Deck
    /// </summary>
    public string CreateNewDeck(string deckName)
    {
        string newDeckId = Guid.NewGuid().ToString();
        MemoryDeck newDeck = new MemoryDeck
        {
            deckId = newDeckId,
            deckName = deckName,
            groups = new List<DeckGroup>()
        };

        memoryDecks.Add(newDeck);
        SaveLibrary();
        Debug.Log($"Memory-Deck '{deckName}' mit ID {newDeckId} erstellt");
        return newDeckId;
    }

    /// <summary>
    /// Fügt eine Gruppe zu einem Deck hinzu
    /// </summary>
    public string AddGroupToDeck(string deckId, string groupName, int requiredForMatch = 2)
    {
        MemoryDeck deck = memoryDecks.Find(d => d.deckId == deckId);
        if (deck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return null;
        }

        string newGroupId = Guid.NewGuid().ToString();
        DeckGroup newGroup = new DeckGroup
        {
            groupId = newGroupId,
            groupName = groupName,
            requiredForMatch = Mathf.Max(2, requiredForMatch),
            imageIds = new List<string>()
        };

        deck.groups.Add(newGroup);
        SaveLibrary();
        Debug.Log($"Gruppe '{groupName}' (ID {newGroupId}) zu Deck {deckId} hinzugefügt");
        return newGroupId;
    }

    /// <summary>
    /// Fügt ein Bild aus dem Pool zu einer Gruppe hinzu
    /// </summary>
    public void AddImageToGroup(string deckId, string groupId, string imageId)
    {
        MemoryDeck deck = memoryDecks.Find(d => d.deckId == deckId);
        if (deck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return;
        }

        DeckGroup group = deck.groups.Find(g => g.groupId == groupId);
        if (group == null)
        {
            Debug.LogError($"Gruppe mit ID {groupId} nicht gefunden");
            return;
        }

        if (!imagePool.Exists(img => img.imageId == imageId))
        {
            Debug.LogError($"Bild mit ID {imageId} nicht im Pool vorhanden");
            return;
        }

        if (!group.imageIds.Contains(imageId))
        {
            group.imageIds.Add(imageId);
            SaveLibrary();
            Debug.Log($"Bild {imageId} zu Gruppe {groupId} hinzugefügt");
        }
    }

    /// <summary>
    /// Entfernt ein Bild aus einer Gruppe
    /// </summary>
    public void RemoveImageFromGroup(string deckId, string groupId, string imageId)
    {
        MemoryDeck deck = memoryDecks.Find(d => d.deckId == deckId);
        if (deck == null) return;

        DeckGroup group = deck.groups.Find(g => g.groupId == groupId);
        if (group != null && group.imageIds.Contains(imageId))
        {
            group.imageIds.Remove(imageId);
            SaveLibrary();
        }
    }

    /// <summary>
    /// Gibt ein Deck anhand seiner ID zurück
    /// </summary>
    public MemoryDeck GetDeck(string deckId)
    {
        return memoryDecks.Find(d => d.deckId == deckId);
    }

    /// <summary>
    /// Gibt eine Gruppe anhand ihrer ID zurück
    /// </summary>
    public DeckGroup GetGroup(string deckId, string groupId)
    {
        MemoryDeck deck = GetDeck(deckId);
        if (deck == null) return null;

        return deck.groups.Find(g => g.groupId == groupId);
    }

    /// <summary>
    /// Entfernt ein Deck und alle seine Gruppen (löscht aber keine Bilder aus dem Pool)
    /// </summary>
    public void RemoveDeck(string deckId)
    {
        MemoryDeck deck = GetDeck(deckId);
        if (deck != null)
        {
            memoryDecks.Remove(deck);
            SaveLibrary();
            Debug.Log($"Deck {deckId} entfernt");
        }
    }

    /// <summary>
    /// Erstellt ein Default-Deck basierend auf den internen Default-Sprites.
    /// Die Gruppen enthalten keine Pool-Image-IDs (werden später durch Fallback-Sprites verwendet).
    /// </summary>
    public string CreateDefaultDeck(int groupCount, int requiredForMatch = 2, string deckName = "Default Deck")
    {
        string deckId = Guid.NewGuid().ToString();
        MemoryDeck deck = new MemoryDeck
        {
            deckId = deckId,
            deckName = deckName,
            groups = new List<DeckGroup>()
        };

        for (int i = 0; i < groupCount; i++)
        {
            DeckGroup g = new DeckGroup
            {
                groupId = Guid.NewGuid().ToString(),
                groupName = $"Group_{i}",
                requiredForMatch = Mathf.Max(1, requiredForMatch),
                imageIds = new List<string>() // leave empty so fallback default sprites are used
            };

            deck.groups.Add(g);
        }

        memoryDecks.Add(deck);
        SaveLibrary();
        Debug.Log($"Default deck '{deckName}' erstellt mit {groupCount} Gruppen (requiredForMatch={requiredForMatch}), deckId={deckId}");
        return deckId;
    }

    // ============== PERSISTENTE SPEICHERUNG ==============

    void SaveLibrary()
    {
        try
        {
            // Speichere Bild-Pool
            PoolData poolData = new PoolData { images = imagePool };
            string poolJson = JsonUtility.ToJson(poolData);
            PlayerPrefs.SetString(PLAYERPREFS_POOL_KEY, poolJson);

            // Speichere Decks
            DecksData decksData = new DecksData { decks = memoryDecks };
            string decksJson = JsonUtility.ToJson(decksData);
            PlayerPrefs.SetString(PLAYERPREFS_DECKS_KEY, decksJson);

            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Speichern: {e.Message}");
        }
    }

    void LoadLibrary()
    {
        try
        {
            // Lade Bild-Pool
            if (PlayerPrefs.HasKey(PLAYERPREFS_POOL_KEY))
            {
                string poolJson = PlayerPrefs.GetString(PLAYERPREFS_POOL_KEY);
                PoolData poolData = JsonUtility.FromJson<PoolData>(poolJson);
                if (poolData != null)
                {
                    imagePool = poolData.images ?? new List<PoolImage>();
                }
            }

            // Wenn kein Bild im Pool ist, bleibe bei Default-Sprites (keine Persistenz für Default nötig)
            if (imagePool == null || imagePool.Count == 0)
            {
                Debug.Log("Kein Benutzerbild im Pool gefunden, Default-Sprites werden verwendet");
            }

            // Lade Decks
            if (PlayerPrefs.HasKey(PLAYERPREFS_DECKS_KEY))
            {
                string decksJson = PlayerPrefs.GetString(PLAYERPREFS_DECKS_KEY);
                DecksData decksData = JsonUtility.FromJson<DecksData>(decksJson);
                if (decksData != null)
                {
                    memoryDecks = decksData.decks ?? new List<MemoryDeck>();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Laden: {e.Message}");
            imagePool = new List<PoolImage>();
            memoryDecks = new List<MemoryDeck>();
        }
    }

    [System.Serializable]
    private class PoolData
    {
        public List<PoolImage> images;
    }

    [System.Serializable]
    private class DecksData
    {
        public List<MemoryDeck> decks;
    }
}
