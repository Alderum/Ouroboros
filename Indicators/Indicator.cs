using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net;
using VBTBotConsole3.Analitics;
using VBTBotConsole3.Controllers;
using IPosition = VBTBotConsole3.Analitics.IPosition;

namespace VBTBotConsole3.Indicators
{
    class Indicator
    {
        #region Field and properties

        Controller controller;
        List<Kline> klines;
        List<MovingAvarage> emas;
        List<AdaptiveEnvelope> envelopes;
        List<BinanceFuturesOrder> orders;

        #endregion

        public Indicator(Controller controller)
        {
            //Get list of klines needed to create indicator
            klines = controller.ModelController.Klines;

            //Found a center moving avarage for adaptive envelope
            emas = MovingAvarage.GetEMA(klines, 21);

            //Creating list of adaptive envelopes
            envelopes = AdaptiveEnvelope.GetAdaptiveEnvelope(klines, emas, 2, 2);

            //Getting opened orders to make sure, that distance between orders is enough
            orders = controller.ModelController.Orders;

            this.controller = controller;
        }

        #region Methods

        public bool CheckMarketForShort()
        {
            bool positiveSignal = false;

            //Ubdate data from database
            DataUpdate();

            var lastKline = klines.LastOrDefault();
            var lastEnvelope = envelopes.LastOrDefault();

            //If there is no orders in databse we have to make sure, that it works
            IEnumerable<BinanceFuturesOrder> sellOrders = null;
            BinanceFuturesOrder lastOrder = null;

            //Delta for orders, orders canno't be close to each other
            decimal priceDifference = 31;

            //Is there any orders in the databse?
            if (orders.ValidateNotNull != null)
            {
                sellOrders = from o in orders where o.Side == OrderSide.Buy select o;

                //Is there any buy orders in the database?
                if (sellOrders.ValidateNotNull != null)
                {
                    lastOrder = sellOrders.LastOrDefault();
                    if(lastOrder != null)
                        priceDifference = Math.Abs(lastKline.Close - lastOrder.Price);
                }
            }

            Console.WriteLine("Price difference short: " + priceDifference);

            if (lastKline.High >= lastEnvelope.High[0] && lastKline.Low <= lastEnvelope.High[0]
                && priceDifference >= 5)
                positiveSignal = true;

            return positiveSignal;
        }

        public bool CheckMarketForLong()
        {
            bool positiveSignal = false;

            //Update data from database
            DataUpdate();

            var lastKline = klines.LastOrDefault();
            var lastEnvelope = envelopes.LastOrDefault();

            //If there is no orders in databse we have to make sure, that it works
            IEnumerable<BinanceFuturesOrder> buyOrders = null;
            BinanceFuturesOrder lastOrder = null;

            //Delta for orders, orders canno't be close to each other
            decimal priceDifference = 31;

            //Is there any orders in the databse?
            if (orders.ValidateNotNull != null)
            {
                buyOrders = from o in orders where o.Side == OrderSide.Buy select o;

                //Is there any buy orders in the database?
                if(buyOrders.ValidateNotNull != null)
                {
                    lastOrder = buyOrders.LastOrDefault();
                    
                    if(lastOrder != null)
                        priceDifference = Math.Abs(lastKline.Close - lastOrder.Price);
                }
            }

            Console.WriteLine("Price difference short: " + priceDifference);

            if (lastKline.High >= lastEnvelope.Low[0] && lastKline.Low <= lastEnvelope.Low[0]
                && priceDifference >= 5)
                positiveSignal = true;

            return positiveSignal;
        }

        void DataUpdate()
        {
            //Get list of klines needed to create indicator
            klines = controller.ModelController.Klines;

            //Found a center moving avarage for adaptive envelope
            emas = MovingAvarage.GetEMA(klines, 21);

            //Creating list of adaptive envelopes
            envelopes = AdaptiveEnvelope.GetAdaptiveEnvelope(klines, emas, 2, 2);

            //Getting opened orders to make sure, that distance between orders is enough
            orders = controller.ModelController.Orders;
        }

