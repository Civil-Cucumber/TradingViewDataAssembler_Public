using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using static FileManager;

public class TradingViewDataAssembler : MonoBehaviour
{
    public UIFeedback uiFeedback;

    enum Side
    {
        Long,
        Short
    }

    enum OrderType
    {
        Market,
        Limit,
        Stop,
        TakeProfit,
        StopLoss
    }

    enum OrderStatus
    {
        Filled,
        Cancelled,
        Rejected
    }

    #region Trades
    class Trade
    {
        public string symbol;
        public Side side;
        public List<Order> entries = new List<Order>();
        public List<Order> stopLosses = new List<Order>();
        public List<Order> priceTargets = new List<Order>();
        public List<Order> exits = new List<Order>();

        public bool FirstBuyIn => entries.Count == 0 && exits.Count == 0 && stopLosses.Count == 0 && priceTargets.Count == 0;
        public bool TradeCompleted => entries.Count > 0 && exits.Count > 0 && TotalEntryAmount == TotalExitAmount;
        public DateTime StartTradeTime => entries.OrderBy(x => x.time).Select(x => x.time).First();
        public DateTime EndTradeTime => exits.Count > 0 ? exits.OrderByDescending(x => x.time).Select(x => x.time).First() : DateTime.MinValue;

        public float AvgEntryPrice
        {
            get
            {
                var average = 0f;
                foreach (var entry in entries)
                {
                    average += entry.price * entry.amount;
                }
                return TotalEntryAmount > 0 ? average / TotalEntryAmount : 0;
            }
        }
        public float AvgExitPrice
        {
            get
            {
                var average = 0f;
                foreach (var exit in exits)
                {
                    average += exit.price * exit.amount;
                }
                return TotalExitAmount > 0 ? average / TotalExitAmount : 0;
            }
        }
        public float LastStopLoss => stopLosses.OrderByDescending(x => x.time).FirstOrDefault().price;
        public float LastPriceTarget => priceTargets.OrderByDescending(x => x.time).FirstOrDefault().price;

        public float TotalEntryAmount => entries.Sum(x => x.amount);
        public float TotalExitAmount => exits.Sum(x => x.amount);
    }

    struct Order
    {
        public DateTime time;
        public float price;
        public float amount;
        public int orderId;
    }

