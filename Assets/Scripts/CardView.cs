using UnityEditor;
using UnityEngine;

public class CardView : MonoBehaviour
// GameObject card that is used for unity
{
    //fields
    public GameObject card_button;
    private CardInstance card;
    private GameManager manger;//braucht? oder andersrum


    //methods
    public void OnButtonClick()
    {
        Debug.Log("card_instance:"+card);
        int card_id = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.gameObject.GetInstanceID();
        Debug.Log("Button id:  " + card_id + " was clicked");

    }
    public void BindSprite(GUID card_definition_id, Sprite card_definition_sprite, GameManager manager){}
}
