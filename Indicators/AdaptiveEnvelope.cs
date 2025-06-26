namespace VBTBotConsole3.Indicators
{
    class AdaptiveEnvelope
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public decimal Center { get; set; }
        public int Depth { get; set; }
        public int NumberOfLines { get; set; }
        public List<decimal>? Low { get; set; }
        public List<decimal>? High { get; set; }

        public static List<AdaptiveEnvelope> GetAdaptiveEnvelope(List<Kline> klines, List<MovingAvarage> movingAvarages, int deviation, int movingCount)
        {
            if (klines == null || movingAvarages.Count == 0)
                return null;

            List<AdaptiveEnvelope> envelopes = new List<AdaptiveEnvelope>();
            //It's nesecary because atrs are deviation variables
            //(Optimization) Можемо замінити цей метод на зроблений GetRMA()
            //бо нічого не зміниться
            List<MovingAvarage> atrs = MovingAvarage.GetATRFromTV(klines, movingAvarages[0].Depth);

            AdaptiveEnvelope envelope;
            for (int a = 0; a < klines.Count; a++)
            {
                envelope = new AdaptiveEnvelope();
                envelope.Center = movingAvarages[a].Value;

                envelope.Id = klines[a].KlineId;
                envelope.NumberOfLines = movingCount;
                envelope.Depth = movingAvarages[a].Depth;
                envelope.DateTime = klines[a].DateTime;

                envelope.Low = new List<decimal>();
                envelope.High = new List<decimal>();
                for (int i = 1; i <= movingCount; i++)
                {
                    envelope.Low.Add(envelope.Center - atrs[a].Value * deviation * i);
                    envelope.High.Add(envelope.Center + atrs[a].Value * deviation * i);
                }

                envelopes.Add(envelope);
            }

            return envelopes;
        }
    }
}
