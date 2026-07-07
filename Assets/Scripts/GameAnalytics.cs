using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CardAnalytics
{
    public string imageId;
    public string groupId;
    [FormerlySerializedAs("incorrectGuesses")] public int incorrectKlick;
}

// Neue Klassen für die Gruppierung in der JSON-Ausgabe
[Serializable]
public class GroupedCardAnalytics
{
    public string GroupId;
    public List<CardKlicks> Cards;
}

[Serializable]
public class CardKlicks
{
    public string imageId;
    public int incorrectKlick;
}

// this is how the JSON will look like
[Serializable]
public class RoundAnalyticsData
{
    //deck info
    public string deckId;
    public string deckName;
    public int groupCount;
    public int groupSize;
    public int requiredForMatch;
    public bool useSameImages;
    //round info
    public string timestamp;
    public float timeTakenSeconds;
    public bool gameFinished;
    [FormerlySerializedAs("incorrectGuessesByGroup")] public int incorrectMatchTries;
    public List<GroupedCardAnalytics> incorrectKlicksByCard;
}

public class GameAnalytics : MonoBehaviour
{
    // game info
    public bool GameFinished = false;
    [HideInInspector]
    public string currentDeckId;

    // general guesses
    [FormerlySerializedAs("TotalGuesses")] public int TotalPossibleCorrectGuesses = 0;
    public int IncorrectMatchGuesses = 0;
    public RoundAnalyticsData roundData;

    // card specific guesses
    public List<CardAnalytics> cardStats = new List<CardAnalytics>();

    public void StartAnalytics(ImageManager.MemoryDeck currentDeck)
    {
        currentDeckId = currentDeck.deckId;
        TotalPossibleCorrectGuesses = 0;
        GameFinished = false;
        cardStats.Clear();
        roundData.deckName = currentDeck.deckName;
        roundData.groupCount = currentDeck.groups.Count;
        roundData.groupSize = currentDeck.groupSize;
        roundData.requiredForMatch = currentDeck.requiredForMatch;
        roundData.useSameImages = currentDeck.useSameImages;
    }

    public void RecordIncorrectGuessForCard(string imageId, string groupId)
    {
        if (string.IsNullOrEmpty(imageId)) return;

        var stat = cardStats.Find(c => c.imageId == imageId);
        if (stat == null)
        {
            stat = new CardAnalytics { imageId = imageId, groupId = groupId, incorrectKlick = 0 };
            cardStats.Add(stat);
        }
        stat.incorrectKlick++;
    }

    public void SaveToJson(float finalTime)
    {
        RoundAnalyticsData data = new RoundAnalyticsData
        {
            ////deck info
            deckId = currentDeckId,
            deckName = roundData.deckName,
            groupCount = roundData.groupCount,
            groupSize = roundData.groupSize,
            requiredForMatch = roundData.requiredForMatch,
            useSameImages = roundData.useSameImages,

            ////round info
            timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            timeTakenSeconds = finalTime,
            gameFinished = GameFinished,
            incorrectMatchTries = IncorrectMatchGuesses,

            // Anpassung: Erstellung echter serialisierbaren Klassen anstatt anonymer Objekte, mit .ToList()
            incorrectKlicksByCard = this.cardStats.GroupBy(p => p.groupId)
                .Select(g => new GroupedCardAnalytics
                {
                    GroupId = g.Key,
                    Cards = g.Select(p => new CardKlicks
                    {
                        imageId = p.imageId,
                        incorrectKlick = p.incorrectKlick
                    }).ToList()
                }).ToList()
        };

        string json = JsonUtility.ToJson(data, true);
        string filename = $"Analytics_{currentDeckId}_{data.timestamp}.json";

        // Speichert standardmäßig in C:\Users\<User>\AppData\LocalLow\<Company>\<AppName>
        string path = Path.Combine(Application.persistentDataPath, filename);

        File.WriteAllText(path, json);
        Debug.Log($"Analytics als JSON gespeichert unter: {path}");
    }
}