    public void AssembleData(TradingViewData tradingViewData)
    {
        var floatCulture = new CultureInfo("en-US");
        var historyEntries = GetHistoryEntries(floatCulture, tradingViewData.history);
        var positionsEntries = GetPositionsEntries(floatCulture, tradingViewData.positions);

        var sb = new StringBuilder();
        var trades = new List<Trade>();

        var firstHistoryEntryTime = historyEntries.OrderBy(entry => entry.placingTime).FirstOrDefault().placingTime;

        foreach (var historyEntry in historyEntries)
        {
            // look to which trade entry belongs:
            var currentTrade = trades
                .FirstOrDefault(trade => !trade.TradeCompleted && trade.symbol == historyEntry.symbol);

            // if none found, start a new trade entry:
            if (currentTrade == null)
            {
                currentTrade = new Trade
                {
                    symbol = historyEntry.symbol
                };
                trades.Add(currentTrade);
            }

            // Stop Loss:
            if (historyEntry.type == OrderType.StopLoss)
            {
                currentTrade.side = GetInvertedSide(historyEntry.side);
                currentTrade.stopLosses.Add(historyEntry.GetOrder());
                if (historyEntry.status == OrderStatus.Filled)
                {
                    var exitOrder = historyEntry.GetOrder();
                    currentTrade.exits.Add(exitOrder);
                }
            }
            // Price Target:
            else if (historyEntry.type == OrderType.TakeProfit)
            {
                currentTrade.side = GetInvertedSide(historyEntry.side);
                currentTrade.priceTargets.Add(historyEntry.GetOrder());
                if (historyEntry.status == OrderStatus.Filled)
                {
                    var exitOrder = historyEntry.GetOrder();
                    currentTrade.exits.Add(exitOrder);
                }
            }
            // First buy in:
            else if (currentTrade.FirstBuyIn)
            {
                if (historyEntry.status == OrderStatus.Filled)
                {
                    currentTrade.side = historyEntry.side;
                    var entryOrder = historyEntry.GetOrder();
                    currentTrade.entries.Add(entryOrder);
                }
            }
            // Increase position:
            else if (currentTrade.side == historyEntry.side)
            {
                if (historyEntry.status == OrderStatus.Filled)
                {
                    var entryOrder = historyEntry.GetOrder();
                    currentTrade.entries.Add(entryOrder);

                    // "Reset" trade if first entry added, but has already exit position from before entry (= older trade where entry position info is now missing):
                    if (currentTrade.entries.Count == 1 && currentTrade.exits.Count > 0)
                    {
                        currentTrade.exits.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                        currentTrade.priceTargets.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                        currentTrade.stopLosses.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                    }
                }
            }
            // Decrease / Exit position:
            else
            {
                if (historyEntry.status == OrderStatus.Filled)
                {
                    var exitOrder = historyEntry.GetOrder();
                    currentTrade.exits.Add(exitOrder);
                }
            }
        }


        trades = trades
            .Where(entry => entry.entries.Count > 0)
            .OrderByDescending(entry => entry.StartTradeTime)
            .ToList();

        // Add price targets and stop losses for active trades:
        var remainingPositionsEntries = new List<PositionsEntry>(positionsEntries);
        foreach (var trade in trades)
        {
            if (!trade.TradeCompleted)
            {
                var activePosition = remainingPositionsEntries
                    .FirstOrDefault(entry => entry.symbol == trade.symbol);

                if (activePosition != null)
                {
                    if (activePosition.stopLoss > 0)
                    {
                        var stopLossOrder = new Order
                        {
                            amount = activePosition.amount,
                            price = activePosition.stopLoss
                        };
                        trade.stopLosses.Add(stopLossOrder);
                    }
                    if (activePosition.priceTarget > 0)
                    {
                        var priceTargetOrder = new Order
                        {
                            amount = activePosition.amount,
                            price = activePosition.priceTarget
                        };
                        trade.priceTargets.Add(priceTargetOrder);
                    }
                    remainingPositionsEntries.Remove(activePosition);
                }
            }
        }

        // sort by entry time:
        trades = trades
            .Where(entry => entry.exits.Count == 0 || entry.exits.Min(x => x.time) >= firstHistoryEntryTime)
            .OrderByDescending(entry => entry.StartTradeTime)
            .ToList();

        //sb.AppendLine("Symbol, Side, First Entry, Avg Price, Amount, Stop Loss, Target, Last Exit, Avg Price, Amount, Entries, Exits");
        foreach (var trade in trades)
        {
            // don't have endTradeTime before there was an exit:
            var exitTradeTime = trade.EndTradeTime;
            var exitTradeString = exitTradeTime.ToString();
            if (exitTradeTime == DateTime.MinValue)
            {
                exitTradeString = "";
            }
            sb.AppendLine($"{trade.symbol},{trade.side},{trade.StartTradeTime},{CapDecimalPlaces(trade.AvgEntryPrice, floatCulture)},{FloatToString(trade.TotalEntryAmount, floatCulture)},{CapDecimalPlaces(trade.LastStopLoss, floatCulture)},{CapDecimalPlaces(trade.LastPriceTarget, floatCulture)},{exitTradeString},{CapDecimalPlaces(trade.AvgExitPrice, floatCulture)},{FloatToString(trade.TotalExitAmount, floatCulture)},{trade.entries.Count},{trade.exits.Count}");
        }
        Debug.Log(sb);

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Copied to clipboard!");

        uiFeedback.FinishedConversion(tradingViewData, sb.ToString());
    }
    #endregion

    #region History
    class HistoryEntry
    {
        public string symbol;
        public Side side;
        public OrderType type;
        public float amount;
        public float price;
        public OrderStatus status;
        public DateTime placingTime;
        public DateTime closingTime;
        public int orderId;

        public Order GetOrder()
        {
            return new Order()
            {
                amount = amount,
                orderId = orderId,
                price = price,
                time = closingTime
            };
        }
    }

