using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutOptimizer : MonoBehaviour
{
    private GridLayoutGroup gridLayout;

    [Tooltip("Automatisch bei Änderungen der Kinder aktualisieren")]
    public bool autoUpdate = true;

    void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
    }

    void Start()
    {
        UpdateGridColumns();
    }

    void OnTransformChildrenChanged()
    {
        if (autoUpdate)
        {
            UpdateGridColumns();
        }
    }

    /// <summary>
    /// Berechnet und setzt die optimale Spaltenanzahl für ein möglichst quadratisches Grid.
    /// </summary>
    public void UpdateGridColumns()
    {
        int childCount = GetActiveChildCount();
        
        if (childCount == 0)
            return;

        int columns = CalculateOptimalColumns(childCount);
        
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;

        int rows = Mathf.CeilToInt((float)childCount / columns);
        Debug.Log($"GridLayoutOptimizer: {childCount} Karten -> {columns} Spalten x {rows} Zeilen");
    }

    /// <summary>
    /// Berechnet die optimale Spaltenanzahl für ein möglichst quadratisches Grid.
    /// Bevorzugt mehr Spalten als Zeilen (z.B. 5x4 statt 4x5).
    /// </summary>
    private int CalculateOptimalColumns(int count)
    {
        if (count <= 0)
            return 1;

        // Quadratwurzel als Ausgangspunkt
        int sqrt = Mathf.CeilToInt(Mathf.Sqrt(count));

        // Suche die beste Spaltenanzahl nahe der Quadratwurzel
        int bestColumns = sqrt;
        int bestDifference = int.MaxValue;

        // Prüfe Werte um die Quadratwurzel herum
        for (int cols = Mathf.Max(1, sqrt - 2); cols <= sqrt + 2; cols++)
        {
            int rows = Mathf.CeilToInt((float)count / cols);
            int difference = Mathf.Abs(rows - cols);

            // Bevorzuge Layouts mit weniger Differenz zwischen Zeilen und Spalten
            // Bei gleicher Differenz: bevorzuge mehr Spalten (cols > bestColumns)
            if (difference < bestDifference || 
                (difference == bestDifference && cols > bestColumns))
            {
                bestDifference = difference;
                bestColumns = cols;
            }
        }

        return bestColumns;
    }

    /// <summary>
    /// Zählt nur aktive Kind-Objekte.
    /// </summary>
    private int GetActiveChildCount()
    {
        int count = 0;
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Manuelle Aktualisierung von außen aufrufen.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateGridColumns();
    }
}