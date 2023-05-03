# TradingView Data Assembler

This tool assembles paper trading data from TradingView into a readable csv format, to be able to create a Trading Journal in a spreadsheet.

1 row = 1 trade of the same stock, until all bought stocks have been sold.

**Columns:**

_Symbol | Side | Time of first entry | Avg entry price | Total entry stock amount | Stop Loss | Price Target | Time of last exit | Avg exit price | Total exit stock amount | Amount of different entries | Amount of different exits_

## Download:

[Version 1.2](https://github.com/Civil-Cucumber/TradingViewDataAssembler_Public/releases/tag/v1.2.0)

## Update info
From version 1.2 it's no longer necessary to download TradingJournal.csv! All you will need is Positions.csv and History.csv.

## Setup:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and connect to **PaperTrading**.
2. Click on the **Positions** tab, then click on the 3 dots in the upper right corner, make sure all categories have a checkmark.
3. Click on the **History** tab, then click on the 3 dots in the upper right corner, make sure all categories have a checkmark.

<img src="https://user-images.githubusercontent.com/126332884/222277125-d58adb8b-f4cf-4b73-a285-fbc6c583103a.png" width="600">

4. Open **TradingViewDataAssembler**.
5. Open **Explorer** (Win) or **Finder** (Mac) and copy the path where you plan to save TradingView's Papertrading csv files to (e. g. `C:\Users\yourname\Downloads` (Win) or `/Users/yourname/Downloads` (Mac))
6. Paste it into the `Folder` input field in **TradingViewDataAssembler**.

<img src="https://user-images.githubusercontent.com/126332884/222278572-42cb6627-a752-4664-a773-61b3d96eb3dd.png" width="600">

## How to use:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and connect to **PaperTrading**.
2. Click on **Paper Trading** in the upper left, then `Export Data`.
3. Select **Positions** and click `Export`, select **History** and click `Export`. If you didn't select your Downloads folder in step 5 above, move the downloaded csv files to the folder you defined there. 

<img src="https://user-images.githubusercontent.com/126332884/222279662-691f9e25-7007-40eb-9734-b7c529686077.png" width="600">

4. Open **TradingViewDataAssembler**: the data is automatically read out, combined and saved to your clipboard!

<img src="https://user-images.githubusercontent.com/126332884/222283307-7978a920-6cfd-4657-8f15-f9a214154c79.png" width="600">

5. Open your trading journal **spreadsheet**, and paste the data in.

<img src="https://user-images.githubusercontent.com/126332884/222283536-e9de50e1-a254-45f2-8704-f86ae649e646.png" width="600">

## To consider:

The trade infos are read out from the `Positions.csv` _(open positions)_ and `History.csv` _(recent filled or cancelled orders)_.

The `History.csv` is limited to max. 100 orders. There is unfortunately no way to load more, so information for orders that have been filled before is lost.
