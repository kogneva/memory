using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class ImageManager : MonoBehaviour
{
    public static ImageManager Instance;

    /// <summary>
    /// Ein einzelnes Bild im Pool
    /// </summary>
    [System.Serializable]
    public class PoolImage
    {
        public int imageId;
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
        public int groupId;
        public string groupName;
        public int requiredForMatch = 2;
        public List<int> imageIds = new List<int>(); // IDs der Bilder aus dem Pool
    }

    /// <summary>
    /// Ein komplettes Memory-Deck mit mehreren Gruppen
    /// </summary>
    [System.Serializable]
    public class MemoryDeck
    {
        public int deckId;
        public string deckName;
        public List<DeckGroup> groups = new List<DeckGroup>();
    }

    // Zentrale Bild-Verwaltung
    public List<PoolImage> imagePool = new List<PoolImage>();
    public List<MemoryDeck> memoryDecks = new List<MemoryDeck>();

    private const string PLAYERPREFS_POOL_KEY = "IMAGE_POOL";
    private const string PLAYERPREFS_DECKS_KEY = "MEMORY_DECKS";
    private int nextImageId = 0;
    private int nextDeckId = 0;
    private int nextGroupId = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLibrary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ============== BILD-POOL VERWALTUNG ==============

    /// <summary>
    /// Lädt ein Bild aus der Galerie in den Pool (Einzel-Auswahl)
    /// </summary>
    public void AddImageToPool(Action<int> onImageAdded = null)
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
    public void AddImagesToPool(Action<List<int>> onImagesAdded = null)
    {
        NativeGallery.GetImagesFromGallery((paths) =>
        {
            if (paths == null || paths.Length == 0)
            {
                return;
            }

            List<int> added = new List<int>();
            foreach (string path in paths)
            {
                int addedId = AddImageFileToPool(path, null);
                if (addedId >= 0) added.Add(addedId);
            }

            if (added.Count > 0)
            {
                SaveLibrary();
                onImagesAdded?.Invoke(added);
            }
        });
    }

    // gemeinsame Logik zum Kopieren und Hinzufügen
    private int AddImageFileToPool(string sourcePath, Action<int> onImageAdded)
    {
        try
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                return -1;

            string fileName = Path.GetFileName(sourcePath);
            string targetFileName = $"pool_image_{nextImageId}_{fileName}";
            string targetPath = Path.Combine(Application.persistentDataPath, targetFileName);

            File.Copy(sourcePath, targetPath, true);

            PoolImage poolImage = new PoolImage
            {
                imageId = nextImageId,
                imagePath = targetPath,
                imageName = fileName
            };

            imagePool.Add(poolImage);
            int addedImageId = nextImageId;
            nextImageId++;

            SaveLibrary();
            onImageAdded?.Invoke(addedImageId);
            Debug.Log($"Bild {fileName} mit ID {addedImageId} zum Pool hinzugefügt");
            return addedImageId;
        }
        catch (Exception e)
        {
            Debug.LogError($"Fehler beim Hinzufügen des Bildes: {e.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Entfernt ein Bild aus dem Pool
    /// </summary>
    public void RemoveImageFromPool(int imageId)
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
    public PoolImage GetPoolImage(int imageId)
    {
        return imagePool.Find(img => img.imageId == imageId);
    }

    /// <summary>
    /// Lädt das Sprite für ein Bild aus dem Pool
    /// </summary>
    public Sprite LoadPoolImageSprite(int imageId)
    {
        PoolImage poolImage = GetPoolImage(imageId);
        if (poolImage == null)
        {
            Debug.LogWarning($"Bild mit ID {imageId} nicht gefunden");
            return null;
        }

        // Verwende Cache wenn verfügbar
        if (poolImage.sprite != null)
            return poolImage.sprite;

        Sprite s = LoadSpriteFromPath(poolImage.imagePath);
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

    // ============== MEMORY-DECK VERWALTUNG ==============

    /// <summary>
    /// Erstellt ein neues leeres Memory-Deck
    /// </summary>
    public int CreateNewDeck(string deckName)
    {
        int newDeckId = nextDeckId++;
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
    public int AddGroupToDeck(int deckId, string groupName, int requiredForMatch = 2)
    {
        MemoryDeck deck = memoryDecks.Find(d => d.deckId == deckId);
        if (deck == null)
        {
            Debug.LogError($"Deck mit ID {deckId} nicht gefunden");
            return -1;
        }

        int newGroupId = nextGroupId++;
        DeckGroup newGroup = new DeckGroup
        {
            groupId = newGroupId,
            groupName = groupName,
            requiredForMatch = Mathf.Max(2, requiredForMatch),
            imageIds = new List<int>()
        };

        deck.groups.Add(newGroup);
        SaveLibrary();
        Debug.Log($"Gruppe '{groupName}' (ID {newGroupId}) zu Deck {deckId} hinzugefügt");
        return newGroupId;
    }

    /// <summary>
    /// Fügt ein Bild aus dem Pool zu einer Gruppe hinzu
    /// </summary>
    public void AddImageToGroup(int deckId, int groupId, int imageId)
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
    public void RemoveImageFromGroup(int deckId, int groupId, int imageId)
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
    public MemoryDeck GetDeck(int deckId)
    {
        return memoryDecks.Find(d => d.deckId == deckId);
    }

    /// <summary>
    /// Gibt eine Gruppe anhand ihrer ID zurück
    /// </summary>
    public DeckGroup GetGroup(int deckId, int groupId)
    {
        MemoryDeck deck = GetDeck(deckId);
        if (deck == null) return null;

        return deck.groups.Find(g => g.groupId == groupId);
    }

    /// <summary>
    /// Entfernt ein Deck und alle seine Gruppen (löscht aber keine Bilder aus dem Pool)
    /// </summary>
    public void RemoveDeck(int deckId)
    {
        MemoryDeck deck = GetDeck(deckId);
        if (deck != null)
        {
            memoryDecks.Remove(deck);
            SaveLibrary();
            Debug.Log($"Deck {deckId} entfernt");
        }
    }

    // ============== PERSISTENTE SPEICHERUNG ==============

    void SaveLibrary()
    {
        try
        {
            // Speichere Bild-Pool
            PoolData poolData = new PoolData { images = imagePool, nextImageId = nextImageId };
            string poolJson = JsonUtility.ToJson(poolData);
            PlayerPrefs.SetString(PLAYERPREFS_POOL_KEY, poolJson);

            // Speichere Decks
            DecksData decksData = new DecksData { decks = memoryDecks, nextDeckId = nextDeckId, nextGroupId = nextGroupId };
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
                    nextImageId = poolData.nextImageId;
                }
            }

            // Lade Decks
            if (PlayerPrefs.HasKey(PLAYERPREFS_DECKS_KEY))
            {
                string decksJson = PlayerPrefs.GetString(PLAYERPREFS_DECKS_KEY);
                DecksData decksData = JsonUtility.FromJson<DecksData>(decksJson);
                if (decksData != null)
                {
                    memoryDecks = decksData.decks ?? new List<MemoryDeck>();
                    nextDeckId = decksData.nextDeckId;
                    nextGroupId = decksData.nextGroupId;
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
        public int nextImageId;
    }

    [System.Serializable]
    private class DecksData
    {
        public List<MemoryDeck> decks;
        public int nextDeckId;
        public int nextGroupId;
    }
}
