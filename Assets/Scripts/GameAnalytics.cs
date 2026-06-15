using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class CardAnalytics
{
    public string imageId;
    [FormerlySerializedAs("incorrectGuesses")] public int incorrectKlick;
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
    public int incorrectGuessesByGroup;
    public List<CardAnalytics> incorrectKlicksByCard;
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

    public void RecordIncorrectGuessForCard(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return;

        var stat = cardStats.Find(c => c.imageId == imageId);
        if (stat == null)
        {
            stat = new CardAnalytics { imageId = imageId, incorrectKlick = 0 };
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
            incorrectGuessesByGroup = IncorrectMatchGuesses,
            incorrectKlicksByCard = this.cardStats
        };

        string json = JsonUtility.ToJson(data, true);
        string filename = $"Analytics_{currentDeckId}_{data.timestamp}.json";

        // Speichert standardm‰ﬂig in C:\Users\<User>\AppData\LocalLow\<Company>\<AppName>
        string path = Path.Combine(Application.persistentDataPath, filename);

        File.WriteAllText(path, json);
        Debug.Log($"Analytics als JSON gespeichert unter: {path}");
    }
}