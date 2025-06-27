using Binance.Net.Enums;

namespace VBTBotConsole3
{
    class Kline
    {
        public int KlineId { get; set; }
        public decimal High { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal Low { get; set; }
        public KlineInterval Interval { get; set; }
        public DateTime DateTime { get; set; }
    }
}