    List<HistoryEntry> GetHistoryEntries(CultureInfo floatCulture, List<Dictionary<string, string>> history)
    {
        var sb = new StringBuilder();
        var historyEntries = new List<HistoryEntry>();

        foreach (var line in history)
        {
            var status = Enum.Parse<OrderStatus>(line["Status"]);
            if (status == OrderStatus.Rejected)
            {
                continue;
            }

            // Symbol:
            var symbol = line["Symbol"];
            var symbolStartIndex = symbol.LastIndexOf(':') + 1;
            symbol = symbol.Substring(symbolStartIndex, symbol.Length - symbolStartIndex);

            // Side:
            var side = line["Side"] == "Buy" ? Side.Long : Side.Short;

            // Type:
            var typeString = line["Type"].Replace(" ", "");
            var type = Enum.Parse<OrderType>(typeString);

            // Amount:
            var qty = line["Qty"];
            qty = qty.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var amount = float.Parse(qty, floatCulture);

            // Price:
            var fillPriceString = line["Fill Price"];
            fillPriceString = fillPriceString.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var priceString = line["Price"];
            priceString = priceString.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var price = fillPriceString == string.Empty ? float.Parse(priceString, floatCulture) : float.Parse(fillPriceString, floatCulture);
            // necessary since some prices have more than 2 digits:
            price = Mathf.Round(price * 100f) / 100f;

            // Placing Time:
            var placingTime = DateTime.Parse(line["Placing Time"]);

            // Closing Time:
            var closingTime = DateTime.Parse(line["Closing Time"]);

            // Order Id:
            var orderId = int.Parse(line["Order id"]);

            var historyEntry = new HistoryEntry
            {
                symbol = symbol,
                side = side,
                type = type,
                amount = amount,
                price = price,
                status = status,
                placingTime = placingTime,
                closingTime = closingTime,
                orderId = orderId
            };
            historyEntries.Add(historyEntry);
        }

        historyEntries = historyEntries.OrderBy(entry => entry.placingTime).ToList();

        sb.AppendLine("Symbol, Side, Type, Amount, Price, Status, Time, Order Id");
        foreach (HistoryEntry historyEntry in historyEntries)
        {
            sb.AppendLine($"{historyEntry.symbol},{historyEntry.side},{historyEntry.type},{historyEntry.amount},{historyEntry.price},{historyEntry.status},{historyEntry.placingTime},{historyEntry.closingTime},{historyEntry.orderId}");
        }
        Debug.Log(sb);

        return historyEntries;
    }
    #endregion   

    #region Positions
    class PositionsEntry
    {
        public string symbol;
        public Side side;
        public float avgFillPrice;
        public float priceTarget;
        public float stopLoss;
        public float amount;
    }

    List<PositionsEntry> GetPositionsEntries(CultureInfo floatCulture, List<Dictionary<string, string>> positions)
    {
        var sb = new StringBuilder();
        var positionsEntries = new List<PositionsEntry>();

        foreach (var line in positions)
        {
            // Symbol:
            var symbol = line["Symbol"];
            var symbolStartIndex = symbol.LastIndexOf(':') + 1;
            symbol = symbol.Substring(symbolStartIndex, symbol.Length - symbolStartIndex);

            // Side:
            var side = line["Side"] == "Long" ? Side.Long : Side.Short;

            // Avg Fill price:
            var entryPrice = float.Parse(line["Avg Fill Price"], floatCulture);

            // Price target:
            var hasPriceTarget = float.TryParse(line["Take Profit"], NumberStyles.Float, floatCulture, out var priceTarget);

            // Stop loss:
            var hasStopLoss = float.TryParse(line["Stop Loss"], NumberStyles.Float, floatCulture, out var stopLoss);

            // Amount:
            var amount = float.Parse(line["Qty"], floatCulture);

            var positionsEntry = new PositionsEntry
            {
                symbol = symbol,
                side = side,
                avgFillPrice = entryPrice,
                priceTarget = hasPriceTarget ? priceTarget : 0,
                stopLoss = hasStopLoss ? stopLoss : 0,
                amount = amount
            };

            positionsEntries.Add(positionsEntry);
        }

        sb.AppendLine("Symbol, Side, Entry, Price Target, Stop Loss, Amount");
        foreach (var positionsEntry in positionsEntries)
        {
            sb.AppendLine($"{positionsEntry.symbol},{positionsEntry.side},{CapDecimalPlaces(positionsEntry.avgFillPrice, floatCulture)},{CapDecimalPlaces(positionsEntry.priceTarget, floatCulture)},{CapDecimalPlaces(positionsEntry.stopLoss, floatCulture)},{positionsEntry.amount}");
        }
        Debug.Log(sb);

        return positionsEntries;
    }
    #endregion

    #region Helping Functions
    Side GetInvertedSide(Side side)
    {
        return side == Side.Long ? Side.Short : Side.Long;
    }

    string CapDecimalPlaces(float value, CultureInfo floatCulture)
    {
        return value == 0f ? "" : Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", floatCulture);
    }

    string FloatToString(float value, CultureInfo floatCulture)
    {
        return value.ToString(floatCulture);
    }
    #endregion
}