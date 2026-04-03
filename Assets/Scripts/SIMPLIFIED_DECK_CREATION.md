# Vereinfachte Deck-Erstellung - Dokumentation

## Übersicht

Das neue System ermöglicht eine vereinfachte Deck-Erstellung mit folgenden Eigenschaften:
- **Alle Gruppen haben die gleiche Größe** (requiredForMatch)
- **Jedes Bild kommt nur einmal pro Deck vor** (außer bei "useSameImages")
- **Optionale Wiederverwendung der gleichen Bilder** in allen Gruppen

## Workflow

### Schritt 1: Bild-Pool vorbereiten

Bevor ein Deck erstellt werden kann, müssen Bilder in den Pool geladen werden:

```csharp
ImageManager imageManager = ImageManager.Instance;

// Einzelnes Bild hinzufügen
imageManager.AddImageToPool((imageId) => {
    Debug.Log($"Bild {imageId} zum Pool hinzugefügt");
});

// Mehrere Bilder hinzufügen
imageManager.AddImagesToPool((imageIds) => {
    Debug.Log($"{imageIds.Count} Bilder zum Pool hinzugefügt");
});
```

### Schritt 2: Deck-Konfiguration validieren

Vor der Erstellung sollte geprüft werden, ob genug Bilder vorhanden sind:

```csharp
var config = new ImageManager.SimplifiedDeckConfig(
    deckName: "Mein Tier-Deck",
    groupCount: 5,              // 5 Gruppen
    requiredForMatch: 2,        // Je 2 Karten (klassisches Memory)
    useSameImages: false        // Jede Gruppe unterschiedliche Bilder
);

ImageManager.DeckValidationResult validation = imageManager.ValidateSimplifiedDeck(config);

if (validation.isValid)
{
    Debug.Log("Deck kann erstellt werden!");
}
else
{
    Debug.LogError($"Fehler: {validation.errorMessage}");
    Debug.Log($"Benötigt: {validation.requiredImages} Bilder");
    Debug.Log($"Verfügbar: {validation.availableImages} Bilder");
}
```

### Schritt 3A: Automatische Deck-Erstellung

Bilder werden automatisch aus dem Pool zugewiesen:

```csharp
string deckId = imageManager.CreateSimplifiedDeck(config);

if (deckId != null)
{
    Debug.Log($"Deck erstellt mit ID: {deckId}");
}
```

### Schritt 3B: Manuelle Deck-Erstellung mit DeckBuilder

Für eine UI mit manueller Bild-Auswahl:

```csharp
DeckBuilder builder = GetComponent<DeckBuilder>();

// Schritt 1: Konfiguration starten
bool success = builder.StartDeckConfiguration(
    deckName: "Mein Tier-Deck",
    groupCount: 5,
    requiredForMatch: 2,
    useSameImages: false
);

if (!success)
{
    Debug.LogError("Konfiguration ungültig");
    return;
}

// Schritt 2: Bilder den Gruppen zuweisen
// Für jede Gruppe (0 bis groupCount-1):

// Zeige verfügbare Bilder
List<ImageManager.PoolImage> availableImages = builder.GetAvailableImages();

// Benutzer wählt Bilder aus und fügt sie hinzu
builder.AddImageToCurrentGroup("image-id-1");
builder.AddImageToCurrentGroup("image-id-2");

// Zur nächsten Gruppe wechseln
if (builder.IsCurrentGroupComplete())
{
    builder.NextGroup();
}

// Oder zurück zur vorherigen Gruppe
builder.PreviousGroup();

// Schritt 3: Deck finalisieren
string deckId = builder.FinalizeDeck();
if (deckId != null)
{
    Debug.Log($"Deck erstellt: {deckId}");
}
```

## Beispiele

### Beispiel 1: Klassisches Memory (5 Paare)

```csharp
// Benötigt: 10 Bilder im Pool (5 Gruppen × 2 Karten)
var config = new ImageManager.SimplifiedDeckConfig(
    "Klassisch", 
    groupCount: 5, 
    requiredForMatch: 2, 
    useSameImages: false
);

string deckId = ImageManager.Instance.CreateSimplifiedDeck(config);
```

### Beispiel 2: Triplets (4 Dreier-Gruppen)

```csharp
// Benötigt: 12 Bilder im Pool (4 Gruppen × 3 Karten)
var config = new ImageManager.SimplifiedDeckConfig(
    "Triplets", 
    groupCount: 4, 
    requiredForMatch: 3, 
    useSameImages: false
);

string deckId = ImageManager.Instance.CreateSimplifiedDeck(config);
```

