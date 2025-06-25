using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VBTBotConsole3.Analitics
{
    class StatisticsBuy
    {
        public decimal AvarageValue { get; set; }
        public decimal AvarageDuration { get; set; }

        List<BuyPosition> buys = new List<BuyPosition>();

        public StatisticsBuy(List<BuyPosition> buyPositions)
        {
            buys = buyPositions;
        }

        
    }
}
