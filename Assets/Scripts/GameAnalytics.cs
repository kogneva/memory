using UnityEngine;

public class GameAnalytics : MonoBehaviour
{
    
    public int Guesses => CorrectGuesses + IncorrectGuesses;
    public int CorrectGuesses { get; private set; } = 0;
    public int IncorrectGuesses { get; private set; } = 0;
}
