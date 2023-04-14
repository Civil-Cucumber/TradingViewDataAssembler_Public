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
        var remainingTradingJournalEntries = GetTradingJournalEntries(floatCulture, tradingViewData.tradingJournal);
        var positionsEntries = GetPositionsEntries(floatCulture, tradingViewData.positions);

        var sb = new StringBuilder();
        var trades = new List<Trade>();

        var firstTradingJournalEntryTime = remainingTradingJournalEntries.OrderBy(entry => entry.time).FirstOrDefault().time;
        var firstHistoryEntryTime = historyEntries.OrderBy(entry => entry.time).FirstOrDefault().time;

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
                    var exitOrder = GetTradingJournalEntry(historyEntry, remainingTradingJournalEntries, firstTradingJournalEntryTime, "");
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
                    var exitOrder = GetTradingJournalEntry(historyEntry, remainingTradingJournalEntries, firstTradingJournalEntryTime, "");
                    currentTrade.exits.Add(exitOrder);
                }
            }
            // First buy in:
            else if (currentTrade.FirstBuyIn)
            {
                if (historyEntry.status == OrderStatus.Filled)
                {
                    currentTrade.side = historyEntry.side;
                    var entryOrder = GetTradingJournalEntry(historyEntry, remainingTradingJournalEntries, firstTradingJournalEntryTime, "(FIRST BUY-IN)");
                    currentTrade.entries.Add(entryOrder);
                }
            }
            // Increase position:
            else if (currentTrade.side == historyEntry.side)
            {
                if (historyEntry.status == OrderStatus.Filled)
                {
                    var entryOrder = GetTradingJournalEntry(historyEntry, remainingTradingJournalEntries, firstTradingJournalEntryTime, "(INCREASE POS)");
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
                    var exitOrder = GetTradingJournalEntry(historyEntry, remainingTradingJournalEntries, firstTradingJournalEntryTime, "(DECREASE POS)");
                    currentTrade.exits.Add(exitOrder);
                }
            }
        }

        trades = trades.Where(entry => entry.entries.Count > 0).OrderByDescending(entry => entry.StartTradeTime).ToList();

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

        // Something went wrong if there are trading journal entries left:
        if (remainingTradingJournalEntries.Count > 0)
        {
            var tradesBeforeFirstHistoryEntry = remainingTradingJournalEntries.Count(x => x.time < firstHistoryEntryTime);
            var tradesAfterFirstHistoryEntry = remainingTradingJournalEntries.Count - tradesBeforeFirstHistoryEntry;
            if (tradesAfterFirstHistoryEntry > 0)
            {
                Debug.LogWarning($"There are {tradesAfterFirstHistoryEntry} remaining trading journal entries where no trade was found for - AFTER first history entry!!!\nProbably because TradingView sometimes simply doesn't show some trades from a few days ago in History (and in TradingJournal as well) anymore for some reason!");
            }
            if (tradesBeforeFirstHistoryEntry > 0)
            {
                Debug.Log($"There are {tradesBeforeFirstHistoryEntry} remaining trading journal entries where no trade was found for - BEFORE first history entry, so that's probably why.");
            }
        }

        // filter out trades that were sold before trading journal begins and sort remaining by entry time:
        trades = trades.Where(entry => entry.exits.Count == 0 || entry.exits.Min(x => x.time) >= firstTradingJournalEntryTime)
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
        public DateTime time;
        public int orderId;

        public Order GetOrder()
        {
            return new Order()
            {
                amount = amount,
                orderId = orderId,
                price = price,
                time = time
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

            // Time:
            var time = DateTime.Parse(line["Time"]);

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
                time = time,
                orderId = orderId
            };
            historyEntries.Add(historyEntry);
        }

        historyEntries = historyEntries.OrderBy(entry => entry.time).ToList();

        sb.AppendLine("Symbol, Side, Type, Amount, Price, Status, Time, Order Id");
        foreach (HistoryEntry historyEntry in historyEntries)
        {
            sb.AppendLine($"{historyEntry.symbol},{historyEntry.side},{historyEntry.type},{historyEntry.amount},{historyEntry.price},{historyEntry.status},{historyEntry.time},{historyEntry.orderId}");
        }
        Debug.Log(sb);

        return historyEntries;
    }
    #endregion

    #region Trading Journal
    class TradingJournalEntry
    {
        public int orderId;
        public string symbol;
        public DateTime time;
        public float price;
        public float amount;

        public Order GetOrder()
        {
            return new Order()
            {
                amount = amount,
                orderId = orderId,
                price = price,
                time = time
            };
        }
    }

    List<TradingJournalEntry> GetTradingJournalEntries(CultureInfo floatCulture, List<Dictionary<string, string>> tradingJournal)
    {
        var sb = new StringBuilder();
        var tradingJournalEntries = new List<TradingJournalEntry>();

        foreach (var line in tradingJournal)
        {
            var text = line["Text"];
            if (!text.Contains("has been executed at price"))
            {
                continue;
            }

            // Order Id:
            var orderIdStartIndex = text.IndexOf("Order ") + "Order ".Length;
            var orderIdEndIndex = text.IndexOf(' ', orderIdStartIndex);
            var orderIdString = text.Substring(orderIdStartIndex, orderIdEndIndex - orderIdStartIndex);
            var orderId = int.Parse(orderIdString);

            // Symbol:
            var symbolStartIndex = text.IndexOf(':') + 1;
            var symbolEndIndex = text.IndexOf(' ', symbolStartIndex);
            var symbol = text.Substring(symbolStartIndex, symbolEndIndex - symbolStartIndex);

            // Time:
            var time = DateTime.Parse(line["Time"]);

            // Price:
            var priceStartIndex = text.IndexOf("price ") + "price ".Length;
            var priceEndIndex = text.IndexOf(' ', priceStartIndex);
            var priceString = text.Substring(priceStartIndex, priceEndIndex - priceStartIndex);
            var price = float.Parse(priceString, floatCulture);

            // Amount:
            var amountStartIndex = text.IndexOf("for ", priceEndIndex) + "for ".Length;
            var amountEndIndex = text.IndexOf(' ', amountStartIndex);
            var amountString = text.Substring(amountStartIndex, amountEndIndex - amountStartIndex);
            var amount = float.Parse(amountString, floatCulture);

            var tradingJournalEntry = new TradingJournalEntry
            {
                orderId = orderId,
                symbol = symbol,
                time = time,
                price = price,
                amount = amount
            };

            tradingJournalEntries.Add(tradingJournalEntry);
        }

        tradingJournalEntries = tradingJournalEntries.OrderBy(entry => entry.time).ToList();

        sb.AppendLine("Order Id, Symbol, Time, Price, Amount");
        foreach (var tradingJournalEntry in tradingJournalEntries)
        {
            sb.AppendLine($"{tradingJournalEntry.orderId},{tradingJournalEntry.symbol},{tradingJournalEntry.time},{CapDecimalPlaces(tradingJournalEntry.price, floatCulture)},{tradingJournalEntry.amount}");
        }
        Debug.Log(sb);

        return tradingJournalEntries;
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
    Order GetTradingJournalEntry(HistoryEntry historyEntry, List<TradingJournalEntry> remainingTradingJournalEntries, DateTime firstTradingJournalEntryTime, string additionalMissingEntryWarningTypeInfo)
    {
        var tradingJournalEntry = remainingTradingJournalEntries.FirstOrDefault(x => x.orderId == historyEntry.orderId);
        // temporary order data for trades outside of trading journal - less accurate data, but so trade has all exits and entries and correctly marked as "completed" then:
        Order order = historyEntry.GetOrder();
        if (tradingJournalEntry != null)
        {
            order = tradingJournalEntry.GetOrder();
            remainingTradingJournalEntries.Remove(tradingJournalEntry);
        }
        else
        {
            if (historyEntry.time >= firstTradingJournalEntryTime)
            {
                Debug.LogWarning($"No {historyEntry.type} {additionalMissingEntryWarningTypeInfo} trading journal entry could be found for {historyEntry.symbol} from {historyEntry.time} (after TradingJournal starts!!!)\nProbably because TradingView sometimes simply doesn't show some trades from a few days ago in TradingJournal anymore for some reason!");
            }
        }

        return order;
    }

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