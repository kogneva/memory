using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class AnalyticsBrowserWindow : EditorWindow
{
    private string[] filePaths;
    private Vector2 listScrollPosition;
    private Vector2 detailScrollPosition;

    // View State
    private string viewedFilePath = null;
    private string viewedFileContent = null;

    [MenuItem("Window/Analytics Browser")]
    public static void ShowWindow()
    {
        GetWindow<AnalyticsBrowserWindow>("Analytics Browser");
    }

    private void OnEnable()
    {
        RefreshFiles();
    }

    private void RefreshFiles()
    {
        string dirInfo = Application.persistentDataPath;
        if (Directory.Exists(dirInfo))
        {
            filePaths = Directory.GetFiles(dirInfo, "Analytics_*.json")
                                 .OrderByDescending(f => File.GetLastWriteTime(f))
                                 .ToArray();
        }
        else
        {
            filePaths = new string[0];
        }
    }

    private void OnGUI()
    {
        // Entscheide anhand des Status, welche Ansicht gezeichnet wird
        if (string.IsNullOrEmpty(viewedFileContent))
        {
            DrawListView();
        }
        else
        {
            DrawDetailView();
        }
    }

    private void DrawListView()
    {
        GUILayout.Space(10);
        GUILayout.Label("Gespeicherte JSON Statistiken", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Aktualisieren", GUILayout.Height(25)))
        {
            RefreshFiles();
        }

        if (GUILayout.Button("Speicherordner öffnen", GUILayout.Height(25)))
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (filePaths == null || filePaths.Length == 0)
        {
            GUILayout.Label("Keine lokalen Analytics-Dateien gefunden.");
            return;
        }

        listScrollPosition = GUILayout.BeginScrollView(listScrollPosition);

        foreach (string path in filePaths)
        {
            GUILayout.BeginHorizontal("box");

            GUILayout.Label(Path.GetFileName(path), GUILayout.ExpandWidth(true));

            // NEU: Lädt den Text der Datei in den Speicher und wechselt in die Detailansicht
            if (GUILayout.Button("Ansehen", GUILayout.Width(70)))
            {
                viewedFilePath = path;
                viewedFileContent = File.ReadAllText(path);
                detailScrollPosition = Vector2.zero; // Scroll-Position für neue Datei zurücksetzen
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Löschen", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Datei löschen", $"Möchten Sie\n{Path.GetFileName(path)}\nwirklich löschen?", "Ja", "Abbrechen"))
                {
                    File.Delete(path);
                    RefreshFiles();
                    GUI.backgroundColor = Color.white;
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawDetailView()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        // Zurück-Button, resettet die Detail-Variablen und führt uns damit zurück in die Liste
        if (GUILayout.Button("← Zurück zur Liste", GUILayout.Width(150), GUILayout.Height(25)))
        {
            viewedFileContent = null;
            viewedFilePath = null;
            GUIUtility.ExitGUI();
        }

        // Zeigt den Namen der aktuell geöffneten Datei an
        GUILayout.Label(Path.GetFileName(viewedFilePath), EditorStyles.boldLabel, GUILayout.Height(25));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // In einem formatierten Textfeld rendern
        detailScrollPosition = GUILayout.BeginScrollView(detailScrollPosition);

        // Wir nutzen eine TextArea, damit der Text kopierbar bleibt.
        // Das Setzen auf eine eigene Variable verhindert, dass der Nutzer den Text hier überschreiben und löschen kann (Read-Only Feel).
        EditorGUILayout.TextArea(viewedFileContent, GUILayout.ExpandHeight(true));

        GUILayout.EndScrollView();
    }
}
