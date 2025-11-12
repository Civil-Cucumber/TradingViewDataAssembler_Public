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
        StopLoss,
        IBKR
    }

    enum OrderStatus
    {
        Working,
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

        public float TotalCommissions
        {
            get { return entries.Sum(x => x.commission) + exits.Sum(x => x.commission); }
        }
        
        public string Adds
        {
            get
            {
                var adds = string.Empty;
                for (int i = 1; i < entries.Count; i++)
                {
                    if (adds != string.Empty)
                    {
                        adds += "#";
                    }

                    adds += entries[i].time;
                }
                return adds;
            }
        }
        
        public string PartialCloses
        {
            get
            {
                var partialCloses = string.Empty;
                for (int i = 0; i < exits.Count - 1; i++)
                {
                    if (partialCloses != string.Empty)
                    {
                        partialCloses += "#";
                    }

                    partialCloses += exits[i].time;
                }
                return partialCloses;
            }
        }
    }

    struct Order
    {
        public DateTime time;
        public float price;
        public float amount;
        public uint orderId;
        public float commission;
    }

    public void AssembleData(TradingViewData tradingViewData, InteractiveBrokersData interactiveBrokersData)
    {
        var broker = (Broker)PlayerPrefs.GetInt(SAVED_BROKER_INDEX);
        var floatCulture = new CultureInfo("en-US");

        var sb = new StringBuilder();
        var trades = new List<Trade>();
        var firstEntryTime = DateTime.MinValue;

        if (broker == Broker.TV_PaperTrading || broker == Broker.TV_IBKR_Paper || broker == Broker.TV_IBKR)
        {
            var historyEntries = GetHistoryEntries(floatCulture, tradingViewData.history, broker);
            var positionsEntries = GetPositionsEntries(floatCulture, tradingViewData.positions, broker);
            var orderEntries = GetOrderEntries(floatCulture, tradingViewData.orders, broker);

            if (historyEntries.Count > 0)
            {
                firstEntryTime = historyEntries.OrderBy(entry => entry.placingTime).FirstOrDefault().placingTime;
            }

            foreach (var historyEntry in historyEntries)
            {
                // look to which trade entry belongs:
                var currentTrade = trades.FirstOrDefault(trade => !trade.TradeCompleted && trade.symbol == historyEntry.symbol);

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

                        // Stop Loss + Price Target for IBKR:
                        // TODO: filter cancelled / filled / working somehow? On the other hand then as soon as trade completed PT and SL wouldn't be available anymore...
                        var relevantEntries = orderEntries.Where(entry => entry.symbol == historyEntry.symbol && entry.time >= historyEntry.placingTime).ToList();
                        var priceTargets = relevantEntries.Where(entry => entry.side != historyEntry.side).ToList();
                        foreach (var pt in priceTargets)
                        {
                            var ptOrder = new Order
                            {
                                amount = pt.amount,
                                price = pt.limitPrice,
                                time = pt.time,
                            };
                            currentTrade.priceTargets.Add(ptOrder);
                        }

                        var stopLosses = relevantEntries.Where(entry => entry.side == historyEntry.side).ToList();
                        foreach (var sl in stopLosses)
                        {
                            var slOrder = new Order
                            {
                                amount = sl.amount,
                                price = sl.limitPrice,
                                time = sl.time,
                            };
                            currentTrade.stopLosses.Add(slOrder);
                        }
                    }
                }
                // Increase position:
                else if (currentTrade.side == historyEntry.side)
                {
                    if (historyEntry.status == OrderStatus.Filled || broker != Broker.TV_PaperTrading)
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
                    if (historyEntry.status == OrderStatus.Filled || broker != Broker.TV_PaperTrading)
                    {
                        var exitOrder = historyEntry.GetOrder();
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
                    var activePosition = remainingPositionsEntries.FirstOrDefault(entry => entry.symbol == trade.symbol);

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
        }
        else if (broker is Broker.IB_Paper or Broker.IB_Live)
        {
            var ibTradesEntries = GetIBTradesEntries(floatCulture, interactiveBrokersData.trades);
            // var ibOrdersEntries = GetIBOrderEntries(floatCulture, interactiveBrokersData.trades);
            var ibPositionsEntries = GetIBPositionsEntries(floatCulture, interactiveBrokersData.positions);
            //
            if (ibTradesEntries.Count > 0)
            {
                firstEntryTime = ibTradesEntries.OrderBy(entry => entry.closingTime).FirstOrDefault().closingTime;
            }

            foreach (var listEntry in ibTradesEntries)
            {
                // look to which trade entry belongs:
                var currentTrade = trades.FirstOrDefault(trade => !trade.TradeCompleted && trade.symbol == listEntry.symbol);
            
                // if none found, start a new trade entry:
                if (currentTrade == null)
                {
                    currentTrade = new Trade
                    {
                        symbol = listEntry.symbol
                    };
                    trades.Add(currentTrade);
                }
            
            //     // Stop Loss:
            //     if (historyEntry.type == OrderType.StopLoss)
            //     {
            //         currentTrade.side = GetInvertedSide(historyEntry.side);
            //         currentTrade.stopLosses.Add(historyEntry.GetOrder());
            //         if (historyEntry.status == OrderStatus.Filled)
            //         {
            //             var exitOrder = historyEntry.GetOrder();
            //             currentTrade.exits.Add(exitOrder);
            //         }
            //     }
            //     // Price Target:
            //     else if (historyEntry.type == OrderType.TakeProfit)
            //     {
            //         currentTrade.side = GetInvertedSide(historyEntry.side);
            //         currentTrade.priceTargets.Add(historyEntry.GetOrder());
            //         if (historyEntry.status == OrderStatus.Filled)
            //         {
            //             var exitOrder = historyEntry.GetOrder();
            //             currentTrade.exits.Add(exitOrder);
            //         }
            //     }
            //     // First buy in:
            //     else if (currentTrade.FirstBuyIn)...
                    if (currentTrade.FirstBuyIn)
                    {
                        currentTrade.side = listEntry.side;
                        var entryOrder = listEntry.GetOrder();
                        currentTrade.entries.Add(entryOrder);
                
                        // Stop Loss + Price Target:
                        // TODO: filter cancelled / filled / working somehow? On the other hand then as soon as trade completed PT and SL wouldn't be available anymore...
                        // var relevantEntries = orderEntries.Where(entry => entry.symbol == listEntry.symbol && entry.time >= listEntry.closingTime).ToList();
                        // var priceTargets = relevantEntries.Where(entry => entry.side != listEntry.side).ToList();
                        // foreach (var pt in priceTargets)
                        // {
                        //     var ptOrder = new Order
                        //     {
                        //         amount = pt.amount,
                        //         price = pt.limitPrice,
                        //         time = pt.time,
                        //     };
                        //     currentTrade.priceTargets.Add(ptOrder);
                        // }
                
                        // var stopLosses = relevantEntries.Where(entry => entry.side == historyEntry.side).ToList();
                        // foreach (var sl in stopLosses)
                        // {
                        //     var slOrder = new Order
                        //     {
                        //         amount = sl.amount,
                        //         price = sl.limitPrice,
                        //         time = sl.time,
                        //     };
                        //     currentTrade.stopLosses.Add(slOrder);
                        // }
                    }
                    // Increase position:
                    else if (currentTrade.side == listEntry.side)
                    {
                        var entryOrder = listEntry.GetOrder();
                        currentTrade.entries.Add(entryOrder);
                
                        // "Reset" trade if first entry added, but has already exit position from before entry (= older trade where entry position info is now missing):
                        if (currentTrade.entries.Count == 1 && currentTrade.exits.Count > 0)
                        {
                            currentTrade.exits.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                            currentTrade.priceTargets.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                            currentTrade.stopLosses.RemoveAll(x => x.time < currentTrade.StartTradeTime);
                        }
                    }
                    // Decrease / Exit position:
                    else
                    {
                        var exitOrder = listEntry.GetOrder();
                        currentTrade.exits.Add(exitOrder);
                    }
                }
            
            
            trades = trades.Where(entry => entry.entries.Count > 0).OrderByDescending(entry => entry.StartTradeTime).ToList();
            
            // // Add price targets and stop losses for active trades:
            // var remainingPositionsEntries = new List<PositionsEntry>(positionsEntries);
            // foreach (var trade in trades)
            // {
            //     if (!trade.TradeCompleted)
            //     {
            //         var activePosition = remainingPositionsEntries.FirstOrDefault(entry => entry.symbol == trade.symbol);
            //
            //         if (activePosition != null)
            //         {
            //             if (activePosition.stopLoss > 0)
            //             {
            //                 var stopLossOrder = new Order
            //                 {
            //                     amount = activePosition.amount,
            //                     price = activePosition.stopLoss
            //                 };
            //                 trade.stopLosses.Add(stopLossOrder);
            //             }
            //
            //             if (activePosition.priceTarget > 0)
            //             {
            //                 var priceTargetOrder = new Order
            //                 {
            //                     amount = activePosition.amount,
            //                     price = activePosition.priceTarget
            //                 };
            //                 trade.priceTargets.Add(priceTargetOrder);
            //             }
            //
            //             remainingPositionsEntries.Remove(activePosition);
            //         }
            //     }
            // }
        }

        // sort by entry time:
        trades = trades
            .Where(entry => entry.exits.Count == 0 || entry.exits.Min(x => x.time) >= firstEntryTime)
            .OrderByDescending(entry => entry.StartTradeTime)
            .ToList();

        // TODO: improve! (override system culture):
        System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
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
            sb.AppendLine($"{trade.symbol},{trade.side},{trade.StartTradeTime},{CapDecimalPlaces(trade.AvgEntryPrice, floatCulture)},{FloatToString(trade.TotalEntryAmount, floatCulture)},{CapDecimalPlaces(trade.LastStopLoss, floatCulture)},{CapDecimalPlaces(trade.LastPriceTarget, floatCulture)},{exitTradeString},{CapDecimalPlaces(trade.AvgExitPrice, floatCulture)},{FloatToString(trade.TotalExitAmount, floatCulture)},{trade.entries.Count},{trade.Adds},{trade.exits.Count},{trade.PartialCloses},{CapDecimalPlaces(trade.TotalCommissions, floatCulture)}");
        }
        Debug.Log(sb);

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Copied to clipboard!");

        uiFeedback.FinishedConversion(tradingViewData, interactiveBrokersData, sb.ToString());
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
        public uint orderId;
        public float commission;

        public Order GetOrder()
        {
            return new Order()
            {
                amount = amount,
                orderId = orderId,
                price = price,
                time = closingTime,
                commission = commission,
            };
        }
    }

    List<HistoryEntry> GetHistoryEntries(CultureInfo floatCulture, List<Dictionary<string, string>> history, Broker broker)
    {
        var sb = new StringBuilder();
        var historyEntries = new List<HistoryEntry>();

        foreach (var line in history)
        {
            var status = broker == Broker.TV_PaperTrading ? Enum.Parse<OrderStatus>(line["Status"]) : OrderStatus.Filled;
            if (status == OrderStatus.Rejected)
            {
                continue;
            }

            // Symbol:
            var symbol = line["Symbol"];
            var symbolStartIndex = symbol.LastIndexOf(':') + 1;
            symbol = symbol.Substring(symbolStartIndex, symbol.Length - symbolStartIndex);

            // Ignore €/$ currency conversions (=cash in / cash out) for "real" brokers:
            if (broker != Broker.TV_PaperTrading && symbol == "EUR.USD")
            {
                continue;
            }

            // Side:
            var side = line["Side"] == "Buy" ? Side.Long : Side.Short;

            // Type:
            var typeString = broker == Broker.TV_PaperTrading ? line["Type"].Replace(" ", "") : "";
            var type = broker == Broker.TV_PaperTrading ? Enum.Parse<OrderType>(typeString) : OrderType.IBKR;

            // Amount:
            var qty = line["Qty"];
            qty = qty.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var amount = float.Parse(qty, floatCulture);

            // Price:
            var fillPriceString = line["Fill Price"];
            fillPriceString = fillPriceString.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var priceString = broker == Broker.TV_PaperTrading ? line["Limit Price"] : "";
            priceString = priceString.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            
            // Ignore cancelled stop & market orders (TradingView doesn't export the price for (cancelled) Stop Loss orders, and market orders don't have a price anyways - which would lead to errors further below. Therefore skip these types of orders):
            if ((type == OrderType.Stop || type == OrderType.Market) && fillPriceString == string.Empty && priceString == string.Empty)
            {
                continue;
            }
            
            var price = fillPriceString == string.Empty ? float.Parse(priceString, floatCulture) : float.Parse(fillPriceString, floatCulture);
            // necessary since some prices have more than 2 digits:
            price = Mathf.Round(price * 100f) / 100f;

            // Placing Time:
            var placingTime = broker == Broker.TV_PaperTrading ? DateTime.Parse(line["Placing Time"]) : DateTime.Parse(line["Time"]);

            // Closing Time:
            var closingTime = broker == Broker.TV_PaperTrading ? DateTime.Parse(line["Closing Time"]) : DateTime.Parse(line["Time"]);

            // Order Id:
            var orderId = broker == Broker.TV_PaperTrading ? uint.Parse(line["Order ID"]) : 0;

            // Commission:
            var commissionString = (broker == Broker.TV_IBKR_Paper || broker == Broker.TV_IBKR) ? line["Commission"] : string.Empty;
            var commission = (broker == Broker.TV_IBKR_Paper || broker == Broker.TV_IBKR) && commissionString != string.Empty ? float.Parse(commissionString, floatCulture) : 0.0f;

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
                orderId = orderId,
                commission = commission,
            };
            historyEntries.Add(historyEntry);
        }

        historyEntries = broker == Broker.TV_PaperTrading ? historyEntries.OrderBy(entry => entry.orderId).ToList() : historyEntries.OrderBy(entry => entry.placingTime).ToList();

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

    List<PositionsEntry> GetPositionsEntries(CultureInfo floatCulture, List<Dictionary<string, string>> positions, Broker broker)
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
            var entryPrice = float.Parse(broker == Broker.TV_PaperTrading ? line["Avg Fill Price"] : line["Avg Price"], floatCulture);

            // Price target:
            var priceTarget = 0.0f;
            var hasPriceTarget = broker == Broker.TV_PaperTrading && float.TryParse(line["Take Profit"], NumberStyles.Float, floatCulture, out priceTarget);

            // Stop loss:
            var stopLoss = 0.0f;
            var hasStopLoss = broker == Broker.TV_PaperTrading && float.TryParse(line["Stop Loss"], NumberStyles.Float, floatCulture, out stopLoss);

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

    #region Order Entries
    // These are only to get Price Target and Stop Loss orders for IBKR Trades as well:
    class OrderEntry
    {
        public string symbol;
        public Side side;
        public OrderType type;
        public float amount;
        public float limitPrice;
        public OrderStatus status;
        public DateTime time;
    }

    List<OrderEntry> GetOrderEntries(CultureInfo floatCulture, List<Dictionary<string, string>> orders, Broker broker)
    {
        var sb = new StringBuilder();
        var orderEntries = new List<OrderEntry>();

        if ((broker != Broker.TV_IBKR_Paper && broker != Broker.TV_IBKR) || orders == null)
        {
            return orderEntries;
        }

        foreach (var line in orders)
        {
            // Symbol:
            var symbol = line["Symbol"];

            // Side:
            var sideString = line["Side"];
            var side = sideString == "Buy" ? Side.Long : Side.Short;

            // Type:
            if (Enum.TryParse<OrderType>(line["IB Order Type"], out var type))
            {
                if (type != OrderType.Limit)
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            // Amount:
            var amountString = line["Filled/Remain"];
            if (amountString != null)
            {
                var slashIndex = amountString.IndexOf('/');
                if (slashIndex >= 0)
                {
                    amountString = amountString.Substring(slashIndex + 1, amountString.Length - (slashIndex + 1));
                }
            }
            var amount = float.Parse(amountString, floatCulture);

            // Price:
            var priceString = line["Limit Price"];
            var price = float.Parse(priceString, floatCulture);

            // Status:
            var statusString = line["Status"];
            var status = statusString == "Working" ? OrderStatus.Working : statusString == "Filled" ? OrderStatus.Filled : OrderStatus.Cancelled;

            // Time:
            var time = DateTime.Parse(line["Last Update Time"]);

            var orderEntry = new OrderEntry
            {
                symbol = symbol,
                side = side,
                type = type,
                amount = amount,
                limitPrice = price,
                status = status,
                time = time
            };

            orderEntries.Add(orderEntry);
        }

        orderEntries = orderEntries.OrderBy(entry => entry.time).ToList();

        sb.AppendLine("Symbol, Side, Order Type, Amount, Limit Price, Status, Time, Order Id");
        foreach (var orderEntry in orderEntries)
        {
            sb.AppendLine($"{orderEntry.symbol},{orderEntry.side},{orderEntry.type},{orderEntry.amount},{CapDecimalPlaces(orderEntry.limitPrice, floatCulture)},{orderEntry.status},{orderEntry.time}");
        }
        Debug.Log(sb);

        return orderEntries;
    }
    #endregion

    #region Interactive Brokers
    class IBTradeEntry
    {
        public string symbol;
        public Side side;
        public float amount;
        public float price;
        public DateTime closingTime;
        public float commission;

        public Order GetOrder()
        {
            return new Order()
            {
                amount = amount,
                price = price,
                time = closingTime,
                commission = commission,
            };
        }
    }

    List<IBTradeEntry> GetIBTradesEntries(CultureInfo floatCulture, List<Dictionary<string, string>> trades)
    {
        var sb = new StringBuilder();
        var ibTradeEntries = new List<IBTradeEntry>();

        foreach (var line in trades)
        {
            if (line["Header"] != "Data")
            {
                continue;
            }
            
            // Symbol:
            var symbol = line["Symbol"];
        
            // Amount:
            var qty = line["Quantity"];
            // qty = qty.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var amount = float.Parse(qty, floatCulture);
            
            // Side:
            var side = amount < 0 ? Side.Short : Side.Long;
        
            // Price:
            var priceString = line["T. Price"];
            // priceString = priceString.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            
            // Ignore cancelled stop & market orders (TradingView doesn't export the price for (cancelled) Stop Loss orders, and market orders don't have a price anyways - which would lead to errors further below. Therefore skip these types of orders):
            // if ((type == OrderType.Stop || type == OrderType.Market) && fillPriceString == string.Empty && priceString == string.Empty)
            // {
                // continue;
            // }
            
            var price = float.Parse(priceString, floatCulture);
            // necessary since some prices have more than 2 digits:
            price = Mathf.Round(price * 100f) / 100f;
        
            // Closing Time:
            var eastern = TZ("Eastern Standard Time", "America/New_York");
            var berlin  = TZ("W. Europe Standard Time", "Europe/Berlin");

            // Your input has NO offset, but is in ET (EDT/EST depending on date):
            string s = line["Date/Time"]; // e.g. "2025-10-15 13:45"
            var etLocal = DateTime.Parse(s, CultureInfo.InvariantCulture); // Unspecified kind

            // Attach the correct UTC offset for ET on that date (DST-aware)
            var etDto = new DateTimeOffset(etLocal, eastern.GetUtcOffset(etLocal));

            // Convert to Berlin (CEST/CET handled)
            var closingTime = TimeZoneInfo.ConvertTime(etDto, berlin).DateTime;
        
            // Order Id:
            // var orderId = broker == Broker.TV_PaperTrading ? uint.Parse(line["Order ID"]) : 0;
        
            // Commission:
            var commissionString = line["Comm/Fee"];
            var commission = commissionString != string.Empty ? Mathf.Abs(float.Parse(commissionString, floatCulture)) : 0.0f;
        
            var interactiveBrokersEntry = new IBTradeEntry
            {
                symbol = symbol,
                side = side,
                amount = Math.Abs(amount),
                price = price,
                closingTime = closingTime,
                commission = commission,
            };
            ibTradeEntries.Add(interactiveBrokersEntry);
        }
        
        ibTradeEntries = ibTradeEntries.OrderBy(entry => entry.closingTime).ToList();
        
        sb.AppendLine("Symbol, Side, Amount, Price, Time");
        foreach (IBTradeEntry ibTradeEntry in ibTradeEntries)
        {
            sb.AppendLine($"{ibTradeEntry.symbol},{ibTradeEntry.side},{ibTradeEntry.amount},{ibTradeEntry.price},{ibTradeEntry.closingTime}");
        }
        Debug.Log(sb);

        return ibTradeEntries;
    }
    
    static TimeZoneInfo TZ(string windowsId, string ianaId)
    {
        // Windows uses Windows IDs, macOS/Linux/Android use IANA.
        try { return TimeZoneInfo.FindSystemTimeZoneById(windowsId); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById(ianaId); }
    }
    
    class IBPositionsEntry
    {
        public string symbol;
        public Side side;
        public float avgFillPrice;
        public float priceTarget;
        public float stopLoss;
        public float amount;
    }

    List<IBPositionsEntry> GetIBPositionsEntries(CultureInfo floatCulture, List<Dictionary<string, string>> positions)
    {
        var sb = new StringBuilder();
        var positionsEntries = new List<IBPositionsEntry>();

        foreach (var line in positions)
        {
            if (line["Header"] != "Data")
            {
                continue;
            }
            
            // Symbol:
            var symbol = line["Symbol"];
        
            // Amount:
            var qty = line["Quantity"];
            // qty = qty.Replace(" ", ""); // TradingView adds space instead of comma for numbers > 999, therefore need to remove it
            var amount = float.Parse(qty, floatCulture);
            
            // Side:
            var side = amount < 0 ? Side.Short : Side.Long;

            // Avg Fill price:
            var entryPrice = float.Parse(line["Cost Price"], floatCulture);

            var positionsEntry = new IBPositionsEntry
            {
                symbol = symbol,
                side = side,
                avgFillPrice = entryPrice,
                priceTarget = 0,
                stopLoss = 0,
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
    
    // These are only to get Price Target and Stop Loss orders for Interactive Brokers Trades:
    class IBOrderEntry
    {
        public string symbol;
        public Side side;
        public OrderType type;
        public float amount;
        public float limitPrice;
        public OrderStatus status;
        public DateTime time;
    }

    List<IBOrderEntry> GetIBOrderEntries(CultureInfo floatCulture, List<Dictionary<string, string>> ibOrders)
    {
        var sb = new StringBuilder();
        var ibOrderEntries = new List<IBOrderEntry>();

        foreach (var line in ibOrders)
        {
            // Symbol:
            var symbol = line["Symbol"];

            // Side:
            var sideString = line["Buy/Sell"];
            var side = sideString == "BUY" ? Side.Long : Side.Short;

            // Type:
            var type = OrderType.Limit;
            if (line["OrderType"] != "LMT")
            {
                continue;
            }

            // Amount:
            var amountString = line["Quantity"];
            // if (amountString != null)
            // {
            //     var slashIndex = amountString.IndexOf('/');
            //     if (slashIndex >= 0)
            //     {
            //         amountString = amountString.Substring(slashIndex + 1, amountString.Length - (slashIndex + 1));
            //     }
            // }
            var amount = float.Parse(amountString, floatCulture);

            // Price:
            var priceString = line["TradePrice"];
            var price = float.Parse(priceString, floatCulture);

            // Status:
            // TODO: THAT'S THE PROBLEM! IB ONLY SHOWS FILLED ORDERS!
            // var statusString = line["Status"];
            // var status = statusString == "Working" ? OrderStatus.Working : statusString == "Filled" ? OrderStatus.Filled : OrderStatus.Cancelled;
            var status = OrderStatus.Filled;
            
            // Time:
            var time = DateTime.Parse(line["OrderTime"]);

            var ibOrderEntry = new IBOrderEntry
            {
                symbol = symbol,
                side = side,
                type = type,
                amount = amount,
                limitPrice = price,
                status = status,
                time = time
            };

            ibOrderEntries.Add(ibOrderEntry);
        }

        ibOrderEntries = ibOrderEntries.OrderBy(entry => entry.time).ToList();

        sb.AppendLine("Symbol, Side, Order Type, Amount, Limit Price, Status, Time, Order Id");
        foreach (var orderEntry in ibOrderEntries)
        {
            sb.AppendLine($"{orderEntry.symbol},{orderEntry.side},{orderEntry.type},{orderEntry.amount},{CapDecimalPlaces(orderEntry.limitPrice, floatCulture)},{orderEntry.status},{orderEntry.time}");
        }
        Debug.Log(sb);

        return ibOrderEntries;
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