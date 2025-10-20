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

    // Folder:
    public GameObject invalidFolderText;
    public Button goButton;
    public TMP_InputField folderInput;
    public EventTrigger clickToFinish;

    // Broker:
    public ToggleGroup toggleGroup;
    public Toggle paperTrading;
    public Toggle ibkr;
    public Toggle ibPaper;
    public Toggle ibLive;
    public TMP_Text ibActivityStatementText;
    
    // ConfigManager:
    public ConfigManager configManager;
    
    bool isClosing = false;

    // Folder:
    public void InitializeInput()
    {
        var hasIbPaperUserId = !string.IsNullOrEmpty(configManager.Config.ibPaperUserId);
        var hasIbLiveUserId = !string.IsNullOrEmpty(configManager.Config.ibLiveUserId);
        
        ibPaper.gameObject.SetActive(hasIbPaperUserId);
        ibLive.gameObject.SetActive(hasIbLiveUserId);
        if (!hasIbPaperUserId && !hasIbLiveUserId)
        {
            ibActivityStatementText.color = new Color(0,0,0,0);
        }
        
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);
        if (folderPath != "")
        {
            folderInput.text = folderPath;
        }
        OnUpdateFolderPath(folderPath);

        var brokerIndex = PlayerPrefs.GetInt(SAVED_BROKER_INDEX);
        OnUpdateBroker(brokerIndex);
    }

    public void OnUpdateBroker(int brokerIndex)
    {
        var hasIbPaperUserId = !string.IsNullOrEmpty(configManager.Config.ibPaperUserId);
        var hasIbLiveUserId = !string.IsNullOrEmpty(configManager.Config.ibLiveUserId);
        
        if (((Broker)brokerIndex == Broker.IB_Paper && !hasIbPaperUserId) 
            || ((Broker)brokerIndex == Broker.IB_Live && !hasIbLiveUserId))
        {
            brokerIndex = (int)Broker.TV_PaperTrading;
        }
        
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
            case (int)Broker.IB_Paper:
                ibPaper.SetIsOnWithoutNotify(true);
                break;
            case (int)Broker.IB_Live:
                ibLive.SetIsOnWithoutNotify(true);
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

        if ((Broker)brokerIndex == Broker.IB_Paper || (Broker)brokerIndex == Broker.IB_Live)
        {
            sb.AppendLine($"{interactiveBrokersData.ibFileName}");
        }
        else
        {
            sb.AppendLine($"{tradingViewData.historyFileName}");
            sb.AppendLine($"{tradingViewData.positionsFileName}");

            if (tradingViewData.ordersFileName != "")
            {
                sb.AppendLine($"{tradingViewData.ordersFileName}");
            }
        }
        sb.AppendLine("<color=white>");
        sb.AppendLine("<size=50%>Click or press Esc / Return / Space to close the app.");
        
        sb.AppendLine("<size=33%><color=#ACACAC><align=\"left\">");
        sb.Append(debugText);

        statusText.text = sb.ToString();

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
        
        sb.AppendLine("<size=33%><color=#ACACAC><align=\"left\">");
        sb.AppendLine("(Please read https://github.com/Civil-Cucumber/TradingViewDataAssembler_Public to make sure you follow all steps as described.)");
        
        statusText.text = sb.ToString();
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
                case Broker.IB_Paper:
                    tradingJournalURL = configManager.Config.ibPaperJournalUrl;
                    break;
                case Broker.IB_Live:
                    tradingJournalURL = configManager.Config.ibLiveJournalUrl;
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
