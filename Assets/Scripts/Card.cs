using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [Header("Card Info")]
    public int groupId;
    public Sprite frontSprite;
    
    private Image imageComponent;
    private Button buttonComponent;
    private bool isRevealed = false;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        buttonComponent = GetComponent<Button>();
        buttonComponent.onClick.AddListener(OnClick);
        
        // Zeige die Rückseite am Anfang
        Hide();
    }

    public void Reveal()
    {
        if (isRevealed) return;
        isRevealed = true;
        imageComponent.sprite = frontSprite;
        buttonComponent.interactable = false;
    }

    public void Hide()
    {
        isRevealed = false;
        // Verwende die Rückseite vom GameController
        if (GameController.Instance != null)
        {
            imageComponent.sprite = GameController.Instance.backSprite;
        }
        buttonComponent.interactable = true;
    }

    public bool IsRevealed()
    {
        return isRevealed;
    }

    private void OnClick()
    {
        if (isRevealed) return;
        GameController.Instance.CardRevealed(this);
    }
}
