
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VBTBotConsole3.Analitics
{
    class SellPosition : IPosition
    {
        public decimal Value { 
            get
            {
                return EntryPrice - ClosePrice;
            }
        }

        public decimal EntryPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime EntryDateTime { get; set; }
        public DateTime CloseDateTime { get; set; }
        public bool Completed { get; set; }

        public SellPosition()
        {
            Completed = false;
        }
    }
}
