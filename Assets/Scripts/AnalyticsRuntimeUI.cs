using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nutzt TextMeshPro für bessere Darstellung
using System.IO;
using System.Linq;

public class AnalyticsRuntimeUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject listViewPanel;
    public GameObject detailViewPanel;

    [Header("List View References")]
    public Transform listContentParent; // Das Content-Objekt einer ScrollView
    public GameObject fileButtonPrefab; // Ein Prefab mit einem Button und TextMeshProUGUI

    [Header("Detail View References")]
    public TMP_Text detailContentText;  // Text-Komponente für den JSON-Inhalt

    public void OpenStatisticsMenu()
    {
        gameObject.SetActive(true);
        ShowListView();
    }

    public void CloseStatisticsMenu()
    {
        gameObject.SetActive(false);
    }

    private void ShowListView()
    {
        listViewPanel.SetActive(true);
        detailViewPanel.SetActive(false);
        RefreshFiles();
    }

    public void ShowDetailView(string filePath)
    {
        if (File.Exists(filePath))
        {
            string content = File.ReadAllText(filePath);
            detailContentText.text = content;

            listViewPanel.SetActive(false);
            detailViewPanel.SetActive(true);
        }
    }

    public void BackToListView()
    {
        ShowListView();
    }

    private void RefreshFiles()
    {
        // Alte Buttons löschen
        foreach (Transform child in listContentParent)
        {
            Destroy(child.gameObject);
        }

        string dirInfo = Application.persistentDataPath;
        if (Directory.Exists(dirInfo))
        {
            string[] filePaths = Directory.GetFiles(dirInfo, "Analytics_*.json")
                                          .OrderByDescending(f => File.GetLastWriteTime(f))
                                          .ToArray();

            foreach (string path in filePaths)
            {
                // Erstelle einen neuen Button aus dem Prefab
                GameObject btnObj = Instantiate(fileButtonPrefab, listContentParent);

                // Setze den Text des Buttons auf den Dateinamen
                TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = Path.GetFileName(path);
                }

                // Füge das Klick-Event hinzu
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    string capturedPath = path; // Wichtig für die Closure (Lambda)
                    btn.onClick.AddListener(() => ShowDetailView(capturedPath));
                }
            }
        }
    }
}