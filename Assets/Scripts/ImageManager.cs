using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;

public class ImageManager : MonoBehaviour
{
    public static ImageManager Instance;

    [System.Serializable]
    public class PoolImage
    {
        public string imageId;
        public string imagePath;
        public string imageName;
        [NonSerialized] public Sprite sprite;
    }

    [System.Serializable]
    public class DeckGroup
    {
        public string groupId;
        public string groupName;
        public int groupSize = 2;        // NEU: Anzahl Karten in der Gruppe
        public int requiredForMatch = 2; // Wie viele davon für Match nötig
        public List<string> imageIds = new List<string>();
    }

    [System.Serializable]
    public class MemoryDeck
    {
        public string deckId;
        public string deckName;
        public List<DeckGroup> groups = new List<DeckGroup>();
    }

    [System.Serializable]
    public class SimplifiedDeckConfig
    {
        public string deckName;
        public int groupCount; 
        public int groupSize;            
        public int requiredForMatch;
        public bool useSameImages;
        
        public SimplifiedDeckConfig(string deckName, int groupCount, int groupSize, int requiredForMatch, bool useSameImages = false)
        {
            this.deckName = deckName;
            this.groupCount = groupCount;
            this.groupSize = groupSize;
            this.requiredForMatch = requiredForMatch;
            this.useSameImages = useSameImages;
        }
    }

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

    public List<PoolImage> imagePool = new List<PoolImage>();
    public List<MemoryDeck> memoryDecks = new List<MemoryDeck>();

    private const string PLAYERPREFS_POOL_KEY = "IMAGE_POOL";
    private const string PLAYERPREFS_DECKS_KEY = "MEMORY_DECKS";

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

    public void AddImageToPool(Action<string> onImageAdded = null)
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (string.IsNullOrEmpty(path))
                return;

