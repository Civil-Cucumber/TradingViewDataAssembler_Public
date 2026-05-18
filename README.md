# TradingView Data Assembler

This tool assembles your data from any of these 5 sources... 
- **TradingView:**
    - TradingView's Paper Trading
    - TradingView's Interactive Brokers Paper account connection
    - TradingView's Interactive Brokers Live account connection
- **Interactive Brokers' Activity Statement:**
    - Interactive Brokers' Paper account Activity Statement
    - Interactive Brokers' Live account Activity Statement

...into a **single readable csv format** in your clipboard, that you can then easily paste into your **Trading Journal spreadsheet** to update it.

--> 1 row = 1 trade of the same stock or option, until all bought stocks have been sold. (Multi-leg Options are treated as separate trades.)

**Columns:**

1. _Symbol_
2. _Side_
3. _Time of first entry_
4. _Avg entry price_
5. _Total entry amount (incl. later position increases)_
6. _Stop Loss_
7. _Price Target_
8. _Time of final exit_
9. _Avg exit price_
10. _Total exit amount (incl. earlier partial closes)_
11. _Amount of entries in total_
12. _Timestamps of position increases (separated by '#')_
13. _Amount of exits in total_
14. _Timestamps of partial closes (separated by '#')_
15. _Total commission costs_

**HINT:** 'Stop Loss' or 'Price Target' info can only be read out if you if you are importing the information from TradingView, and set them up there. Unfortunately Interactive Brokers itself doesn't provide a way to export this information.

## Download:

