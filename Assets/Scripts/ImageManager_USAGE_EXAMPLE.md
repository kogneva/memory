# ImageManager - Verwendungsbeispiele

## Überblick

Das neue System ist in zwei Teile unterteilt:

1. **Bild-Pool**: Zentrale Verwaltung aller hochgeladenen Bilder
2. **Memory-Decks**: Verschiedene Spielkonfigurationen mit Gruppen und Bildauswahlen

## Workflow

### Schritt 1: Bilder in den Pool laden

```csharp
ImageManager imageManager = ImageManager.Instance;

// Öffne die Galerie und lade Bilder
// Dies wird mehrmals aufgerufen, um alle Bilder hochzuladen
imageManager.AddImageToPool((addedImageId) => {
    Debug.Log($"Bild {addedImageId} wurde zum Pool hinzugefügt");
});
```

**Ergebnis:** Bilder mit IDs 0, 1, 2, 3, 4, ... werden im Pool gespeichert


### Schritt 2: Memory-Decks erstellen

```csharp
// Erstelle ein neues Deck
int deckId1 = imageManager.CreateNewDeck("Tiere Deck");

// Oder mehrere verschiedene Decks
int deckId2 = imageManager.CreateNewDeck("Früchte Deck");
```

### Schritt 3: Gruppen zu einem Deck hinzufügen

```csharp
// Füge eine Gruppe zu Deck 1 hinzu (z.B. 3 Karten pro Gruppe)
int groupId1 = imageManager.AddGroupToDeck(deckId1, "Hunde", requiredForMatch: 3);

// Weitere Gruppen im gleichen Deck
int groupId2 = imageManager.AddGroupToDeck(deckId1, "Katzen", requiredForMatch: 2);
int groupId3 = imageManager.AddGroupToDeck(deckId1, "Vögel", requiredForMatch: 4);
```

### Schritt 4: Bilder aus dem Pool den Gruppen zuordnen

```csharp
// Gruppe "Hunde" bekommen Bilder 0 und 1 (können unterschiedliche Hundebilder sein)
imageManager.AddImageToGroup(deckId1, groupId1, 0);  // Bild 1
imageManager.AddImageToGroup(deckId1, groupId1, 1);  // Bild 2

// Gruppe "Katzen" bekommen Bilder 2 und 3
imageManager.AddImageToGroup(deckId1, groupId2, 2);
imageManager.AddImageToGroup(deckId1, groupId2, 3);

// Gruppe "Vögel" bekommen Bilder 4, 5, 6, 7 (mehr Bilder für 4er Gruppe)
imageManager.AddImageToGroup(deckId1, groupId3, 4);
imageManager.AddImageToGroup(deckId1, groupId3, 5);
imageManager.AddImageToGroup(deckId1, groupId3, 6);
imageManager.AddImageToGroup(deckId1, groupId3, 7);
```

### Schritt 5: Das Spiel starten

```csharp
// Starte das Spiel mit Deck 1
GameController gameController = GameController.Instance;
gameController.InitializeGame(deckId1);
```

## Mehrere Decks mit gleichen Bildern

Das ist jetzt sehr einfach möglich! Bilder können in mehreren Decks wiederverwendet werden:

```csharp
// Deck 1: "Tiere"
int deckId1 = imageManager.CreateNewDeck("Tiere");
int groupId1 = imageManager.AddGroupToDeck(deckId1, "Hunde", 2);
imageManager.AddImageToGroup(deckId1, groupId1, 0);  // Hundbild
imageManager.AddImageToGroup(deckId1, groupId1, 1);  // Hundbild

// Deck 2: "Schwierig" - Nutzt die GLEICHEN Bilder (0, 1) aber in einer anderen Gruppe
int deckId2 = imageManager.CreateNewDeck("Schwierig");
int groupId2 = imageManager.AddGroupToDeck(deckId2, "Seltene Tiere", 3);
imageManager.AddImageToGroup(deckId2, groupId2, 0);  // Bild 0 wieder verwendet
imageManager.AddImageToGroup(deckId2, groupId2, 1);  // Bild 1 wieder verwendet
imageManager.AddImageToGroup(deckId2, groupId2, 8);  // Neue Bild
```

## Löschen

```csharp
// Entferne ein Bild aus dem Pool (wird aus allen Decks entfernt)
imageManager.RemoveImageFromPool(0);

// Entferne ein komplettes Deck (Bilder bleiben im Pool)
imageManager.RemoveDeck(deckId1);

// Entferne ein Bild nur aus einer Gruppe (bleibt im Pool)
imageManager.RemoveImageFromGroup(deckId1, groupId1, 0);
```

## Vorteile dieser Struktur

✅ **Einmalige Speicherung**: Bilder werden nur einmal auf dem Gerät gespeichert
✅ **Wiederverwendung**: Bilder können in mehreren Decks verwendet werden
✅ **Flexibilität**: Decks können beliebig konfiguriert werden
✅ **Speichereffizienz**: Keine Duplikate von Bildern
✅ **Persistenz**: Alles wird automatisch gespeichert und wiederhergestellt
