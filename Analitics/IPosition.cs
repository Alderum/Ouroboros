namespace VBTBotConsole3.Analitics
{
    interface IPosition
    {
        public decimal Value { get; }
        public decimal EntryPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime EntryDateTime { get; set; }
        public DateTime CloseDateTime { get; set; }
        public bool Completed { get; set; }
    }
}
