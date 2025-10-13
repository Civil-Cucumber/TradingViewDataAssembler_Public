using UnityEngine;

public class Initializer : MonoBehaviour
{
    public TradingViewDataAssembler dataAssembler;
    public FileManager fileManager;
    public ConfigManager configManager;
    public UIFeedback uiFeedback;

    void Start()
    {
        try
        {
            configManager.LoadConfig();
        }
        catch
        {
            uiFeedback.FailedConversion("Config issue!", "Something went wrong while accessing/reading SettingsConfig.json");
        }
        var hasInteractiveBrokersUserId = !string.IsNullOrEmpty(configManager.Config.interactiveBrokersUserId);
        uiFeedback.InitializeInput(hasInteractiveBrokersUserId);
        ConvertData();
    }

    public void ConvertData()
    {
        var loadSuccess = fileManager.OpenFiles(out var tradingViewData, out var interactiveBrokersData, configManager.Config.interactiveBrokersUserId, uiFeedback);
        if (loadSuccess)
        {
            try
            {
                dataAssembler.AssembleData(tradingViewData, interactiveBrokersData);
            }
            catch
            {
                uiFeedback.FailedConversion("Couldn't read / convert data!", "There was a problem when trying to read out / convert the data from the CSV files.");
            }
        }
    }
}