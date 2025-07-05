using Binance.Net.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace VBTBotConsole3
{
    [Index(nameof(SymbolId), IsUnique = false)]
    [Index(nameof(OpenTime), IsUnique = true)]
    [Index(nameof(CloseTime), IsUnique = true)]
    class Kline : IBinanceKline
    {
        public Kline () : base() { }

        public Kline (IBinanceKline binanceKline, string symbol)
        {
            SymbolId = symbol.GetHashCode();

            OpenPrice = binanceKline.OpenPrice;
            HighPrice = binanceKline.HighPrice;
            LowPrice = binanceKline.LowPrice;
            ClosePrice = binanceKline.ClosePrice;

            OpenTime = binanceKline.OpenTime;
            CloseTime = binanceKline.CloseTime;

            Volume = binanceKline.Volume;
            QuoteVolume = binanceKline.QuoteVolume;

            TradeCount = binanceKline.TradeCount;

            TakerBuyBaseVolume = binanceKline.TakerBuyBaseVolume;
            TakerBuyQuoteVolume = binanceKline.TakerBuyQuoteVolume;

            //Hashes all data that does not change in the future
            KlineId = HashCode.Combine(SymbolId.GetHashCode(), OpenTime.GetHashCode(), (CloseTime - OpenTime).GetHashCode());
        }
        [Key]
        public int KlineId { get; set; }

        public int SymbolId { get; set; }

        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }

        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }

        public decimal Volume { get; set; }
        public decimal QuoteVolume { get; set; }

        public int TradeCount { get; set; }

        public decimal TakerBuyBaseVolume { get; set; }
        public decimal TakerBuyQuoteVolume { get; set; }

        public override string ToString()
        {
            return $" ID numbers\t\t| Time properties\t\t\t| Price properties \n" +
                $" KlineId: {KlineId}\t| OpenTime: {OpenTime}  \t| OpenPrice: {OpenPrice}\n" +
                $" SymbolId: {SymbolId}\t| CloseTime: {CloseTime}\t| ClosePrice: {ClosePrice}";
        }

        public void Update(IBinanceKline binanceKline)
        {
            HighPrice = binanceKline.HighPrice;
            LowPrice = binanceKline.LowPrice;
            ClosePrice = binanceKline.ClosePrice;

            CloseTime = binanceKline.CloseTime;

            Volume = binanceKline.Volume;
            QuoteVolume = binanceKline.QuoteVolume;

            TradeCount = binanceKline.TradeCount;

            TakerBuyBaseVolume = binanceKline.TakerBuyBaseVolume;
            TakerBuyQuoteVolume = binanceKline.TakerBuyQuoteVolume;
        }
    }
}