### Beispiel 3: Alle Gruppen mit gleichen Bildern

```csharp
// Benötigt: Nur 2 Bilder im Pool (alle 5 Gruppen verwenden die gleichen 2 Bilder)
var config = new ImageManager.SimplifiedDeckConfig(
    "Gleiche Bilder", 
    groupCount: 5, 
    requiredForMatch: 2, 
    useSameImages: true
);

string deckId = ImageManager.Instance.CreateSimplifiedDeck(config);
```

### Beispiel 4: Mit manueller Bild-Zuweisung

```csharp
var config = new ImageManager.SimplifiedDeckConfig(
    "Manuelle Auswahl", 
    groupCount: 3, 
    requiredForMatch: 2, 
    useSameImages: false
);

// Manuelle Zuweisungen definieren
Dictionary<int, List<string>> assignments = new Dictionary<int, List<string>>
{
    { 0, new List<string> { "img-id-1", "img-id-2" } },  // Gruppe 0
    { 1, new List<string> { "img-id-3", "img-id-4" } },  // Gruppe 1
    { 2, new List<string> { "img-id-5", "img-id-6" } }   // Gruppe 2
};

string deckId = ImageManager.Instance.CreateSimplifiedDeck(config, assignments);
```

### Beispiel 5: Auto-Fill mit gleichen Bildern

```csharp
DeckBuilder builder = GetComponent<DeckBuilder>();

builder.StartDeckConfiguration("Auto-Fill Test", 10, 2, useSameImages: true);

// Benutzer füllt nur die erste Gruppe
builder.AddImageToCurrentGroup("img-1");
builder.AddImageToCurrentGroup("img-2");

// Alle anderen Gruppen werden automatisch gefüllt
builder.AutoFillWithSameImages();

// Deck erstellen
string deckId = builder.FinalizeDeck();
```

## Validierungs-Regeln

### Regel 1: Mindestanforderungen
- **groupCount**: Mindestens 1
- **requiredForMatch**: Mindestens 2

### Regel 2: Genug Bilder im Pool

**Bei useSameImages = false:**
```
Benötigte Bilder = groupCount × requiredForMatch
```

Beispiel: 5 Gruppen × 2 Karten = 10 Bilder erforderlich

**Bei useSameImages = true:**
```
Benötigte Bilder = requiredForMatch
```

Beispiel: 2 Karten (unabhängig von groupCount)

### Regel 3: Keine Duplikate innerhalb eines Decks

Bei `useSameImages = false`:
- Jedes Bild darf nur in **einer** Gruppe verwendet werden
- Die Methode `AddImageToGroupSafe()` validiert dies automatisch

Bei `useSameImages = true`:
- Alle Gruppen teilen sich die gleichen Bilder
- Duplikate sind erlaubt und erwünscht

## API-Referenz

### ImageManager

#### `SimplifiedDeckConfig`
```csharp
public class SimplifiedDeckConfig
{
    public string deckName;          // Name des Decks
    public int groupCount;           // Anzahl Gruppen
    public int requiredForMatch;     // Karten pro Gruppe
    public bool useSameImages;       // Gleiche Bilder in allen Gruppen
}
```

#### `DeckValidationResult`
```csharp
public class DeckValidationResult
{
    public bool isValid;             // Ist die Konfiguration gültig?
    public string errorMessage;      // Fehlermeldung (falls ungültig)
    public int requiredImages;       // Benötigte Anzahl Bilder
    public int availableImages;      // Verfügbare Anzahl Bilder
}
```

#### Methoden

```csharp
// Validiere Deck-Konfiguration
DeckValidationResult ValidateSimplifiedDeck(SimplifiedDeckConfig config)

// Erstelle vereinfachtes Deck
string CreateSimplifiedDeck(
    SimplifiedDeckConfig config, 
    Dictionary<int, List<string>> imageAssignments = null
)

// Füge Bild zu Gruppe hinzu (mit Validierung)
bool AddImageToGroupSafe(string deckId, string groupId, string imageId)

// Hole verwendete Bilder in einem Deck
HashSet<string> GetUsedImagesInDeck(string deckId)

// Hole verfügbare Bilder für ein Deck
List<PoolImage> GetAvailableImagesForDeck(string deckId)
```

### DeckBuilder

#### Methoden