        #endregion

        #region Analitics
        public void ColisionWithAdaptiveEnvelope()
        {
            //Time gap from wich klines are being taken
            klines = klines.FindAll(k => k.DateTime >= DateTime.Now - new TimeSpan(90, 0, 0, 0));

            //Here we use list of our interface for better analitics
            List<IPosition> positions = new List<IPosition>();

            foreach (Kline k in klines)
            {
                decimal lowEnvelope;
                decimal highEnvelope;
                decimal centerEnvelope;

                //Checking center
                centerEnvelope = envelopes[k.KlineId - 1].Center;
                if (k.High >= centerEnvelope && k.Low <= centerEnvelope)
                {
                    //CloseBuyPositions(positions, k);
                }
                
                //Check low and high
                for (int i = 0; i < envelopes[0].NumberOfLines; i++)
                {
                    //Checking low
                    lowEnvelope = envelopes[k.KlineId - 1].Low[i];

                    if (k.High > lowEnvelope && k.Close <= lowEnvelope && i == 1)
                    {
                        CloseSellPositions(positions, k);
                    }

                    if (k.Low < lowEnvelope && k.Close >= lowEnvelope && i == 1)
                    {
                        CloseSellPositions(positions, k);
                    }

                    //Checking high
                    highEnvelope = envelopes[k.KlineId - 1].High[i];

                    if (k.High > highEnvelope && k.Close <= highEnvelope)
                    {
                        OpenSellPositions(positions, k);
                    }

                    //Date gap
                    var openPositions = from p in positions where p.Completed == false && p as SellPosition != null select p;
                    if (openPositions.Count() != 0)
                    {
                        openPositions.OrderBy(p => p.EntryDateTime);
                        var latestEntry = openPositions.First();
                        if (latestEntry.EntryDateTime.CompareTo(k.DateTime - new TimeSpan(1, 0, 0, 0)) < 0)
                        {
                            CloseSellPositions(positions, k);
                        }
                    }

                }
            }

            #region View all position history
            /*
            foreach (IPosition p in positions)
            {
                if (p as SellPosition != null)
                    Console.WriteLine("Sell");
                else if (p as BuyPosition != null)
                    Console.WriteLine("Buy");

                Console.WriteLine("Entry time: " + p.EntryDateTime + " Close time: " + p.CloseDateTime);
                Console.WriteLine("Entry price: " + p.EntryPrice + " Close price: " + p.ClosePrice);
                Console.WriteLine("PNL: " + p.Value);
            }
            */
            #endregion

            //Here we have actual analitics on selected data
            decimal statPNL = 0;
            int sellPosNumb = 0;
            int buyPosNumb = 0;
            int winNumb = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                //If position isn't opened
                if (positions[i].ClosePrice != 0)
                    statPNL += positions[i].Value;

                //Short and long ratio
                if (positions[i] as BuyPosition != null)
                    buyPosNumb++;
                else if (positions[i] as SellPosition != null)
                    sellPosNumb++;

                //Wining position number
                if (positions[i].Value > 0)
                    winNumb++;
            }

            Console.WriteLine("Overall PNL: " + statPNL);
            Console.WriteLine("Sell position number: " + sellPosNumb);
            Console.WriteLine("Buy position number: " + buyPosNumb);
            Console.WriteLine("Win percentage: " + winNumb / (decimal)positions.Count);

            #region Order statistics

            decimal avarageLosOnBuy = 0;
            decimal avarageLosOnSell = 0;
            TimeSpan avarageLosDurationOnBuy = new TimeSpan();
            TimeSpan avarageLosDurationOnSell = new TimeSpan();
            int losBuyPositionNumber = 0;
            int losSellPositionNumber = 0;
            foreach (IPosition p in positions)
            {
                if (p as SellPosition != null && p.Value <= 0)
                {
                    losSellPositionNumber++;
                    avarageLosOnSell += p.Value;
                    avarageLosDurationOnSell = p.CloseDateTime - p.EntryDateTime;
                }
                if (p as BuyPosition != null && p.Value <= 0)
                {
                    losBuyPositionNumber++;
                    avarageLosOnBuy += p.Value;
                    avarageLosDurationOnBuy = p.CloseDateTime - p.EntryDateTime;
                }
            }
            if(losBuyPositionNumber != 0)
                avarageLosOnBuy /= losBuyPositionNumber;
            avarageLosOnSell /= losSellPositionNumber;

