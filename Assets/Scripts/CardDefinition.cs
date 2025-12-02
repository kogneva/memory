
using System;
using UnityEditor;
using UnityEngine;


public class CardDefinition
//for database
{
    public GUID id;
    public string name;
    public string image_path;//in db
    public Sprite image; // is on runtime, not in database 

    public CardDefinition()
    {
        id = new GUID();
        name = "hansTest";
        image_path = "C:/Users/ognev/Pictures/totorologo.jpg";

    }

    public void Test()
    {
        image = GetSpriteFromPath(image_path);
    }
    private Sprite GetSpriteFromPath(string path)
    {
       
        Texture2D texture = NativeGallery.LoadImageAtPath(path);
        Sprite sprite_image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        return sprite_image;
        //TODO: get and set image by using image path
        // check if valid path -> send error if no pic found ODER bilder in extra ordner auf dem gerät speichern
    }
}
