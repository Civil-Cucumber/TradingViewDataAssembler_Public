# TradingView Data Assembler

This tool assembles your data from TradingView's Paper Trading or Interactive Brokers connection into a **readable csv format**, to be able to easily update your Trading Journal spreadsheet.

1 row = 1 trade of the same stock, until all bought stocks have been sold.

**Columns:**

_Symbol | Side | Time of first entry | Avg entry price | Total entry stock amount (incl. from Adds) | Stop Loss | Price Target | Time of final exit | Avg exit price | Total exit stock amount (incl. from partial closes) | Amount of entries in total | Times of Adds (separated by '#') | Amount of exits in total | Times of Partial Closes (separated by '#') | Total commission costs_

## Download:

[Version 2.0](https://github.com/Civil-Cucumber/TradingViewDataAssembler_Public/releases/tag/v2.0.0)

## Update info:
- From version 2.0 on 2 more columns are now exported: One that shows when was added to the position, the other when partial closes happened. This means you will likely need to adjust your Trading Journals accordingly!
- From version 1.2 on it's no longer necessary to download TradingJournal.csv! All you will need is Positions.csv and History.csv.

## Setup:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and in the Trading Panel connect to **Paper Trading** - or **Interactive Brokers** (if you want to start with 1 Share trading already).
2. Click on the **Positions** tab, then click on the 3 dots in the upper right corner: make sure all categories have a checkmark.
3. Click on the **History** (IBKR: **Trade History**) tab, then click on the 3 dots in the upper right corner: make sure all categories have a checkmark.
4. (Only for IBKR:) Click on the **Orders** tab, then click on the 3 dots in the upper right corner: make sure all categories have a checkmark.

<img src="https://user-images.githubusercontent.com/126332884/222277125-d58adb8b-f4cf-4b73-a285-fbc6c583103a.png" width="600">

4. Open **TradingViewDataAssembler**.
5. Open **Explorer** (Win) or **Finder** (Mac) and copy the path where you plan to save TradingView's exported Paper Trading / Interactive Broker csv files to (e. g. `C:\Users\yourname\Downloads` (Win) or `/Users/yourname/Downloads` (Mac))
6. Paste it into the `Folder` input field in **TradingViewDataAssembler**.
7. Select your "Broker": do you want to read out data from **Paper Trading** or **IBKR** files?

<img src="https://user-images.githubusercontent.com/126332884/222278572-42cb6627-a752-4664-a773-61b3d96eb3dd.png" width="600">

8. If you want to always open your Trading Journal automatically after you click to close the TradingViewDataAssembler: go to the folder containing the TradingView Data Assembler.exe file and open there the "TradingView Data Assembler_Data" folder. Then go to "StreamingAssets" and open "SettingsConfig.json".
9. Here copy-paste your journal's URL between the quotation marks for the according variables, depending on whether it should be happening after you've read out the data for Paper Trading or IBKR.
    F. e.:
    `"paperTradingJournalUrl": "https://docs.google.com/spreadsheets/d/1wbYD_wsuVRZhZAlszSL__EglcEidM4J-BGCBX8C_StM/",`

## How to use:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and connect to **PaperTrading**.
2. Click on **Paper Trading** in the upper left, then `Export Data`.
3. Select **Positions** and click `Export`, select **History** (IBKR: **Trade History**) and click `Export`, (Only for IBKR:) select **Orders** and click `Export`. If you didn't select your Downloads folder in step 5 above, move the downloaded csv files to the folder you defined there.

<img src="https://user-images.githubusercontent.com/126332884/222279662-691f9e25-7007-40eb-9734-b7c529686077.png" width="600">

4. Open **TradingViewDataAssembler**: the data is automatically read out, combined and saved to your clipboard!

<img src="https://user-images.githubusercontent.com/126332884/222283307-7978a920-6cfd-4657-8f15-f9a214154c79.png" width="600">

5. Open your trading journal **spreadsheet**, and paste the data in.

<img src="https://user-images.githubusercontent.com/126332884/222283536-e9de50e1-a254-45f2-8704-f86ae649e646.png" width="600">

## To consider:

### Paper Trading:

The trade infos are read out from the `Positions.csv` _(open positions)_ and `History.csv` _(recent filled or cancelled orders)_.

The `History.csv` is limited to max. 100 orders. There is unfortunately no way to load more, so information for orders that have been filled before is lost.

### IBKR:

`Account History.csv` and `Orders.csv` only contains the data of the last **7 days**! There is unfortunately no way to load more, so information for orders that have been filled before would need to be manually received and copied from the Interactive Broker's Activity Statements site in the Reports section. 
Better update your Journal every day to avoid this!

## How to create your own Trading Journal in Google Sheets:

See here: https://www.reddit.com/r/RealDayTrading/comments/1ffyj1q/from_38_to_81_after_18_months_44_trading_journal/
