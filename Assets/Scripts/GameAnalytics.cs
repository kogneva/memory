using UnityEngine;

public class GameAnalytics : MonoBehaviour
{
    
    // general guesses
    public int Guesses => CorrectGuesses + IncorrectGuesses;
    public int CorrectGuesses  = 0;
    public int IncorrectGuesses = 0;

    // card specific guesses

    // game info:
    // deck id
    // gameFinished bool, default false
    // time taken to finish game
}
