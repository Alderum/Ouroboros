using Binance.Net.Enums;
using Serilog;
using VBTBotConsole3.Indicators;

using Program = VTB.VTB;

namespace VBTBotConsole3.Controllers
{
    class IndicatorController
    {
        #region Field and properties

        private readonly ModelController modelController;

        List<MovingAvarage> emas;
        List<AdaptiveEnvelope> envelopes;

        #endregion

        public IndicatorController(ModelController modelController)
        {
            this.modelController = modelController;

            emas = MovingAvarage.GetEMA(modelController.GetKlines(), 21);
            envelopes = AdaptiveEnvelope.GetAdaptiveEnvelope(modelController.GetKlines(), emas, 2, 2);
        }

        #region Methods

        public bool CheckMarketForShort()
        {
            bool positiveSignal = false;

            //Ubdate data from database
            DataUpdate();

            var lastKline = modelController.GetKlines().Last();
            var lastEnvelope = envelopes.Last();

            //Delta for orders, orders canno't be close to each other
            decimal priceDifference = 31;

            //Is there any orders in the databse?
            if (modelController.GetOrders().Any())
            {
                var sellOrders = from o in modelController.GetOrders() where o.Side == OrderSide.Sell select o;

                //Is there any buy orders in the database?
                if (sellOrders.Any())
                {
                    var lastOrder = sellOrders.Last();

                    priceDifference = Math.Abs(lastKline.ClosePrice - lastOrder.Price);
                }
                else
                    Program.ShowMessage("There is no sell orders");
            }
            else
                Program.ShowMessage("There is no orders");

            if (lastKline.HighPrice >= lastEnvelope.High[0]
                && lastKline.LowPrice <= lastEnvelope.High[0]
                && priceDifference >= 5)
            {
                positiveSignal = true;
                Log.Information("A good short oportunity");
            }

            return positiveSignal;
        }

        public bool CheckMarketForLong()
        {
            bool positiveSignal = false;

            //Update data from database
            DataUpdate();

            var lastKline = modelController.GetKlines().Last();
            var lastEnvelope = envelopes.Last();

            //Delta for orders, orders canno't be close to each other
            decimal priceDifference = 31;

            //Is there any orders in the databse?
            if (modelController.GetOrders().Any())
            {
                var buyOrders = from o in modelController.GetOrders() where o.Side == OrderSide.Buy select o;

                //Is there any buy orders in the database?
                if (buyOrders.Any())
                {
                    var lastOrder = buyOrders.Last();

                    priceDifference = Math.Abs(lastKline.ClosePrice - lastOrder.Price);
                }
                else
                    Program.ShowMessage("There is no buy orders");
            }
            else
                Program.ShowMessage("There is no orders");

            if (lastKline.HighPrice >= lastEnvelope.Low[0]
                && lastKline.LowPrice <= lastEnvelope.Low[0]
                && priceDifference >= 5)
            {
                positiveSignal = true;
                Log.Information("A good long oportunity");
            }

            return positiveSignal;
        }

        void DataUpdate()
        {
            //Found a center moving avarage for adaptive envelope
            emas = MovingAvarage.GetEMA(modelController.GetKlines(), 21);

            //Creating list of adaptive envelopes
            envelopes = AdaptiveEnvelope.GetAdaptiveEnvelope(modelController.GetKlines(), emas, 2, 2);
        }

        #endregion
    }
}
