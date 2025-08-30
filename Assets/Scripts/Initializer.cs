using UnityEngine;

public class Initializer : MonoBehaviour
{
    public TradingViewDataAssembler dataAssembler;
    public FileManager fileManager;
    public ConfigManager configManager;
    public UIFeedback uiFeedback;

    void Start()
    {
        uiFeedback.InitializeInput();
        try
        {
            configManager.LoadConfig();
        }
        catch
        {
            uiFeedback.FailedConversion("Config issue!", "Something went wrong while accessing/reading SettingsConfig.json");
        }
        ConvertData();
    }

    public void ConvertData()
    {
        var loadSuccess = fileManager.OpenFiles(out var tradingViewData, uiFeedback);
        if (loadSuccess)
        {
            try
            {
                dataAssembler.AssembleData(tradingViewData);
            }
            catch
            {
                uiFeedback.FailedConversion("Couldn't read / convert data!", "There was a problem when trying to read out / convert the data from the CSV files.");
            }
        }
    }
}