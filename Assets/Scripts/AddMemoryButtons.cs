using UnityEngine;

[ExecuteAlways]
public class AddMemoryButtons : MonoBehaviour
{
    [SerializeField]
    private Transform memoryField;

    [SerializeField]
    private GameObject btn;

    [SerializeField]
    private GameController gameController;

    [Header("Configuration")]
    [Tooltip("Anzahl der Gruppen (0 = Fallback verwenden)")]
    [SerializeField]
    private int fallbackGroupCount = 4;

    [Tooltip("Karten pro Gruppe")]
    [SerializeField]
    private int fallbackGroupSize = 2;

    // GroupCount: Wenn GameController 0 liefert oder nicht existiert, nutze Fallback
    public int GroupCount
    {
        get
        {
            int count = gameController != null ? gameController.DefaultGroupCount : 0;
            return count > 0 ? count : fallbackGroupCount;
        }
    }

    // GroupSize: Wenn GameController 0 liefert oder nicht existiert, nutze Fallback
    public int GroupSize
    {
        get
        {
            int size = gameController != null ? gameController.DefaultGroupSize : 0;
            return size > 0 ? size : fallbackGroupSize;
        }
    }

    public int TotalCards => GroupCount * GroupSize;

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = FindAnyObjectByType<GameController>();
        }

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

        int groupCount = GroupCount;
        int groupSize = GroupSize;
        int total = groupCount * groupSize;

        Debug.Log($"AddMemoryButtons: Generating {total} cards ({groupCount} groups × {groupSize} cards per group)");

        for (int i = 0; i < total; i++)
        {
            GameObject button = Instantiate(btn, memoryField, false);
            button.name = i.ToString();

            try
            {
                button.tag = "MemoryCard";
            }
            catch { }

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