            AddImageFileToPool(path, onImageAdded);
        });
    }

    public void AddImagesToPool(Action<List<string>> onImagesAdded = null)
    {
        NativeGallery.GetImagesFromGallery((paths) =>
        {
            if (paths == null || paths.Length == 0)
                return;

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

            string newId = Guid.NewGuid().ToString();

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

    public void RemoveImageFromPool(string imageId)
    {
        PoolImage poolImage = imagePool.Find(img => img.imageId == imageId);
        if (poolImage != null)
        {
            foreach (MemoryDeck deck in memoryDecks)
            {
                foreach (DeckGroup group in deck.groups)
                {
                    group.imageIds.Remove(imageId);
                }
            }

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

    public PoolImage GetPoolImage(string imageId)
    {
        return imagePool.Find(img => img.imageId == imageId);
    }

    public Sprite LoadPoolImageSprite(string imageId)
    {
        PoolImage poolImage = GetPoolImage(imageId);
        if (poolImage == null)
        {
            Debug.LogWarning($"Bild mit ID {imageId} nicht gefunden, verwende Default-Sprite");
            return GetDefaultSpriteByGuid(imageId);
        }

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
            }
            else
            {
                Debug.LogWarning($"Keine Default-Sprites in Resources/{DEFAULT_RESOURCE_FOLDER} gefunden");
                defaultSprites = new List<Sprite>();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Fehler beim Laden der Default-Sprites: {e.Message}");
            defaultSprites = new List<Sprite>();
        }
    }

    // ============== DECK-ERSTELLUNG ==============

    public DeckValidationResult ValidateSimplifiedDeck(SimplifiedDeckConfig config)
    {
        if (config.groupCount < 2)
            return new DeckValidationResult(false, "Mindestens 2 Gruppen erforderlich");

        if (config.groupSize < 2)
            return new DeckValidationResult(false, "Mindestens 2 Karten pro Gruppe erforderlich");

        if (config.requiredForMatch < 2 || config.requiredForMatch > config.groupSize)
            return new DeckValidationResult(false, 
                $"requiredForMatch ({config.requiredForMatch}) muss zwischen 2 und groupSize ({config.groupSize}) liegen");

        int requiredImages = config.useSameImages ? config.groupCount : config.groupCount * config.groupSize;
        int availableImages = imagePool?.Count ?? 0;

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

    public string CreateSimplifiedDeck(SimplifiedDeckConfig config, Dictionary<int, List<string>> imageAssignments = null)
    {
        DeckValidationResult validation = ValidateSimplifiedDeck(config);
        if (!validation.isValid)
        {
            Debug.LogError($"Deck-Validierung fehlgeschlagen: {validation.errorMessage}");
            return null;
        }

        string deckId = Guid.NewGuid().ToString();
        MemoryDeck deck = new MemoryDeck
        {
            deckId = deckId,
            deckName = config.deckName,
            groups = new List<DeckGroup>()
        };

        if (imageAssignments != null && !ValidateImageAssignments(config, imageAssignments))
        {
            Debug.LogError("Ungültige Bild-Zuweisungen");
            return null;
        }

        for (int i = 0; i < config.groupCount; i++)
        {
            DeckGroup group = new DeckGroup
            {
                groupId = Guid.NewGuid().ToString(),
                groupName = $"Gruppe {i + 1}",
                requiredForMatch = config.requiredForMatch,
                imageIds = imageAssignments != null && imageAssignments.ContainsKey(i) 
                    ? new List<string>(imageAssignments[i]) 
                    : new List<string>()
            };

            deck.groups.Add(group);
        }

        if (imageAssignments == null)
        {
            AutoAssignImagesToGroups(deck, config);
        }

        memoryDecks.Add(deck);
        SaveLibrary();
        
        Debug.Log($"Deck '{config.deckName}' erstellt mit {config.groupCount} Gruppen (je {config.requiredForMatch} Karten)");
        return deckId;
    }

    /// <summary>
    /// Erstellt ein Default-Deck mit leeren imageIds (nutzt Fallback-Sprites)
    /// </summary>
    public string CreateDefaultDeck(int groupCount, int groupSize, int requiredForMatch = 2, string deckName = "Default Deck")
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
            deck.groups.Add(new DeckGroup
            {
                groupId = Guid.NewGuid().ToString(),
                groupName = $"Group_{i}",
                groupSize = Mathf.Max(1, groupSize),              // NEU
                requiredForMatch = Mathf.Max(1, requiredForMatch),
                imageIds = new List<string>()
            });
        }

        memoryDecks.Add(deck);
        SaveLibrary();
        Debug.Log($"Default deck '{deckName}' erstellt: {groupCount} Gruppen × {groupSize} Karten (requiredForMatch={requiredForMatch})");
        return deckId;
    }

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

            if (groupImages.Count != config.groupSize)
            {
                Debug.LogError($"Gruppe {i} hat {groupImages.Count} Bilder, benötigt aber {config.groupSize}");
                return false;
            }

            foreach (string imageId in groupImages)
            {
                if (!imagePool.Exists(img => img.imageId == imageId))
                {
                    Debug.LogError($"Bild {imageId} existiert nicht im Pool");
                    return false;
                }

                if (!config.useSameImages)
                {
                    if (usedImages.Contains(imageId))
                    {
                        Debug.LogError($"Bild {imageId} wird in mehreren Gruppen verwendet");
                        return false;
                    }
                    usedImages.Add(imageId);
                }
            }
        }

        return true;
    }

    private void AutoAssignImagesToGroups(MemoryDeck deck, SimplifiedDeckConfig config)
    {
        if (imagePool == null || imagePool.Count == 0)
        {
            Debug.LogWarning("Kein Bild im Pool - Default-Sprites werden verwendet");
            return;
        }

        List<string> availableImageIds = imagePool.Select(img => img.imageId).ToList();
        int imageIndex = 0;

        if (config.useSameImages)
        {
            foreach (DeckGroup group in deck.groups)
            {
                if (imageIndex < availableImageIds.Count)
                {
                    string imageId = availableImageIds[imageIndex];
                    for (int i = 0; i < config.groupSize; i++)
                    {
                        group.imageIds.Add(imageId);
                    }
                    imageIndex++;
                }
            }
        }
        else
        {
            foreach (DeckGroup group in deck.groups)
            {
                for (int i = 0; i < config.groupSize && imageIndex < availableImageIds.Count; i++)
                {
                    group.imageIds.Add(availableImageIds[imageIndex]);
                    imageIndex++;
                }
            }
        }
    }

    // ============== DECK VERWALTUNG ==============

    public MemoryDeck GetDeck(string deckId)
    {
        return memoryDecks.Find(d => d.deckId == deckId);
    }

    public DeckGroup GetGroup(string deckId, string groupId)
    {
        MemoryDeck deck = GetDeck(deckId);
        return deck?.groups.Find(g => g.groupId == groupId);
    }

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

    public List<PoolImage> GetAvailableImagesForDeck(string deckId)
    {
        HashSet<string> usedImages = GetUsedImagesInDeck(deckId);
        return imagePool.Where(img => !usedImages.Contains(img.imageId)).ToList();
    }

    // ============== PERSISTENTE SPEICHERUNG ==============

    void SaveLibrary()
    {
        try
        {
            PoolData poolData = new PoolData { images = imagePool };
            string poolJson = JsonUtility.ToJson(poolData);
            PlayerPrefs.SetString(PLAYERPREFS_POOL_KEY, poolJson);

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
            if (PlayerPrefs.HasKey(PLAYERPREFS_POOL_KEY))
            {
                string poolJson = PlayerPrefs.GetString(PLAYERPREFS_POOL_KEY);
                PoolData poolData = JsonUtility.FromJson<PoolData>(poolJson);
                if (poolData != null)
                {
                    imagePool = poolData.images ?? new List<PoolImage>();
                }
            }

            if (imagePool == null || imagePool.Count == 0)
            {
                Debug.Log("Kein Benutzerbild im Pool gefunden, Default-Sprites werden verwendet");
            }

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
