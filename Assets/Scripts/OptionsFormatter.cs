using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class OptionsFormatter
{
    public static string FormatOption(string raw)
    {
        if (raw.Contains("Call") || raw.Contains("Put") || !raw.Any(char.IsDigit))
        {
            return raw;
        }

        string ticker;
        string strike;
        string origType;
        DateTime expiry;
        
        var parts = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        // 1. IB Format: "CIEN 07NOV25 195 C"
        if (parts.Length >= 4)
        {
            ticker      = parts[0];   // "CIEN"
            var expText = parts[1];   // "07NOV25"
            strike      = parts[2];   // "195"
            origType    = parts[3];   // "C" or "P"

            if (!DateTime.TryParseExact(expText, "ddMMMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out expiry))
            {
                throw new Exception($"FormatOption: Couldn't parse date \"{expText}\" (IB Format) in \"{raw}\"!");
            }
        }
        // 2. TV PaperTrading Format: "KVUE251010P19.5"
        else
        {
            var regMatch = Regex.Match(raw, @"^([A-Za-z]+)(\d{6})([CP])(.+)$");
            if (!regMatch.Success)
            {
                Debug.LogWarning($"FormatOption: Invalid Format: \"{raw}\"");
                return raw;
            }

            ticker = regMatch.Groups[1].Value;   // "CIEN"
            var expText = regMatch.Groups[2].Value;   // "251107"
            origType = regMatch.Groups[3].Value;   // "C" or "P"
            strike = regMatch.Groups[4].Value; // "195"

            if (!DateTime.TryParseExact(expText, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out expiry))
            {
                throw new Exception($"FormatOption: Couldn't parse date \"{expText}\" (TV PaperTrading Format) in \"{raw}\"!");
            }
            
            if (strike.EndsWith(".0"))
            {
                strike = strike.Substring(0, strike.Length - 2);
            }
        }
        
        var month = expiry.ToString("MMM", CultureInfo.InvariantCulture); // "Nov"
        var day   = expiry.ToString("dd",  CultureInfo.InvariantCulture); // "07"
        var year  = expiry.ToString("yy",  CultureInfo.InvariantCulture); // "25"

        string type;
        switch (origType.ToUpperInvariant())
        {
            case "C":
                type = "Call";
                break;
            case "P":
                type = "Put";
                break;
            default:
                throw new Exception("Wrong Call / Put Format!");
        }

        // Aligned format: "CIEN Nov07 '25 195 Call"
        var result = $"{ticker} {month}{day} '{year} {strike} {type}";
        
        return result;
    }
}