```csharp
// Starte Deck-Konfiguration
bool StartDeckConfiguration(
    string deckName, 
    int groupCount, 
    int requiredForMatch, 
    bool useSameImages
)

// Füge Bild zur aktuellen Gruppe hinzu
bool AddImageToCurrentGroup(string imageId)

// Entferne Bild aus aktueller Gruppe
bool RemoveImageFromCurrentGroup(string imageId)

// Hole Bilder der aktuellen Gruppe
List<string> GetCurrentGroupImages()

// Prüfe ob aktuelle Gruppe vollständig
bool IsCurrentGroupComplete()

// Nächste/Vorherige Gruppe
bool NextGroup()
bool PreviousGroup()

// Hole verfügbare Bilder
List<ImageManager.PoolImage> GetAvailableImages()

// Auto-Fill (nur bei useSameImages=true)
void AutoFillWithSameImages()

// Deck erstellen
string FinalizeDeck()

// Abbrechen
void CancelDeckCreation()

// Hilfs-Methoden
BuilderState GetCurrentState()
int GetCurrentGroupIndex()
SimplifiedDeckConfig GetCurrentConfig()
float GetProgress()
string GetProgressDescription()
```

## UI-Integration Beispiel

```csharp
public class DeckCreationUI : MonoBehaviour
{
    private DeckBuilder builder;
    
    // UI Elemente
    public InputField deckNameInput;
    public InputField groupCountInput;
    public InputField matchSizeInput;
    public Toggle useSameImagesToggle;
    
    public GameObject configPanel;
    public GameObject assignmentPanel;
    public Text progressText;
    
    public Transform imagePoolContainer;
    public Transform currentGroupContainer;
    
    void Start()
    {
        builder = gameObject.AddComponent<DeckBuilder>();
    }
    
    public void OnStartButtonClick()
    {
        string deckName = deckNameInput.text;
        int groupCount = int.Parse(groupCountInput.text);
        int matchSize = int.Parse(matchSizeInput.text);
        bool useSameImages = useSameImagesToggle.isOn;
        
        if (builder.StartDeckConfiguration(deckName, groupCount, matchSize, useSameImages))
        {
            configPanel.SetActive(false);
            assignmentPanel.SetActive(true);
            UpdateUI();
        }
    }
    
    public void OnImageClick(string imageId)
    {
        if (builder.AddImageToCurrentGroup(imageId))
        {
            UpdateUI();
        }
    }
    
    public void OnNextGroupClick()
    {
        if (builder.NextGroup())
        {
            UpdateUI();
        }
    }
    
    public void OnFinishClick()
    {
        string deckId = builder.FinalizeDeck();
        if (deckId != null)
        {
            Debug.Log("Deck erstellt!");
            // Zurück zum Hauptmenü o.ä.
        }
    }
    
    void UpdateUI()
    {
        progressText.text = builder.GetProgressDescription();
        
        // Zeige verfügbare Bilder
        DisplayAvailableImages(builder.GetAvailableImages());
        
        // Zeige aktuelle Gruppen-Bilder
        DisplayCurrentGroupImages(builder.GetCurrentGroupImages());
    }
    
    void DisplayAvailableImages(List<ImageManager.PoolImage> images) { /* ... */ }
    void DisplayCurrentGroupImages(List<string> imageIds) { /* ... */ }
}
```

## Fehlerbehandlung

```csharp
// Vor der Deck-Erstellung immer validieren
var validation = ImageManager.Instance.ValidateSimplifiedDeck(config);

if (!validation.isValid)
{
    if (validation.requiredImages > validation.availableImages)
    {
        int missing = validation.requiredImages - validation.availableImages;
        ShowError($"Bitte füge {missing} weitere Bilder zum Pool hinzu");
    }
    else
    {
        ShowError(validation.errorMessage);
    }
    return;
}

// Deck erstellen
string deckId = ImageManager.Instance.CreateSimplifiedDeck(config);
if (deckId == null)
{
    ShowError("Deck konnte nicht erstellt werden");
}
```

## Zusammenfassung

Die vereinfachte Deck-Erstellung bietet:

? **Einfache Konfiguration**: Nur 3-4 Parameter
? **Validierung**: Automatische Prüfung auf genug Bilder
? **Duplikat-Vermeidung**: Bilder werden nur einmal pro Deck verwendet
? **Flexible Wiederverwendung**: Option für gleiche Bilder in allen Gruppen
? **UI-freundlich**: DeckBuilder für schrittweisen Workflow
? **Fehlerbehandlung**: Aussagekräftige Fehlermeldungen
