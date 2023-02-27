using UnityEngine;

public class Initializer : MonoBehaviour
{
    public TradingViewDataAssembler dataAssembler;
    public FileManager fileManager;
    public UIFeedback uiFeedback;

    void Start()
    {
        uiFeedback.InitializeFolderInput();
        ConvertData();
    }

    public void ConvertData()
    {
        var loadSuccess = fileManager.OpenFiles(out var tradingViewData);
        if (loadSuccess)
        {
            dataAssembler.AssembleData(tradingViewData);
        }
    }
}