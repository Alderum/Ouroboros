using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Binance.Net.Enums;
using Timer = System.Timers.Timer;
using System.Timers;
using System;
using Binance.Net.Objects.Models.Futures;
using VBTBotConsole3.Indicators;

namespace VBTBotConsole3.Controllers
{
    class TradeController
    {
        #region Properties
        Timer timer = new Timer();
        public double CheckingInterval {
            get
            {
                return timer.Interval;
            }
            set
            {
                timer.Interval = value;
            }
        }

        Controller controller = new Controller();
        Indicator indicator;
        #endregion

        //Writing API Binance public and secret keys
        public TradeController(string publicKey, string secretKey, Controller controller)
        {
            //Initializig REST client
            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials(publicKey, secretKey); // <- Provide you API key/secret in these fields to retrieve data related to your account
            });

            //Timer tick event
            timer.Elapsed += new ElapsedEventHandler(Tick);
            timer.Interval = 10000;

            this.controller = controller;
            indicator = new Indicator(controller);
        }

        #region Methods

        public async void ShowBalance()
        {
            var restClient = new BinanceRestClient();
            var balance = await restClient.UsdFuturesApi.Account.GetBalancesAsync();
            Console.WriteLine("Balance: " + balance.Data.MaxBy(k => k.AvailableBalance).WalletBalance);
        }

        public async void Tick(object source, ElapsedEventArgs e)
        {
            //Updating database for actual market
            await controller.ModelController.UpdateSymbol("BNBUSDT", KlineInterval.OneHour);

            //Is it good to enter long position
            bool enterSignal = indicator.CheckMarketForShort();
            Console.WriteLine("ID of current thread: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("Is it good to enter short now: " + enterSignal);
            if (enterSignal)
            {
                OpenMarketShortPosition("BNBUSDC", (decimal)0.01);
            }

            //Is it good to quit short position
            bool quitSignal = indicator.CheckMarketForLong();
            Console.WriteLine("Is it good to quit short now: " + quitSignal);
            if (quitSignal)
            {
                var ordersAbove = from o in controller.ModelController.GetAllFuturesOrdersAbove()
                                  where o.Side == OrderSide.Sell select o;

                decimal quantity = 0;
                foreach (var order in ordersAbove)
                {
                    quantity += order.Quantity;
                }

                if(quantity != 0)
                    await CloseMarketShortPosition("BNBUSDC", quantity);

                controller.ModelController.ClearBinanceFuturesOrders(ordersAbove.ToList());
            }
        }

        public bool IsClientAuthenticated()
        {
            var restClient = new BinanceRestClient();
            var isAuthenticated = restClient.SpotApi.Authenticated;
            return isAuthenticated;
        }

        #region Positions methods

        public async void ShowOpenPositions()
        {
            var restClient = new BinanceRestClient();
            var openOrders = await restClient.UsdFuturesApi.Trading.GetPositionsAsync();
            var openOrdersList = openOrders.Data.ToList();
            foreach(var o in openOrdersList)
            {
                Console.WriteLine("Position on " + o.Symbol + " with margin " + o.InitialMargin + " of " + o.MarginAsset + " with liquidation on " + o.LiquidationPrice);
            }
        }

        public async void TryOpenLimitPosition()
        {
            var restClient = new BinanceRestClient();
            var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync("BNBUSDT", OrderSide.Sell, FuturesOrderType.Limit, (decimal)0.01, 640, PositionSide.Short, TimeInForce.GoodTillCanceled);
            if(result.Error != null)
                Console.WriteLine("Error: " + result.Error);
            else
                Console.WriteLine("Order success: " + result.Success);
        }

        public async void OpenMarketShortPosition(string symbol, decimal amount)
        {
            var restClient = new BinanceRestClient();
            var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Market, amount, null, PositionSide.Short);
            result.Data.Price = restClient.SpotApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price;
            if (result.Error != null)
                Console.WriteLine("Error: " + result.Error);
            else
            {
                Console.WriteLine("Order price: " + result.Data.Price);
                await controller.ModelController.WriteNewOrderDown(result.Data);
                Console.WriteLine("Order success: " + result.Success);
            }
        }

        public async Task CloseMarketShortPosition(string symbol, decimal amount)
        {
            var restClient = new BinanceRestClient();
            var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Market, amount, null, PositionSide.Short);
            result.Data.Price = restClient.SpotApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price;

            if (result.Error != null)
                Console.WriteLine("Error: " + result.Error);
            else
            {
                Console.WriteLine("Order price: " + result.Data.Price);
                await controller.ModelController.WriteNewOrderDown(result.Data);
                Console.WriteLine("Order success: " + result.Success);
            }
        }

        #endregion

        public void StartTrading()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            timer.Start();
        }

        public void StopTrading()
        {
            timer.Stop();
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion
    }
}
