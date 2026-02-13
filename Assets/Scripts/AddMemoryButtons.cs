using UnityEngine;

[ExecuteAlways]
public class AddMemoryButtons : MonoBehaviour
{
    [SerializeField]
    private Transform memoryField;

    [SerializeField]
    private GameObject btn;

    [Header("Configuration")]
    [Tooltip("Number of pairs (each pair produces two cards).")]
    [SerializeField]
    public int pairs = 4;

    private void Awake()
    {
        // Only generate at runtime in Play Mode by default
        if (Application.isPlaying)
        {
            GenerateButtons();
        }
    }

    [ContextMenu("Generate Buttons")]
    public void GenerateButtons()
    {
        if (memoryField == null || btn == null)
        {
            Debug.LogWarning("AddMemoryButtons: memoryField or btn prefab is not assigned.");
            return;
        }

        ClearButtons();

        int total = Mathf.Max(0, pairs) * 2;
        for (int i = 0; i < total; i++)
        {
            GameObject button = Instantiate(btn, memoryField, false);
            button.name = i.ToString();
            button.transform.SetParent(memoryField, false);

            // set tag so GameController can find them
            try
            {
                button.tag = "MemoryCard";
            }
            catch { }

            // ensure RectTransform defaults
            var rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
            }
        }
    }

    [ContextMenu("Clear Buttons")]
    public void ClearButtons()
    {
        if (memoryField == null)
            return;

        // Destroy all child objects in edit and play mode
        int childCount = memoryField.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            var child = memoryField.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
