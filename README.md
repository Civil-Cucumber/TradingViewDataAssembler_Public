# TradingViewDataAssembler

This tool assembles paper trading data from TradingView into a readable csv format, to be able to create a Trading Journal in a spreadsheet.
1 row = 1 trade of the same stock, until all bought stocks have been sold.

**Columns:**

_Symbol | Side | Time of first entry | Avg entry price | Total entry stock amount | Stop Loss | Price Target | Time of last exit | Avg exit price | Total exit stock amount | Amount of different entries | Amount of different exits_

## Setup:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and connect to **PaperTrading**.
2. Click on the **Positions** tab, then click on the 3 dots in the upper right corner, make sure all categories have a checkmark.
3. Click on the **History** tab, then click on the 3 dots in the upper right corner, make sure all categories have a checkmark.
4. Click on the **Trading Journal** tab, then click on the 3 dots in the upper right corner, make sure all categories have a checkmark.
5. Open **TradingViewDataAssembler**.
6. Open **Explorer** (Win) or **Finder** (Mac) and copy the path where you plan to save TradingView's Papertrading csv files to (e. g. `C:\Users\yourname\Downloads` (Win) or `/Users/yourname/Downloads` (Mac))
7. Paste it into the `Folder` input field in **TradingViewDataAssembler**.

## How to use:
1. Open [tradingview.com/chart](tradingview.com/chart) (make sure your language is set to English!) and connect to **PaperTrading**.
2. Click on **Paper Trading** in the upper left, then `Export Data`.
3. Select **Positions** and click `Export`, select **History** and click `Export`, select **Trading Journal** and click `Export`. If you didn't select your Downloads folder in step 6 above, move the downloaded csv files to the folder you defined there. 
4. Open **TradingViewDataAssembler**: the data is automatically read out, combined and saved to your clipboard!
5. Open your trading journal **spreadsheet**, and paste the data in.
