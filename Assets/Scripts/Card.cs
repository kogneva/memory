using System;
using UnityEditor;
using UnityEngine;

public class Card : MonoBehaviour
{
    //fields
    public GameObject card_button;
    public GUID id;
    //public string name;
    public string image_path;
    public Sprite image; 

    //TODO: guesses außerdhalb der card zählen
    public int Guesses => CorrectGuesses + IncorrectGuesses;
    public int CorrectGuesses { get; private set; } = 0;
    public int IncorrectGuesses { get; private set; } = 0;

    //methods
    public bool is_revealed { get; set; } = false;
    //TODO: Set clickable
    //TODO: functions to build statistics
    // - pro paar, wie oft aufgedeckt bis  match (if selected && no_match -> count++)
    // - ggf ob direkt hintereinander oder nicht

    //public CardDefinition()
    //{
    //    id = new GUID();
    //    name = "hansTest";
    //    image_path = "C:/Users/ognev/Pictures/totorologo.jpg";

    //}

    public void Test()
    {
        image = GetSpriteFromPath(image_path);
    }
    private Sprite GetSpriteFromPath(string path)
    {
        if (path == null)
        {
            throw new Exception("no image path");
        }
        Texture2D texture = NativeGallery.LoadImageAtPath(path);
        if (texture == null)
        {
            throw new Exception("no image found");
        }
        Sprite sprite_image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        return sprite_image;
        // TODO: ggf bilder in extra ordner auf dem gerät speichern
    }
    private Sprite SetSprite(Sprite sprite_image)
    {
        return image = sprite_image;
    }

    public void CountGuesses(bool guessed)//all guesses until round completion, count maybe in gameController
    {
        if (guessed == false)
        {
            IncorrectGuesses += 1;
        }
        else
        {
            CorrectGuesses += 1;
        }
    }
    public void OnClick()
    {
        Debug.Log("card clicked:");
        int card_id = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.gameObject.GetInstanceID();
        Debug.Log("Button id:  " + card_id + " was clicked");
        //TODO: send event button clicked ??? einfach im controller vergleichen


    }
}
