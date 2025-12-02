using UnityEngine;
using System.Collections;

public class AddMemoryButtons : MonoBehaviour
{
    [SerializeField]
    private Transform memoryField;

    [SerializeField]
    private GameObject btn;

    [SerializeField]
    public int pair_count = 32;
    private void Awake(){
        for(int i = 0; i<pair_count*2; i++){
            GameObject button = Instantiate(btn);
            button.name = "" + i;

            button.transform.SetParent(memoryField,false);
        }

    }

}
