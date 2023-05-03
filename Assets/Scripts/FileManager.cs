using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class FileManager : MonoBehaviour
{
    public const string SAVED_FOLDER_KEY = "folderPath";

    const string HISTORY_FILE_KEYWORD = "history-all";
    const string POSITIONS_FILE_KEYWORD = "positions";

    public bool OpenFiles(out TradingViewData tradingViewData)
    {
        var folderPath = PlayerPrefs.GetString(SAVED_FOLDER_KEY);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Folder doesn't exist!");
            tradingViewData = null;

            return false;
        }

        var historyFiles = new List<FileData>();
        var positionsFiles = new List<FileData>();

        DirectoryInfo directory = new DirectoryInfo(folderPath);
        var csvFiles = directory.GetFiles("*.csv");
        foreach (var file in csvFiles)
        {
            if (file.Name.Contains(HISTORY_FILE_KEYWORD))
            {
                historyFiles.Add(new FileData(file.Name));
            }
            else if (file.Name.Contains(POSITIONS_FILE_KEYWORD))
            {
                positionsFiles.Add(new FileData(file.Name));
            }
        }

        var newestHistoryFile = historyFiles.OrderByDescending(entry => entry.time).FirstOrDefault();
        var newestPositionsFile = positionsFiles.OrderByDescending(entry => entry.time).FirstOrDefault();

        var historyFileName = newestHistoryFile.name;
        var positionsFileName = newestPositionsFile.name;

        try
        {
            var historyCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + historyFileName);
            var positionsCsv = File.ReadAllText(folderPath + Path.DirectorySeparatorChar + positionsFileName);

            tradingViewData = new TradingViewData
            {
                history = CsvReader.Read(historyCsv),
                positions = CsvReader.Read(positionsCsv),

                historyFileName = historyFileName,
                positionsFileName = positionsFileName
            };
        }
        catch
        {
            Debug.LogError("Close the CSV files!");
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
            var timeEndIndex = name.IndexOf('.', timeStartIndex);

            var dateString = name.Substring(dateStartIndex, dateEndIndex - dateStartIndex);
            var dateValues = dateString.Split('-');
            dateString = $"{dateValues[2]}.{dateValues[1]}.{dateValues[0]}";

            var timeString = name.Substring(timeStartIndex, timeEndIndex - timeStartIndex);
            var timeValues = timeString.Split('_');
            timeString = $"{timeValues[0]}:{timeValues[1]}:{timeValues[2]}";

            var dateTimeString = $"{dateString} {timeString}";
            var dateTimeCulture = new CultureInfo("de-DE");
            return DateTime.Parse(dateTimeString, dateTimeCulture);
        }
    }

    public class TradingViewData
    {
        public List<Dictionary<string, string>> history;
        public List<Dictionary<string, string>> positions;

        public string historyFileName;
        public string positionsFileName;
    }
}