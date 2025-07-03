using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Binance.Net.Enums;
using System.Timers;
using Serilog;

using Timer = System.Timers.Timer;
using Program = VTB.VTB;

namespace VBTBotConsole3.Controllers
{
    static class TradeController
    {
        #region Properties
        static Timer timer = new Timer()
        {
            Interval = 10000
        };
        public static double CheckingInterval
        {
            get
            {
                return timer.Interval;
            }
            set
            {
                timer.Interval = value;
            }
        }

        #endregion

        #region Methods

        public static async void ShowBalance()
        {
            try
            {
                //Asking Binance server about current account balances and showing it
                var restClient = new BinanceRestClient();
                var balance = await restClient.UsdFuturesApi.Account.GetBalancesAsync();
                Program.ShowMessage("Balance: " + balance.Data.MaxBy(k => k.AvailableBalance).WalletBalance);
            }
            catch (Exception e)
            {
                Log.Error(e, 
                    "An error occured while processing an request to " +
                    "Binance REST service in order to get client balance.");
                Program.ShowMessage("Error in ShowBalance: " + e.Message);
            }
        }

        static async Task Tick(object source, ElapsedEventArgs args)
        {
            try
            {
                //Updating database for actual market
                await ModelController.InstallKlines();

                //Is it good to enter long position
                bool enterSignal = IndicatorController.CheckMarketForShort();
                Program.ShowMessage("ID of current thread: " + Thread.CurrentThread.ManagedThreadId);
                Program.ShowMessage("Is it good to enter short now: " + enterSignal);
                if (enterSignal)
                {
                    OpenMarketShortPosition("BNBUSDC", (decimal)0.01);
                }

                //Is it good to quit short position
                bool quitSignal = IndicatorController.CheckMarketForLong();
                Program.ShowMessage("Is it good to quit short now: " + quitSignal);
                if (quitSignal)
                {
                    var ordersAbove = from o in ModelController.GetAllFuturesOrdersAbove()
                                      where o.Side == OrderSide.Sell
                                      select o;

                    //If there are orders above add their quantities
                    decimal quantity = 0;
                    foreach (var order in ordersAbove)
                    {
                        quantity += order.Quantity;
                    }

                    //If there is no orders above do nothing
                    if (quantity != 0)
                        await CloseMarketShortPosition("BNBUSDC", quantity);

                    ModelController.ClearBinanceFuturesOrders(ordersAbove.ToList());
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while procedeing a market check.");
                Program.ShowMessage(string.Format("Error in Tick: {0}", e.Message));
            }
        }

        //Checking is client authenticated or not with BinanceRestClient
        public static bool IsClientAuthenticated()
        {
            try
            {
                var restClient = new BinanceRestClient();
                var isAuthenticated = restClient.SpotApi.Authenticated;
                return isAuthenticated;
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while checking is client authenticated or not.");
                Program.ShowMessage(string.Format("Error in IsClientAuthenticated: {0}", e.Message));
                return false;
            }
        }

        #region Positions methods

        public static async void ShowOpenPositions()
        {
            try
            {
                var restClient = new BinanceRestClient();
                var openOrders = await restClient.UsdFuturesApi.Trading.GetPositionsAsync();
                var openOrdersList = openOrders.Data.ToList();
                foreach (var o in openOrdersList)
                {
                    Program.ShowMessage("Position on " + o.Symbol + " with margin " + o.InitialMargin + " of " + o.MarginAsset + " with liquidation on " + o.LiquidationPrice);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while requesting positions of the user.");
                Program.ShowMessage(string.Format("Error in ShowOpenPositions: {0}", e.Message));
            }
        }

        public static async void TryOpenLimitPosition()
        {
            try
            {
                var restClient = new BinanceRestClient();
                var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync("BNBUSDT", OrderSide.Sell, FuturesOrderType.Limit, (decimal)0.01, 640, PositionSide.Short, TimeInForce.GoodTillCanceled);
                if (result.Error != null)
                    Program.ShowMessage("Binance error: " + result.Error);
                else
                    Program.ShowMessage("Order success: " + result.Success);
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while trying to open a limit position");
                Program.ShowMessage(string.Format("Error in TryOpenPositions: {0}", e.Message));
            }
        }

        public static async Task OpenMarketShortPosition(string symbol, decimal amount)
        {
            try
            {
                var restClient = new BinanceRestClient();
                var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Market, amount, null, PositionSide.Short);
                result.Data.Price = restClient.SpotApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price;
                if (result.Error != null)
                    Program.ShowMessage("Binance error: " + result.Error);
                else
                {
                    Program.ShowMessage("Order price: " + result.Data.Price);
                    await ModelController.WriteNewOrderDown(result.Data);
                    Program.ShowMessage("Order success: " + result.Success);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while openning a market short position.");
                Program.ShowMessage(string.Format("Error in OpenMarketShortPosition: {0}", e.Message));
            }
        }

        public static async Task CloseMarketShortPosition(string symbol, decimal amount)
        {
            try
            {
                var restClient = new BinanceRestClient();
                var result = await restClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Market, amount, null, PositionSide.Short);
                result.Data.Price = restClient.SpotApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price;

                if (result.Error != null)
                    Program.ShowMessage("Binance error: " + result.Error);
                else
                {
                    Program.ShowMessage("Order price: " + result.Data.Price);
                    await ModelController.WriteNewOrderDown(result.Data);
                    Program.ShowMessage("Order success: " + result.Success);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An exception occured while closeing a market short position.");
                Program.ShowMessage(string.Format("Error in CloseMarketShortPosition: {0}", e.Message));
            }
        }

        #endregion

        public static void StartTrading()
        {
            //Timer tick event
            timer.Elapsed += async (s, e) => await Tick(s, e);
            timer.Interval = CheckingInterval;

            Console.ForegroundColor = ConsoleColor.Green;
            timer.Start();
        }

        public static void StopTrading()
        {
            timer.Stop();
            Console.ForegroundColor = ConsoleColor.White;
        }
        #endregion
    }
}
