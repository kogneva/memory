//using UnityEngine;

//public class CardInstance
////collects statistic data during runtime
//{
//    //fields
//    public CardDefinition card_definition;
//    private CardView card_view;

//    public int Guesses => CorrectGuesses+IncorrectGuesses;
//    public int CorrectGuesses { get; private set; } = 0;
//    public int IncorrectGuesses { get; private set; } = 0;

//    //methods
//    public bool is_revealed { get; set; }=false;

//    //TODO: functions to build statistics
//    // - pro paar, wie oft aufgedeckt bis  match (if selected && no_match -> count++)
//    // - ggf ob direkt hintereinander oder nicht

// public void CountGuesses(bool guessed)//all guesses until round completion, count maybe in gameController
//    {
//        if (guessed == false)
//        {
//            IncorrectGuesses += 1;
//        }
//        else
//        {
//            CorrectGuesses += 1;
//        }
//    }
    
//}
