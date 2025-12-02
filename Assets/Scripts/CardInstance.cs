using UnityEngine;

public class CardInstance
//collects statistic data during runtime
{
    //fields
    private CardDefinition card_definition;
    private CardView card_view;

    private int countGuesses;
    private int countCorrectGuesses;
    private int countIncorrectGuesses;

    //methods
    public bool is_revealed { get; set; }=false;

    //TODO: functions to build statistics
    // - pro paar, wie oft aufgedeckt bis  match (if selected && no_match -> count++)
    // - ggf ob direkt hintereinander oder nicht

    
}
