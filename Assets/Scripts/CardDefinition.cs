
//using System;
//using UnityEditor;
//using UnityEngine;


//public class CardDefinition
////for database
//{
//    public GUID id;
//    public string name;
//    public string image_path;//in db
//    public Sprite image; // is on runtime, not in database 

//    public CardDefinition()
//    {
//        id = new GUID();
//        name = "hansTest";
//        image_path = "C:/Users/ognev/Pictures/totorologo.jpg";

//    }

//    public void Test()
//    {
//        image = GetSpriteFromPath(image_path);
//    }
//    private Sprite GetSpriteFromPath(string path)
//    {
//        if (path == null)
//        {
//            throw new Exception("no image path");
//        }
//        Texture2D texture = NativeGallery.LoadImageAtPath(path);
//        if (texture == null)
//        {
//            throw new Exception("no image found");
//        }
//        Sprite sprite_image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
//        return sprite_image;
//        // TODO: ggf bilder in extra ordner auf dem gerät speichern
//    }
//    private Sprite SetSprite(Sprite sprite_image)
//    {
//       return image = sprite_image;
//    }
//}
