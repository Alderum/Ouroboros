namespace VBTBotConsole3.Indicators
{
    class MovingAvarage
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal Value { get; set; }
        public int Depth { get; set; }

        public static List<MovingAvarage> GetATR(List<Kline> klines, decimal depth)
        {
            List<MovingAvarage> atrs = new List<MovingAvarage>();

            MovingAvarage atr = new MovingAvarage();

            atr.Value = klines[0].High - klines[0].Low;
            atr.Id = klines[0].KlineId;
            atr.DateTime = klines[0].DateTime;
            atr.Depth = (int)depth;

            atrs.Add(atr);

            //Calculating atr for beginning
            for (int i = 1; i < depth; i++)
            {
                Console.Write(".");
                atr = new MovingAvarage();

                //True range of all Awailable klines
                for (int a = 0; a <= i; a++)
                    atr.Value += klines[i - a].High - klines[i - a].Low;

                atr.Id = klines[i].KlineId;
                atr.Value /= i;
                atr.DateTime = klines[i].DateTime;
                atr.Depth = (int)depth;

                atrs.Add(atr);
            }

            Console.WriteLine();
            for (int i = (int)depth; i < klines.Count; i++)
            {
                atr = new MovingAvarage();

                for (int a = 0; a <= depth; a++)
                    atr.Value += klines[i - a].High - klines[i - a].Low;
                atr.Value /= depth;

                atr.Id = klines[i].KlineId;
                atr.DateTime = klines[i].DateTime;
                atr.Depth = (int)depth;

                atrs.Add(atr);
            }

            Console.WriteLine();

            return atrs;
        }

        public static List<MovingAvarage> GetATRFromTV(List<Kline> klines, decimal depth)
        {
            //List that will be returned
            List<MovingAvarage> atrs = new List<MovingAvarage>();

            //True range is differance between high and low of kline
            List<decimal> trueRanges = new List<decimal>();
            for (int i = 0; i < klines.Count; i++)
                trueRanges.Add(klines[i].High - klines[i].Low);

            //We don't use atr, because in TV we use rma for calculatin true ranges
            //avarages
            var rmaTrueRanges = GetRMAFromValue(trueRanges, depth);

            //ATR is basicaly RMA but indexed for every kline
            MovingAvarage atr;
            for (int i = 0; i < klines.Count; i++)
            {
                atr = new MovingAvarage();
                atr.Value = rmaTrueRanges[i];

                atr.Id = klines[i].KlineId;
                atr.DateTime = klines[i].DateTime;
                atr.Depth = (int)depth;

                atrs.Add(atr);
            }

            return atrs;
        }

        public static List<MovingAvarage> GetEMA(List<Kline> klines, decimal depth)
        {
            if (klines == null)
                return null;

            List<MovingAvarage> emas = new List<MovingAvarage>();
            Console.WriteLine("Klines count: " + klines.Count);
            decimal weighting = 2 / (depth + 1);

            //First ema calculating
            MovingAvarage ema = new MovingAvarage();
            if(klines.Count != 0)
            {
                ema.Value = klines[0].Close * weighting + klines[0].Open * (1 - weighting);
                ema.DateTime = klines[0].DateTime;
                ema.Id = klines[0].KlineId;
                ema.Depth = (int)depth;

                emas.Add(ema);

                //Calculating ema for the rest of the klines
                for (int i = 1; i <= klines.Count - 1; i++)
                {
                    ema = new MovingAvarage();
                    ema.Value = klines[i].Close * weighting + emas[i - 1].Value * (1 - weighting);
                    ema.DateTime = klines[i].DateTime;
                    ema.Id = klines[i].KlineId;
                    ema.Depth = (int)depth;
                    emas.Add(ema);
                }
            }

            return emas;
        }

        public static List<decimal> GetRMAFromValue(List<decimal> values, decimal depth)
        {
            List<decimal> rmas = new List<decimal>();
            decimal weighting = 1 / depth;

            //First ema calculating
            decimal rma;
            rma = values[0];

            rmas.Add(rma);

            //Calculating ema for the rest of the klines
            for (int i = 1; i <= values.Count - 1; i++)
            {
                rma = values[i] * weighting + rmas[i - 1] * (1 - weighting);
                rmas.Add(rma);
            }

            return rmas;
        }
    }
}
