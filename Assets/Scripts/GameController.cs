using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{

    [SerializeField]
    private Sprite backImage;

    [SerializeField]
    public int pair_of = 2;

    //public Sprite[] memoryPictures;
    //public Sprite[] shuffeledPictures;

    //public List<Sprite> memoryCards = new List<Sprite>();
    //TODO: fotos aufrufen
    public List<Card> cards = new List<Card>();

    private bool firstGuess, secondGuess;
    private string firstGuessCard, secondGuessCard;
    //private int countGuesses;
    //private int countCorrectGuesses;
    //private int countIncorrectGuesses;
    private int gameGuesses;
    private int firstGuessIndex;
    private int secondGuessIndex;
    //public Card card = new Card();


    private void Awake()
    {
        //memoryPictures = Resources.LoadAll<Sprite>("Sprites/diamond-pearl"); //path
        //shuffeledPictures = ShuffleSprites(memoryPictures));//todo: cards need to be shuffeled
        //TODO: initialize
    }
    void Start()
    {
        GetButtons();
        AddListeners();
        //AddPictures();
        Shuffle(memoryCards);
        gameGuesses=memoryCards.Count/2;
      
    }

    void GetButtons()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("MemoryCard");
        for (int i = 0; i < objects.Length; i++)
        {
            // add image for each button 
            cards.Add(objects[i].GetComponent<Card>());
            cards[i].image= backImage;
            //btns[i].cardDefinition.image.sprite = backImage;
        }
    }

    void AddListeners() //wird auch später für cardView gebraucht (OnButtonClick)
    {
        foreach (Card card in cards)
        {
            card.OnClick();//TODO: pickcard aufrufen, button anbinden?
                //OnClick(() => PickCard()); ;
            //btn.onClick.AddListener(() => PickCard());
        }
    }

    //bilder sollten schon da sein??
    //void AddPictures()
    //{
    //    int index = 0;
    //    for (int i = 0; i < cards.Count; i++)
    //    {
    //        if (index == cards.Count / pair_of)
    //        {
    //            index = 0;
    //        }

    //        memoryCards.Add(memoryPictures[index]);
    //        index++;
    //    }

    //}

    public void PickCard()
    {
        string name = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name;
        Debug.Log("Button number " + name + " was clicked");

        if (!firstGuess)
        {
            
            firstGuess = true;
            firstGuessIndex = int.Parse(name);
            firstGuessCard = memoryCards[firstGuessIndex].name;
            // setzt bild (also dreht karte um) 
            // todo: animation einbauen
            cards[firstGuessIndex].image = memoryCards[firstGuessIndex];
            cards[firstGuessIndex].is_revealed=true;
            //cards[firstGuessIndex].interactable = false;

        }
        else if (!secondGuess)
        {
            secondGuess = true;
            secondGuessIndex = int.Parse(name);
            secondGuessCard = memoryCards[secondGuessIndex].name;
            // setzt bild (also dreht karte um)
            cards[secondGuessIndex].image= memoryCards[secondGuessIndex];
            cards[firstGuessIndex].is_revealed = true;
            //cards[secondGuessIndex].interactable = false;
          
            StartCoroutine(CheckForMatch());

            if (firstGuessCard == secondGuessCard)
            {
                Debug.Log("Found a match");
                //cards[secondGuessIndex].CorrectGuesses++;//TODO:
            }
            else
            {
                Debug.Log("Cards don't match");
                //cards[secondGuessIndex].IncorrectGuesses++;//TODO:
            }
        }
    }

    IEnumerator CheckForMatch()
    {
        yield return new WaitForSeconds(1f);
        if (firstGuessCard == secondGuessCard)
        {
            yield return new WaitForSeconds (.5f);
            //cards[firstGuessIndex].interactable = false;
            //cards[secondGuessIndex].interactable = false;
            //cards[firstGuessIndex].image.color = new Color(0, 0, 0, 0); //TODO: karte ausblenden
            //cards[secondGuessIndex].image.color = new Color(0, 0, 0, 0);
            if (pair_of > 2) {
                foreach (var card in cards)
                {
                    Debug.Log("btn: " + card + " bild des buttons: " + card.image.name);
                    Debug.Log("zu vergleichendes bild:" + cards[firstGuessIndex].image.name);
                    if (card.image.name == cards[firstGuessIndex].image.name)
                    //TODO: id einführen
                    {
                        Debug.Log("image matcht");
                        //index: int index = employeeList.FindIndex(employee => employee.LastName.Equals(somename, StringComparison.Ordinal));
                       //btns[indexOf(btn)] btn.interactable = false;
                       // btn.image.color = new Color(0, 0, 0, 0);
                        //btns[int.Parse(btn)].interactable = false;
                        //btns[firstGuessIndex].image.color = new Color(0, 0, 0, 0);
                    }
                }
                //find all cards with same image
                //and set color to 0 and make uninteractable
                //RemoveMatchingCards(btns, btns[firstGuessIndex].image.sprite);

            }


            CheckIfGameOver();
        }
        else
        {
            //btns[firstGuessIndex].image.sprite = backImage;
            //btns[secondGuessIndex].image.sprite = backImage;
            //btns[firstGuessIndex].interactable = true; 
            //btns[secondGuessIndex].interactable = true;
        }
        yield return new WaitForSeconds(.5f);
        firstGuess = secondGuess = false;
    }

    void CheckIfGameOver()
    {
        //countCorrectGuesses++;
        //if(countCorrectGuesses == gameGuesses)
        //{
        //    Debug.Log("Game Over");
        //    Debug.Log("guesses:" + countGuesses);
        //}
    }

    void Shuffle(List<Sprite> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Sprite temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }

    }

    //void RemoveMatchingCards(List<Button> btns, Sprite img)
    //{
    //    Debug.Log("in remove matching cards");
    //    Debug.Log("übergebnes bild:"+img);


    //    foreach (var btn in btns)
    //    {
    //        Debug.Log("btn: "+btn+" bild des buttons: "+btn.image.sprite);

    //        if (btn.image.sprite == img)
    //        {
    //            Debug.Log("image matcht");
    //            //index: int index = employeeList.FindIndex(employee => employee.LastName.Equals(somename, StringComparison.Ordinal));
    //            btn.interactable = false;
    //            btn.image.color = new Color(0, 0, 0, 0);
    //            //btns[int.Parse(btn)].interactable = false;
    //            //btns[firstGuessIndex].image.color = new Color(0, 0, 0, 0);
    //        }
    //    }
    //}
}