            Console.WriteLine("Los sell position number: " + losSellPositionNumber);
            Console.WriteLine("Avarage los on sell position: " + avarageLosOnSell);
            Console.WriteLine("Avarage los on sell position duration: " + avarageLosDurationOnSell.ToString());

            Console.WriteLine("Los buy position number: " + losBuyPositionNumber);
            Console.WriteLine("Avarage los on buy position: " + avarageLosOnBuy);
            Console.WriteLine("Avarage los on buy position duration: " + avarageLosDurationOnBuy.ToString());

            decimal avarageWinOnBuy = 0;
            decimal avarageWinOnSell = 0;
            TimeSpan avarageWinDurationOnBuy = new TimeSpan();
            TimeSpan avarageWinDurationOnSell = new TimeSpan();
            int winBuyPositionNumber = 0;
            int winSellPositionNumber = 0;
            foreach (IPosition p in positions)
            {
                if (p as SellPosition != null && p.Value > 0)
                {
                    winSellPositionNumber++;
                    avarageWinOnSell += p.Value;
                    avarageWinDurationOnSell = p.CloseDateTime - p.EntryDateTime;
                }
                if (p as BuyPosition != null && p.Value > 0)
                {
                    winBuyPositionNumber++;
                    avarageWinOnBuy += p.Value;
                    avarageWinDurationOnBuy = p.CloseDateTime - p.EntryDateTime;
                }
            }
            if(winBuyPositionNumber != 0)
                avarageWinOnBuy /= winBuyPositionNumber;
            avarageWinOnSell /= winSellPositionNumber;

            Console.WriteLine("Win sell position number: " + winSellPositionNumber);
            Console.WriteLine("Avarage win on sell position: " + avarageWinOnSell);
            Console.WriteLine("Avarage win on sell position duration: " + avarageWinDurationOnSell.ToString());

            Console.WriteLine("Win buy position number: " + winBuyPositionNumber);
            Console.WriteLine("Avarage win on buy position: " + avarageWinOnBuy);
            Console.WriteLine("Avarage win on buy position duration: " + avarageWinDurationOnBuy.ToString());

            #endregion
        }

        void CloseSellPositions(List<IPosition> positions, Kline kline)
        {
            var openPositions = from p in positions where p.Completed == false && p as SellPosition != null select p;
            
            foreach(var p in openPositions)
            {
                p.ClosePrice = kline.Close;
                p.CloseDateTime = kline.DateTime + new TimeSpan(0, 0, 0, (int)kline.Interval);
                p.Completed = true;
            }
        }

        void CloseBuyPositions(List<IPosition> positions, Kline kline)
        {
            var openPositions = from p in positions where p.Completed == false && p as BuyPosition != null select p;

            foreach (var p in openPositions)
            {
                p.ClosePrice = kline.Close;
                p.CloseDateTime = kline.DateTime + new TimeSpan(0, 0, 0, (int)kline.Interval);
                p.Completed = true;
            }
        }

        void OpenSellPositions(List<IPosition> positions, Kline k)
        {
            SellPosition sell = new SellPosition()
            {
                EntryPrice = k.Close,
                EntryDateTime = k.DateTime + new TimeSpan(0, 0, 0, (int)k.Interval)
            };

            positions.Add(sell);
        }

        void OpenBuyPositions(List<IPosition> positions, Kline k)
        {
            BuyPosition buy = new BuyPosition()
            {
                EntryPrice = k.Close,
                EntryDateTime = k.DateTime + new TimeSpan(0, 0, 0, (int)k.Interval)
            };

            positions.Add(buy);
        }
        #endregion


    }
}
