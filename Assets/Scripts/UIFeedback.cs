using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static FileManager;
using static UnityEngine.EventSystems.EventTrigger;

public class UIFeedback : MonoBehaviour
{
    // Status:
    public TMP_Text statusText;
    public TMP_Text previewText;

    // Folder:
    public GameObject invalidFolderText;
    public Button goButton;
    public TMP_InputField folderInput;
    public EventTrigger clickToFinish;

    // Folder:
    public void InitializeFolderInput()
    {
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);
        if (folderPath != "")
        {
            folderInput.text = folderPath;
        }
        OnUpdateFolderPath(folderPath);
    }

    public void OnUpdateFolderPath(string path)
    {
        var success = Directory.Exists(path);
        invalidFolderText.SetActive(!success);
        goButton.interactable = success;
        if (success)
        {
            PlayerPrefs.SetString(SAVED_FOLDER_KEY, path);
            PlayerPrefs.Save();
        }
    }

    // Status:
    public void FinishedConversion(TradingViewData tradingViewData, string debugText)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Copied to clipboard!");
        sb.AppendLine("<size=35%><color=#ABABAB>");
        sb.AppendLine($"{tradingViewData.historyFileName}");
        sb.AppendLine($"{tradingViewData.positionsFileName}");
        sb.AppendLine("<color=white>");
        sb.AppendLine("<size=50%>Click or press Esc / Return / Space to close the app.");

        statusText.text = sb.ToString();
        previewText.text = debugText;

        var entry = new Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener(data => Quit());

        clickToFinish.triggers.Add(entry);

        StopAllCoroutines();
        StartCoroutine(CloseOnInput());
    }

    IEnumerator CloseOnInput()
    {
        while (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }

        Quit();
    }

    void Quit()
    {
        //Application.OpenURL(tradingJournalURL);
        Application.Quit();
    }
}
