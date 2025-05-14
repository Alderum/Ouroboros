using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VBTBotConsole3.Analitics
{
    class BuyPosition : IPosition
    {
        public decimal Value
        {
            get
            {
                return ClosePrice - EntryPrice;
            }
        }

        public decimal EntryPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime EntryDateTime { get; set; }
        public DateTime CloseDateTime { get; set; }
        public bool Completed { get; set; }

        public BuyPosition()
        {
            Completed = false;
        }
    }
}
