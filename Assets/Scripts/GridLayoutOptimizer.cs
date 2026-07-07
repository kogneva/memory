using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridLayoutOptimizer : MonoBehaviour
{
    private GridLayoutGroup gridLayout;
    private RectTransform rectTransform;

    [Tooltip("Automatisch bei Änderungen der Kinder aktualisieren")]
    public bool autoUpdate = true;

    [Header("Cell Size Settings")]
    [Tooltip("Minimale Zellgröße (Breite und Höhe)")]
    public Vector2 minCellSize = new Vector2(50f, 50f);

    [Tooltip("Maximale Zellgröße (Breite und Höhe)")]
    public Vector2 maxCellSize = new Vector2(200f, 200f);

    [Tooltip("Seitenverhältnis der Karten (Breite/Höhe). 1 = quadratisch")]
    public float cardAspectRatio = 1f;

    [Header("Spacing Settings")]
    [Tooltip("Padding: Links, Rechts, Oben, Unten")]
    public int paddingLeft = 10;
    public int paddingRight = 10;
    public int paddingTop = 10;
    public int paddingBottom = 10;

    [Tooltip("Abstand zwischen den Karten")]
    public Vector2 spacing = new Vector2(10f, 10f);

    [Header("Dynamic Layout")]
    [Tooltip("Optional: Ein UI-Element (z.B. ein Verlassen-Button), unter dem das Grid beginnen soll.")]
    public RectTransform avoidTopElement;

    private RectOffset padding;

    void Awake()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
        padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
    }

    void Start()
    {
        // Warte einen Frame, damit das Layout korrekt initialisiert ist
        StartCoroutine(DelayedUpdate());
    }

    private System.Collections.IEnumerator DelayedUpdate()
    {
        yield return null;
        UpdateGridLayout();
    }

    void OnTransformChildrenChanged()
    {
        if (autoUpdate)
        {
            UpdateGridLayout();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        if (autoUpdate && gridLayout != null)
        {
            UpdateGridLayout();
        }
    }

    /// <summary>
    /// Berechnet und setzt das optimale Grid-Layout, damit alle Karten auf den Bildschirm passen.
    /// </summary>
    public void UpdateGridLayout()
    {
        int childCount = GetActiveChildCount();

        if (childCount == 0)
            return;

        // Dynamisches Top-Padding berechnen
        int dynamicPaddingTop = paddingTop;
        if (avoidTopElement != null && avoidTopElement.gameObject.activeInHierarchy)
        {
            // Höhe des UI-Elements plus einen kleinen Basis-Abstand zusammenrechnen
            dynamicPaddingTop += Mathf.CeilToInt(avoidTopElement.rect.height) + 10; // 10 ist der Basis-Abstand
        }

        // Padding aktualisieren
        padding = new RectOffset(paddingLeft, paddingRight, dynamicPaddingTop, paddingBottom);

        // Padding anwenden
        gridLayout.padding = padding;
        gridLayout.spacing = spacing;

        // Verfügbare Größe berechnen
        Vector2 availableSize = GetAvailableSize();

        // Optimales Layout berechnen
        LayoutResult result = CalculateOptimalLayout(childCount, availableSize);

        // Layout anwenden
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = result.columns;
        gridLayout.cellSize = result.cellSize;

        Debug.Log($"GridLayoutOptimizer: {childCount} Karten -> {result.columns}x{result.rows}, " +
                  $"Zellgröße: {result.cellSize.x:F1}x{result.cellSize.y:F1}, " +
                  $"Verfügbar: {availableSize.x:F1}x{availableSize.y:F1}");
    }

    /// <summary>
    /// Berechnet die verfügbare Größe für das Grid (abzüglich Padding).
    /// </summary>
    private Vector2 GetAvailableSize()
    {
        Rect rect = rectTransform.rect;

        // Nutze hier das fertig berechnete padding.top/bottom/left/right Objekt (inkl. dynamischer Button-Höhe)
        float availableWidth = rect.width - padding.left - padding.right;
        float availableHeight = rect.height - padding.top - padding.bottom;

        return new Vector2(
            Mathf.Max(availableWidth, 100f),
            Mathf.Max(availableHeight, 100f)
        );
    }

    /// <summary>
    /// Berechnet das optimale Layout (Spalten, Zeilen, Zellgröße) für die gegebene Kartenanzahl.
    /// </summary>
    private LayoutResult CalculateOptimalLayout(int count, Vector2 availableSize)
    {
        if (count <= 0)
            return new LayoutResult { columns = 1, rows = 1, cellSize = maxCellSize };

        LayoutResult bestResult = new LayoutResult();
        float bestScore = float.MinValue;

        // Probiere verschiedene Spaltenzahlen durch
        int maxColumns = Mathf.Min(count, Mathf.CeilToInt(availableSize.x / minCellSize.x));
        int minColumns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count) * 0.5f));

        for (int cols = minColumns; cols <= maxColumns; cols++)
        {
            int rows = Mathf.CeilToInt((float)count / cols);

            // Berechne maximale Zellgröße für diese Konfiguration
            float maxCellWidth = (availableSize.x - (cols - 1) * spacing.x) / cols;
            float maxCellHeight = (availableSize.y - (rows - 1) * spacing.y) / rows;

            // Wende Aspect Ratio an
            float cellWidth, cellHeight;
            if (cardAspectRatio >= 1f)
            {
                // Breiter als hoch oder quadratisch
                cellWidth = Mathf.Min(maxCellWidth, maxCellHeight * cardAspectRatio);
                cellHeight = cellWidth / cardAspectRatio;
            }
            else
            {
                // Höher als breit
                cellHeight = Mathf.Min(maxCellHeight, maxCellWidth / cardAspectRatio);
                cellWidth = cellHeight * cardAspectRatio;
            }

            // Begrenze auf min/max Werte
            cellWidth = Mathf.Clamp(cellWidth, minCellSize.x, maxCellSize.x);
            cellHeight = Mathf.Clamp(cellHeight, minCellSize.y, maxCellSize.y);

            // Prüfe, ob alle Karten passen
            float totalWidth = cols * cellWidth + (cols - 1) * spacing.x;
            float totalHeight = rows * cellHeight + (rows - 1) * spacing.y;

            if (totalWidth > availableSize.x + 1f || totalHeight > availableSize.y + 1f)
                continue;

            // Bewertung: Größere Zellen und besseres Seitenverhältnis (rows ≈ cols) sind besser
            float sizeScore = cellWidth * cellHeight;
            float ratioScore = 1f / (1f + Mathf.Abs(rows - cols));
            float fillScore = (totalWidth * totalHeight) / (availableSize.x * availableSize.y);

            float score = sizeScore * 0.5f + ratioScore * 100f + fillScore * 50f;

            if (score > bestScore)
            {
                bestScore = score;
                bestResult = new LayoutResult
                {
                    columns = cols,
                    rows = rows,
                    cellSize = new Vector2(cellWidth, cellHeight)
                };
            }
        }

        // Fallback: Falls keine gültige Konfiguration gefunden wurde
        if (bestResult.columns == 0)
        {
            int sqrt = Mathf.CeilToInt(Mathf.Sqrt(count));
            bestResult.columns = sqrt;
            bestResult.rows = Mathf.CeilToInt((float)count / sqrt);
            bestResult.cellSize = minCellSize;
        }

        return bestResult;
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
        UpdateGridLayout();
    }

    /// <summary>
    /// Hilfsmethode zum Einstellen der Konfiguration zur Laufzeit.
    /// </summary>
    public void Configure(Vector2 minSize, Vector2 maxSize, float aspectRatio)
    {
        minCellSize = minSize;
        maxCellSize = maxSize;
        cardAspectRatio = aspectRatio;
        UpdateGridLayout();
    }

    private struct LayoutResult
    {
        public int columns;
        public int rows;
        public Vector2 cellSize;
    }
}