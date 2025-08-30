using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class FileManager : MonoBehaviour
{
    public const string SAVED_FOLDER_KEY = "folderPath";
    public const string SAVED_BROKER_INDEX = "brokerIndex";

    const string PT_HISTORY_FILE_KEYWORD = "paper-trading-history-all";
    const string PT_POSITIONS_FILE_KEYWORD = "paper-trading-positions";

    const string IBKR_HISTORY_FILE_KEYWORD = "interactive-brokers-trade-history";
    const string IBKR_POSITIONS_FILE_KEYWORD = "interactive-brokers-positions";
    const string IBKR_ORDERS_FILE_KEYWORD = "interactive-brokers-orders-all";

    public bool OpenFiles(out TradingViewData tradingViewData, UIFeedback uiFeedback)
    {
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);

        if (!Directory.Exists(folderPath))
        {
            uiFeedback.FailedConversion("Folder doesn't exist!", "The selected folder does NOT exist!");
            Debug.LogError("Folder doesn't exist!");
            tradingViewData = null;

            return false;
        }

        var broker = (Broker)PlayerPrefs.GetInt(SAVED_BROKER_INDEX, 0);
        var historyFileKeyword = broker == Broker.PaperTrading ? PT_HISTORY_FILE_KEYWORD : IBKR_HISTORY_FILE_KEYWORD;
        var positionsFileKeyword = broker == Broker.PaperTrading ? PT_POSITIONS_FILE_KEYWORD : IBKR_POSITIONS_FILE_KEYWORD;
        var ordersFileKeyword = IBKR_ORDERS_FILE_KEYWORD;

        var historyFiles = new List<FileData>();
        var positionsFiles = new List<FileData>();
        var ordersFiles = new List<FileData>();

        DirectoryInfo directory = new DirectoryInfo(folderPath);
        var csvFiles = directory.GetFiles("*.csv");

        foreach (var file in csvFiles)
        {
            if (file.Name.Contains(historyFileKeyword))
            {
                historyFiles.Add(new FileData(file.Name));
            }
            else if (file.Name.Contains(positionsFileKeyword))
            {
                positionsFiles.Add(new FileData(file.Name));
            }
            else if (file.Name.Contains(ordersFileKeyword))
            {
                ordersFiles.Add(new FileData(file.Name));
            }
        }


        var newestHistoryFile = historyFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
        var newestPositionsFile = positionsFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
        var newestOrdersFile = ordersFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
        

        if (newestHistoryFile == null || newestPositionsFile == null || (newestOrdersFile == null && broker == Broker.IBKR))
        {
            uiFeedback.FailedConversion("Missing Files!", "At least one of the required CSV files is missing in the selected folder.");
            Debug.LogError("At least one of the required CSV files is missing in the selected folder.");
        }

        var historyFileName = newestHistoryFile.name;
        var positionsFileName = newestPositionsFile.name;
        // Orders file is only necessary for IBKR:
        var orderFileName = newestOrdersFile != null && broker == Broker.IBKR ? newestOrdersFile.name : string.Empty;


        try
        {
            var historyCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + historyFileName);
            var positionsCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + positionsFileName);
            var ordersCsv = orderFileName != string.Empty ? File.ReadAllText(folderPath + Path.DirectorySeparatorChar + orderFileName) : string.Empty;

            tradingViewData = new TradingViewData
            {
                history = CsvReader.Read(historyCsv),
                positions = CsvReader.Read(positionsCsv),
                orders = orderFileName != string.Empty ? CsvReader.Read(ordersCsv) : null,

                historyFileName = historyFileName,
                positionsFileName = positionsFileName,
                ordersFileName = orderFileName
            };
        }
        catch
        {
            uiFeedback.FailedConversion("Can't access files!", "The CSV files need to be closed!");
            Debug.LogError("The CSV files need to be closed!");
            tradingViewData = null;

            return false;
        }

        return true;
    }

    class FileData
    {
        public string name;
        public DateTime time;

        public FileData(string name)
        {
            this.name = name;
            time = GetTime();
        }

        DateTime GetTime()
        {
            var dateStartIndex = name.IndexOf('2');
            var dateEndIndex = name.IndexOf('T', dateStartIndex);
            var timeStartIndex = dateEndIndex + 1;

            var dateString = name.Substring(dateStartIndex, dateEndIndex - dateStartIndex);
            var dateValues = dateString.Split('-');
            dateString = $"{dateValues[2]}.{dateValues[1]}.{dateValues[0]}";

            var timeString = name.Substring(timeStartIndex, 8);
            timeString = $"{timeString[0]}{timeString[1]}:{timeString[3]}{timeString[4]}:{timeString[6]}{timeString[7]}";

            var dateTimeString = $"{dateString} {timeString}";
            var dateTimeCulture = new CultureInfo("de-DE");
            return DateTime.Parse(dateTimeString, dateTimeCulture);
        }
    }

    public class TradingViewData
    {
        public List<Dictionary<string, string>> history;
        public List<Dictionary<string, string>> positions;
        public List<Dictionary<string, string>> orders;

        public string historyFileName;
        public string positionsFileName;
        public string ordersFileName;
    }
}