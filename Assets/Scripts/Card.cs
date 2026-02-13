using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [Header("Card Info")]
    public string groupId; // switched to string GUID
    public Sprite frontSprite;
    
    private Image imageComponent;
    private Button buttonComponent;
    private bool isRevealed;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        buttonComponent = GetComponent<Button>();
        buttonComponent.onClick.AddListener(OnClick);

        // Prevent interaction until front sprite is assigned
        if (buttonComponent != null)
            buttonComponent.interactable = false;
    }

    void Start()
    {
        Hide();
    }

    public void Reveal()
    {
        if (isRevealed) return;
        if (frontSprite == null)
        {
            Debug.LogWarning($"Trying to reveal card but frontSprite is null on {gameObject.name}");
            return;
        }

        isRevealed = true;
        imageComponent.sprite = frontSprite;
        if (buttonComponent != null)
            buttonComponent.interactable = false;
    }

    public void Hide()
    {
        isRevealed = false;
        if (GameController.Instance != null && GameController.Instance.backSprite != null)
        {
            imageComponent.sprite = GameController.Instance.backSprite;
        }
        // keep button disabled until front sprite assigned
        // if frontSprite already assigned, ensure interactable true
        if (frontSprite != null && buttonComponent != null)
            buttonComponent.interactable = true;

        // Ensure image is visible when hiding (reset alpha and raycast)
        if (imageComponent != null)
        {
            Color c = imageComponent.color;
            imageComponent.color = new Color(c.r, c.g, c.b, 1f);
            imageComponent.raycastTarget = true;
        }
    }

    public bool IsRevealed()
    {
        return isRevealed;
    }

    // Safe setter used by GameController to assign front sprites and enable interaction
    public void SetFrontSprite(Sprite sprite)
    {
        frontSprite = sprite;
        if (sprite != null && buttonComponent != null)
        {
            // Only enable interaction when a valid front sprite is present
            buttonComponent.interactable = true;
        }
    }

    private void OnClick()
    {
        if (isRevealed) return;
        // Log which card was clicked
        Debug.Log($"Card clicked: {gameObject.name} (groupId={groupId})");
        if (GameController.Instance != null)
        {
            GameController.Instance.CardRevealed(this);
        }
        else
        {
            Debug.LogWarning("GameController.Instance ist null beim Klick auf Karte.");
        }
    }

    // Neue Methode: Markiere Karte als gefundene (nicht interaktiv, visuell transparent)
    public void MarkAsMatched(float alpha = 0f)
    {
        // Disable interaction
        if (buttonComponent != null)
            buttonComponent.interactable = false;

        // Make image fully transparent and disable raycast so it's not visible/interactable
        if (imageComponent != null)
        {
            Color c = imageComponent.color;
            imageComponent.color = new Color(c.r, c.g, c.b, alpha);
            imageComponent.raycastTarget = false;
        }
    }
}
