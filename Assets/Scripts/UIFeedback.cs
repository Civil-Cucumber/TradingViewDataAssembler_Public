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

    // Broker:
    public ToggleGroup toggleGroup;
    public Toggle paperTrading;
    public Toggle ibkr;
    public Toggle interactiveBrokers;
    
    // ConfigManager:
    public ConfigManager configManager;
    
    bool isClosing = false;

    // Folder:
    public void InitializeInput(bool hasInteractiveBrokersId)
    {
        interactiveBrokers.gameObject.SetActive(hasInteractiveBrokersId);
        
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);
        if (folderPath != "")
        {
            folderInput.text = folderPath;
        }
        OnUpdateFolderPath(folderPath);

        var brokerIndex = PlayerPrefs.GetInt(SAVED_BROKER_INDEX);
        if ((Broker)brokerIndex == Broker.InteractiveBrokers && !hasInteractiveBrokersId)
        {
            brokerIndex = (int)Broker.TV_PaperTrading;
        }
        OnUpdateBroker(brokerIndex);
    }

    public void OnUpdateBroker(int brokerIndex)
    {
        toggleGroup.SetAllTogglesOff(false);
        switch (brokerIndex)
        {
            default:
            case (int)Broker.TV_PaperTrading:
                paperTrading.SetIsOnWithoutNotify(true);
                break;
            case (int)Broker.TV_IBKR:
                ibkr.SetIsOnWithoutNotify(true);
                break;
            case (int)Broker.InteractiveBrokers:
                interactiveBrokers.SetIsOnWithoutNotify(true);
                break;
        }
        PlayerPrefs.SetInt(SAVED_BROKER_INDEX, brokerIndex);
        PlayerPrefs.Save();
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
    public void FinishedConversion(TradingViewData tradingViewData, InteractiveBrokersData interactiveBrokersData, string debugText)
    {
        var brokerIndex = PlayerPrefs.GetInt(SAVED_BROKER_INDEX);
        
        var sb = new StringBuilder();

        sb.AppendLine("Copied to clipboard!");
        sb.AppendLine("<size=35%><color=#ABABAB>");

        if ((Broker)brokerIndex == Broker.InteractiveBrokers)
        {
            sb.AppendLine($"{interactiveBrokersData.ibFileName}");
        }
        else
        {
            sb.AppendLine($"{tradingViewData.historyFileName}");
            sb.AppendLine($"{tradingViewData.positionsFileName}");
            sb.AppendLine($"{tradingViewData.ordersFileName}");
        }
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

    public void FailedConversion(string errorTitle, string errorMessage)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<color=#FF9292>ERROR: " + errorTitle);
        sb.AppendLine("<size=50%><color=white>");
        sb.AppendLine(errorMessage);
        sb.AppendLine();
        
        statusText.text = sb.ToString();
        previewText.text = "(Please read https://github.com/Civil-Cucumber/TradingViewDataAssembler_Public to make sure you follow all steps as described.)";
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
        if (!isClosing)
        {
            isClosing = true;
            var broker = (Broker)PlayerPrefs.GetInt(SAVED_BROKER_INDEX);
            var tradingJournalURL = configManager.Config.tvPaperTradingJournalUrl;
            switch (broker)
            {
                case Broker.TV_PaperTrading:
                    tradingJournalURL = configManager.Config.tvPaperTradingJournalUrl;
                    break;
                case Broker.TV_IBKR:
                    tradingJournalURL = configManager.Config.tvIbkrJournalUrl;
                    break;
                case Broker.InteractiveBrokers:
                    tradingJournalURL = configManager.Config.interactiveBrokersJournalUrl;
                    break;
            }
            if (tradingJournalURL != string.Empty)
            {
                Application.OpenURL(tradingJournalURL);
            }
            Application.Quit();
        }
    }
}
