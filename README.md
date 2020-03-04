AlpacaTrade
===

[![Donate with Bitcoin](https://en.cryptobadges.io/badge/micro/12BMo7nBeBhDGDGagwqSRPAv3fkQi8nCfq)](https://en.cryptobadges.io/donate/12BMo7nBeBhDGDGagwqSRPAv3fkQi8nCfq)
[![Donate with Ethereum](https://en.cryptobadges.io/badge/micro/0xd163fdde358f9000A4E9290f23B84DFb6E9190D3)](https://en.cryptobadges.io/donate/0xd163fdde358f9000A4E9290f23B84DFb6E9190D3)
[![Donate with Litecoin](https://en.cryptobadges.io/badge/micro/LVSmZByqa6Cp1BFwgqeUyMjKmpfHP23ApR)](https://en.cryptobadges.io/donate/LVSmZByqa6Cp1BFwgqeUyMjKmpfHP23ApR)

Simple automated trading example for [alpaca.markets](https://alpaca.markets) written in C#.

Strategy
---

**Entry**

Buys at the low of day at 10:00 AM EST if the first hourly candle closes red. Day orders are used so they cancel at the market close. The strategy essentially resets everyday if there is no fill.

**Exit**

Sells when the position is up half the hourly 20 period Average True Range (ATR).

**Stop**

Holds the bag until profitable.

Requirements
---

You should have a live [alpaca.markets](https://alpaca.markets) brokerage account for real time data from Polygon. If you do not, you will need to modify the code to retrieve data from their free API that utilizes IEX. Using the free data means there will be missing bars on certain stocks and possibly wrong OHLC values. This will affect entries for the strategy.

Configuration
---

Upon first start a `settings.json` file will be created. This file allows you to set your Alpaca API keys and the symbols you want it to trade.

Disclaimer
---

Do not use this to trade with a live account without rigorous testing and _do_ expect to lose all your money if you use the implemented strategy. I take no responsibility for how you use this code.

[Risks of Automated Trading Systems](https://support.alpaca.markets/hc/en-us/articles/360015623671-Risks-of-Automated-Trading-Systems)

License
---

MIT
