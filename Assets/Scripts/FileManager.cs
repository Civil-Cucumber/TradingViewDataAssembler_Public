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

    const string TV_PT_HISTORY_FILE_KEYWORD = "paper-trading-history-all";
    const string TV_PT_POSITIONS_FILE_KEYWORD = "paper-trading-positions";

    const string TV_IBKR_HISTORY_FILE_KEYWORD = "interactive-brokers-trade-history";
    const string TV_IBKR_POSITIONS_FILE_KEYWORD = "interactive-brokers-positions";
    const string TV_IBKR_ORDERS_FILE_KEYWORD = "interactive-brokers-orders-all";
    
    // const string INTERACTIVE_BROKERS_ORDERS_FILE_KEYWORD = "TradingJournal_-_Orders";

    public bool OpenFiles(out TradingViewData tradingViewData, out InteractiveBrokersData interactiveBrokersData, string interactiveBrokersUserId, UIFeedback uiFeedback)
    {
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);
        tradingViewData = null;
        interactiveBrokersData = null;

        if (!Directory.Exists(folderPath))
        {
            uiFeedback.FailedConversion("Folder doesn't exist!", "The selected folder does NOT exist!");
            Debug.LogError("Folder doesn't exist!");

            return false;
        }

        var broker = (Broker)PlayerPrefs.GetInt(SAVED_BROKER_INDEX, 0);
        if (broker == Broker.TV_PaperTrading || broker == Broker.TV_IBKR)
        {
            var historyFileKeyword = broker == Broker.TV_PaperTrading ? TV_PT_HISTORY_FILE_KEYWORD : TV_IBKR_HISTORY_FILE_KEYWORD;
            var positionsFileKeyword = broker == Broker.TV_PaperTrading ? TV_PT_POSITIONS_FILE_KEYWORD : TV_IBKR_POSITIONS_FILE_KEYWORD;
            var ordersFileKeyword = TV_IBKR_ORDERS_FILE_KEYWORD;

            var historyFiles = new List<TVFileData>();
            var positionsFiles = new List<TVFileData>();
            var ordersFiles = new List<TVFileData>();

            DirectoryInfo directory = new DirectoryInfo(folderPath);
            var csvFiles = directory.GetFiles("*.csv");

            foreach (var file in csvFiles)
            {
                if (file.Name.Contains(historyFileKeyword))
                {
                    historyFiles.Add(new TVFileData(file.Name));
                }
                else if (file.Name.Contains(positionsFileKeyword))
                {
                    positionsFiles.Add(new TVFileData(file.Name));
                }
                else if (file.Name.Contains(ordersFileKeyword))
                {
                    ordersFiles.Add(new TVFileData(file.Name));
                }
            }

            var newestHistoryFile = historyFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
            var newestPositionsFile = positionsFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
            var newestOrdersFile = ordersFiles.OrderByDescending(entry => entry.time).FirstOrDefault();

            if (newestHistoryFile == null || newestPositionsFile == null || (newestOrdersFile == null && broker == Broker.TV_IBKR))
            {
                uiFeedback.FailedConversion("Missing Files!", "At least one of the required CSV files is missing in the selected folder.");
                Debug.LogError("At least one of the required CSV files is missing in the selected folder.");
            }

            var historyFileName = newestHistoryFile.name;
            var positionsFileName = newestPositionsFile.name;
            // Orders file is only necessary for IBKR:
            var orderFileName = newestOrdersFile != null && broker == Broker.TV_IBKR ? newestOrdersFile.name : string.Empty;

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

                return false;
            }
        }
        else if (broker == Broker.InteractiveBrokers)
        {
            var ibFileKeyword = interactiveBrokersUserId;
            // var ibOrdersKeyword = INTERACTIVE_BROKERS_ORDERS_FILE_KEYWORD;
            
            var ibFiles = new List<FileInfo>();
            // var ibOrdersFiles = new List<FileInfo>();

            DirectoryInfo directory = new DirectoryInfo(folderPath);
            var csvFiles = directory.GetFiles("*.csv");

            foreach (var file in csvFiles)
            {
                if (file.Name.Contains(ibFileKeyword))
                {
                    ibFiles.Add(file);
                }
                // else if (file.Name.Contains(ibOrdersKeyword))
                // {
                //     ibOrdersFiles.Add(file);
                // }
            }

            var newestIbFile = ibFiles.OrderByDescending(entry => entry.CreationTimeUtc).FirstOrDefault();
            // var newestIbOrdersFile = ibOrdersFiles.OrderByDescending(entry => entry.CreationTimeUtc).FirstOrDefault();
            
            if (newestIbFile == null /* || newestIbOrdersFile == null */)
            {
                uiFeedback.FailedConversion("Missing File!", "The required CSV file is missing in the selected folder.");
                Debug.LogError("The required CSV file is missing in the selected folder.");
            }

            var ibFileName = newestIbFile.Name;
            // var ibOrdersFileName = newestIbOrdersFile.Name;

            try
            {
                var ibCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + ibFileName);
                // var ibOrdersCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + ibOrdersFileName);
                
                const string TRADES_START = "Trades,Header";
                const string TRADES_END   = "Trades,Total";
                const string POSITIONS_START = "Open Positions,Header";
                const string POSITIONS_END = "Forex Balances,Header";
                
                var tradesStartIndex = ibCsv.IndexOf(TRADES_START, StringComparison.Ordinal);
                if (tradesStartIndex < 0)
                {
                    throw new InvalidOperationException("TRADES_START not found.");
                }

                var tradesEndIndex = ibCsv.IndexOf(TRADES_END, tradesStartIndex + TRADES_START.Length, StringComparison.Ordinal);
                if (tradesEndIndex < 0)
                {
                    tradesEndIndex = ibCsv.Length;
                }
                
                var positionsStartIndex = ibCsv.IndexOf(POSITIONS_START, StringComparison.Ordinal);
                var positionsEndIndex = ibCsv.IndexOf(POSITIONS_END, positionsStartIndex + POSITIONS_START.Length, StringComparison.Ordinal);
                if (positionsEndIndex < 0)
                {
                    positionsEndIndex = ibCsv.Length;
                }

                var ibTradesCsv = ibCsv.Substring(tradesStartIndex, tradesEndIndex - tradesStartIndex);
                var ibPositionsCsv = positionsStartIndex < 0 ? "" : ibCsv.Substring(positionsStartIndex, positionsEndIndex - positionsStartIndex);
                
                interactiveBrokersData = new InteractiveBrokersData
                {
                    trades = CsvReader.Read(ibTradesCsv),
                    positions = CsvReader.Read(ibPositionsCsv),
                    // orders = CsvReader.Read(ibOrdersCsv),
                    
                    ibFileName = ibFileName,
                    // ibOrdersFileName = ibOrdersFileName
                };
            }
            catch
            {
                uiFeedback.FailedConversion("Can't access files!", "The CSV files need to be closed!");
                Debug.LogError("The CSV files need to be closed!");

                return false;
            }
        }

        return true;
    }

    class TVFileData
    {
        public string name;
        public DateTime time;

        public TVFileData(string name)
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
    
    public class InteractiveBrokersData
    {
        public List<Dictionary<string, string>> trades;
        public List<Dictionary<string, string>> positions;
        // public List<Dictionary<string, string>> orders;

        public string ibFileName;
        // public string ibOrdersFileName;
    }
}