[Version 3.1](https://github.com/Civil-Cucumber/TradingViewDataAssembler_Public/releases/tag/v3.1.0)

### Update info:
- From version 2.0 on 2 more columns are now exported: One that shows when was added to the position, the other when partial closes happened. This means you will likely need to adjust your Trading Journals accordingly!
- From version 1.2 on it's no longer necessary to download TradingJournal.csv for TradingView PaperTrading! All you need is Positions.csv and History.csv.

## Option 1: Import TradingView's CSVs:
If you want to import data from TradingView's Paper Trading, TradingView's Interactive Brokers Paper account connection, or TradingView's Interactive Brokers Live account connection:

### Setup:
1. Open [tradingview.com/chart](https://tradingview.com/chart) or the TradingView desktop app, and make sure your language is set to English!
2. In the Trading Panel connect to **Paper Trading** - or **Interactive Brokers** (make sure there that you correctly select "Paper" or "Live" before entering your credentials).
3. Click on the **Positions** tab, then click on the '3 bars' icon in the upper right corner: make sure all categories have a checkmark.
4. Click on the **Order History** (IBKR: **Trade History**) tab, then click on the '3 bars' icon in the upper right corner: make sure all categories have a checkmark.
5. (Only for Interactive Brokers:) Click on the **Orders** tab, then click on the '3 bars' icon in the upper right corner: make sure all categories have a checkmark.

<img src="https://user-images.githubusercontent.com/126332884/222277125-d58adb8b-f4cf-4b73-a285-fbc6c583103a.png" width="600">

6. Open **TradingView Data Assembler.exe**.
7. Open the Windows **Explorer** and copy the path where you plan to save TradingView's exported *.csv files to (f. e. `C:\Users\yourname\Downloads`)
8. Paste the path into the `Folder` input field in **TradingView Data Assembler**.
9. Select your "Broker" (so the app knows which file names to look for):
   - `TV: Paper Trading` = TradingView PaperTrading
   - `TV: IB Paper` = TradingView's Interactive Brokers Paper account connection
   - `TV: IB Live` = TradingView's Interactive Brokers Live account connection

<img src="https://user-images.githubusercontent.com/126332884/222278572-42cb6627-a752-4664-a773-61b3d96eb3dd.png" width="600">

10. **(Optional)** If you use an online spreadsheet as Trading Journal and want to always open it automatically after you click to close the TradingView Data Assembler: go to the folder containing the `TradingView Data Assembler.exe` file and open there the `TradingView Data Assembler_Data` folder. Then go to `StreamingAssets` and open `SettingsConfig.json`.
11. **(Optional)** Here copy-paste your journal's URL between the quotation marks for the according variables, depending on whether it should be happening after you've read out the data for Paper Trading (`tvPaperTradingJournalUrl`), Interactive Brokers Paper (`ibkrPaperJournalUrl`) or Interactive Brokers Live (`ibkrJournalUrl`).
    F. e.:
    `"tvPaperTradingJournalUrl": "https://docs.google.com/spreadsheets/d/1wbYD_wsuVRZhZAlszSL__EglcEidM4J-BGCBX8C_StM/",`

### How to use:
1. Open [tradingview.com/chart](https://tradingview.com/chart) or the TradingView desktop app, and make sure your language is set to English!
2. In the Trading Panel connect to **Paper Trading** - or **Interactive Brokers** (make sure there that you correctly select "Paper" or "Live" before entering your credentials).
3. Click on **Paper Trading**/**Interactive Brokers Paper**/**Interactive Brokers Live** in the upper left, then `Export Data...`.
4. Select **Positions** and click `Export`, then select **Order History** (IBKR: **Trade History**) and click `Export`, (Only for **IBKR**!:) then select **Orders** and click `Export`.
5. If you didn't select your Downloads folder in the Setup's process step 8, move the downloaded csv files to the folder you defined there.

<img src="https://user-images.githubusercontent.com/126332884/222279662-691f9e25-7007-40eb-9734-b7c529686077.png" width="600">

4. Open **TradingView Data Assembler.exe**: the data is automatically read out, combined and saved to your clipboard!

<img src="https://user-images.githubusercontent.com/126332884/222283307-7978a920-6cfd-4657-8f15-f9a214154c79.png" width="600">

5. Open your Trading Journal **spreadsheet**, and paste the data in.

<img src="https://user-images.githubusercontent.com/126332884/222283536-e9de50e1-a254-45f2-8704-f86ae649e646.png" width="600">

### To consider:

#### Paper Trading:

The trade infos are read out from the `Positions.csv` _(open positions)_ and `History.csv` _(recent filled or cancelled orders)_.

The `History.csv` is limited to max. 100 orders. There is unfortunately no way to load more, so information for orders that have been filled before is lost.

#### Interactive Brokers Activity Statements:
If you want to import data from TradingView's Paper Trading, TradingView's Interactive Brokers Paper account connection, or TradingView's Interactive Brokers Live account connection:

`Account History.csv` and `Orders.csv` only contains the data of the last **7 days**! There is unfortunately no way to load more.
However you can simply import the data from Interactive Broker's Activity Statement instead (see instructions below). Unfortunately this means you wouldn't have Stop Loss and Price Target data anymore though.
My recommendation: simply update your Journal after every trading day to avoid this.

## Option 2: Import Interactive Brokers Activity Statement CSV:
If you want to import data from Interactive Brokers' Paper account Activity Statement, or Interactive Brokers' Live account Activity Statement:

### Setup:
1. Log in to [interactivebrokers.com](https://interactivebrokers.com)  or [interactivebrokers.ie](https://interactivebrokers.ie) (make sure there that you correctly select "Paper" or "Live" before entering your credentials).
2. Copy-paste your account number from the account details in the upper right corner (f. e. "X1234567")
3. Go to the folder containing the `TradingView Data Assembler.exe` file and open there the `TradingView Data Assembler_Data` folder. Then go to `StreamingAssets` and open `SettingsConfig.json`.
4. Here copy-paste your account number between the quotation marks for the according variables, depending on whether you want to use Interactive Brokers Paper (`ibPaperUserId`), Interactive Brokers Live (`ibLiveUserId`), or both _(this is since downloaded actitvity statements are named `[accountnumber]_[firstday]_[lastday].csv`, so the app will need the account number to know which file to search for)._
5. Now open **TradingView Data Assembler.exe**.
6. Open the Windows **Explorer** and copy the path where you plan to save TradingView's exported *.csv files to (f. e. `C:\Users\yourname\Downloads`)
7. Paste the path into the `Folder` input field in **TradingView Data Assembler**.
8. Select your "Broker" (so the app knows which file names to look for). Notice you see now 1-2 more options, since you've entered your account number(s):
   - Ignore `TV: Paper Trading`, `TV: IB Paper`, `TV: IB Live` (or if you want to use them as well: follow instructions in the "TradingView" section above)
   - `IB: Paper` = Interactive Brokers' Paper account Activity Statement
   - `IB: Live` = Interactive Brokers' Live account Activity Statement

9. **(Optional)** If you use an online spreadsheet as Trading Journal and want to always open it automatically after you click to close the TradingView Data Assembler: go to the folder containing the `TradingView Data Assembler.exe` file and open there the `TradingView Data Assembler_Data` folder. Then go to `StreamingAssets` and open `SettingsConfig.json`.
11. **(Optional)** Here copy-paste your journal's URL between the quotation marks for the according variables, depending on whether it should be happening after you've read out the data for Interactive Brokers Paper (`ibkrPaperJournalUrl`) or Interactive Brokers Live (`ibkrJournalUrl`).
    F. e.:
    `"ibkrPaperJournalUrl": "https://docs.google.com/spreadsheets/d/1wbYD_wsuVRZhZAlszSL__EglcEidM4J-BGCBX8C_StM/",`

### How to use:
1. Log in to [interactivebrokers.com](https://interactivebrokers.com)  or [interactivebrokers.ie](https://interactivebrokers.ie) (make sure there that you correctly select "Paper" or "Live" before entering your credentials).
2. Click on **Performance & Reports** and select **Statements**.
3. Click on the blue arrow ("Run") next to **Activity Statement**.
4. In the popup select the date range you want (usually `Period` to `Custom Date Range`, then set `From Date` and `To Date`.
    _**Hint:** usually you can't select today or even yesterday, since it takes at least a day until the info becomes available._
   
5. Click on *Advanced Options** and make sure that `Language` is set to **English**!
6. Click **Download CSV**.
7. If you didn't select your Downloads folder in the Setup's process step 7, move the downloaded csv files to the folder you defined there.
8. Open **TradingView Data Assembler.exe**: the data is automatically read out, combined and saved to your clipboard!
9. Open your Trading Journal **spreadsheet**, and paste the data in.

<img src="https://user-images.githubusercontent.com/126332884/222283536-e9de50e1-a254-45f2-8704-f86ae649e646.png" width="600">

### To consider:

Usually the trading data from today or yesterday isn't available in the Activity Statement. It takes at least a day until the info becomes available in Interactive Brokers.

Also unfortunately Interactive Brokers itself doesn't provide a way to Stop Loss + Price Target information. 
It can only be read out if you if you are importing the information from TradingView, and set them up there. 
My recommendation: enter Stop Loss + Price Target manually to the Trading Journal instead.

## How to create your own Trading Journal in Google Sheets:

See here: https://www.reddit.com/r/RealDayTrading/comments/1ffyj1q/from_38_to_81_after_18_months_44_trading_journal/
