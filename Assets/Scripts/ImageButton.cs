using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ImageButton : MonoBehaviour
{
    [Header("References")]
    public Image imageDisplay;

    private string imageId;
    private bool isSelected = false;

    [System.NonSerialized]
    public UnityEvent<string> onImageClicked = new UnityEvent<string>();

    public void Initialize(string id, Sprite sprite)
    {
        imageId = id;
        if (imageDisplay != null)
            imageDisplay.sprite = sprite;

        SetSelected(false);
    }

    public void OnClick()
    {
        SetSelected(!isSelected);
        onImageClicked?.Invoke(imageId);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public string GetImageId() => imageId;
    public bool IsSelected() => isSelected;
}
