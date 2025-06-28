using Binance.Net.Enums;
using Binance.Net.Interfaces;

namespace VBTBotConsole3
{
    class Kline : IBinanceKline
    {
        public Kline () { }

        public Kline (IBinanceKline binanceKline)
        {
            KlineId = binanceKline.GetHashCode();
            OpenPrice = binanceKline.OpenPrice;
            HighPrice = binanceKline.HighPrice;
            LowPrice = binanceKline.LowPrice;
            OpenTime = binanceKline.OpenTime;
            CloseTime = binanceKline.CloseTime;
            Volume = binanceKline.Volume;
            QuoteVolume = binanceKline.QuoteVolume;
            TradeCount = binanceKline.TradeCount;
            TakerBuyBaseVolume = binanceKline.TakerBuyBaseVolume;
            TakerBuyQuoteVolume = binanceKline.TakerBuyQuoteVolume;
            ClosePrice = binanceKline.ClosePrice;
        }

        public int KlineId { get; set; }
        public DateTime OpenTime { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal QuoteVolume { get; set; }
        public int TradeCount { get; set; }
        public decimal TakerBuyBaseVolume { get; set; }
        public decimal TakerBuyQuoteVolume { get; set; }
    }
}
