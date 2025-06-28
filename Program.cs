using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Serilog;
using Serilog.Formatting.Json;
using VBTBotConsole3;
using VBTBotConsole3.Controllers;

namespace VTB
{
    class VTB
    {
        const string secretTurbo = "Wgd99je2GzPR6Cm31EWGvz4J9SpmETqrTjCD0JnLhpTZX3rTqymbzqO4EDYHxQH9";
        const string keyTurbo = "CNoerwrmrQcE6N3VYufth43K8DcukFKlUKTbHuSGBolKDNDzgw0uJ4yFsbP62KhG";
        const string key = "s9DzJJks344g0geEfggj7rpEzkSIC8HHhAXLcRtgoc9z4ci277U9gNUNrwHGfKyJ";
        const string secret = "za8iGvSjOgkyZZQ3fyS3wtoNizQPrYYTiB1maR2hGTy12KgqrFYAiQ4caPRUfE3Q";

        private static async Task Main()
        {
            //Configuring Serilog for JSON error logging

            var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "log.json");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    new JsonFormatter(),
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7
                )
                .CreateLogger();

            using (var db = new Model())
            {
                db.Database.EnsureCreated();
            }

            Controller controller = new Controller(key, secret);

            //Console.WriteLine("Starting trading session");
            //controller.TradeController.StartTrading();

            #region CommandLine
            string command = "";
            do
            {
                command = Console.ReadLine();
                switch (command)
                {
                    case "help":
                        Console.WriteLine("Commands: ");
                        Console.WriteLine("sdb: see database");
                        Console.WriteLine("dwnBNBUSDT:  download info about last candle in BNBUSDT.");
                        Console.WriteLine("clrdb:   clear database");
                        Console.WriteLine("clr: deletes databases data");
                        Console.WriteLine("getEMA:  get exponetial moving avarage");
                        Console.WriteLine("shord: show all orders information");
                        Console.WriteLine("updb: updates database klines");
                        Console.WriteLine("open position: opens position for specific symbol");
                        Console.WriteLine("close position: closes position for specific symbol");
                        Console.WriteLine("chinter: chenges the interval of trader");
                        Console.WriteLine("start: starts trading session");
                        break;

                    case "sdb":
                        //Get klines and show them

                        var klines = controller.ModelController.Klines;

                        for (int i = 0; i < klines.Count; i++)
                        {
                            Console.WriteLine("Kline Id: " + klines[i].KlineId
                                + " High: " + klines[i].HighPrice +
                                 " Open: " + klines[i].OpenPrice +
                                 " Close: " + klines[i].ClosePrice +
                                 " Low: " + klines[i].LowPrice +
                                 " Interval: " + (klines[i].CloseTime - klines[i].OpenTime) +
                                 " Date: " + klines[i].OpenTime);
                        }
                        break;

                    case "clrdb":
                        //Get klines and remove them
                        controller.ModelController.ClearDatabase();
                        break;

                    case "dwnBNBUSDT":
                        //Use model function to download the data from Binance server
                        await controller.ModelController.WriteDownSymbolInfo("BNBUSDT", KlineInterval.OneHour);
                        break;

                    case "clr":
                        //Detouches sqlite database from migration
                        Console.WriteLine("Are you sure you want to disconect current database? (Y - yes / N - no");
                        string answer = Console.ReadLine();

                        if (answer.ToUpper().Equals("Y") || answer.ToUpper().Equals("YES"))
                            controller.ModelController.DetouchDatabase();
                        else
                            Console.WriteLine("Ok, we won't disconnect current database :)");
                        break;

                    case "getEMA":
                        try
                        {
                            Console.WriteLine("Write the depth of EMA you want to have? (BNBUSDT 1 hour)");
                            var depth = Convert.ToInt32(Console.ReadLine());

                            var emas = controller.GetEMAOfAllCandles(depth);

                            for (int i = 0; i < emas.Count; i++)
                            {
                                Console.WriteLine("EMA id: " + emas[i].Id +
                                    " Value: " + emas[i].Value +
                                    " Data: " + emas[i].DateTime);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                        break;


                    case "updb":
                        Console.WriteLine("Write symbol:");
                        var symbol = Console.ReadLine();

                        await controller.ModelController.UpdateSymbol(symbol, KlineInterval.OneHour);
                        break;

                    case "shord":
                        controller.TradeController.ShowBalance();
                        controller.TradeController.ShowOpenPositions();
                        break;

                    case "open position":
                        try
                        {
                            BinanceFuturesOrder filledOrder;
                            Console.Write("Symbol for your order: ");
                            symbol = Console.ReadLine();
                            Console.Write("Amount of your order: ");
                            var amount = Convert.ToDecimal(Console.ReadLine());
                            Console.WriteLine("Are you sure you want to put position with amount of: " + amount + "? (Y/N)");
                            answer = Console.ReadLine();

                            if (answer.ToLower().Equals("y") || answer.ToLower().Equals("yes"))
                                controller.TradeController.OpenMarketShortPosition(symbol, amount);
                            else
                                break;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                        break;

                    case "close position":
                        try
                        {
                            symbol = Console.ReadLine();
                            var amount = Convert.ToDecimal(Console.ReadLine());
                            Console.WriteLine("Are you sure you want to put position with amount of: " + amount + "? (Y/N)");
                            answer = Console.ReadLine();
                            if (answer.ToLower().Equals("y") || answer.ToLower().Equals("yes"))
                                controller.TradeController.CloseMarketShortPosition(symbol, amount);
                            else
                                break;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }

                        break;

                    case "start":
                        Console.WriteLine("To end the trading session enter 'stop'");
                        controller.TradeController.StartTrading();
                        answer = "";
                        while (!answer.ToUpper().Equals("STOP"))
                        {
                            answer = Console.ReadLine();
                            if (!answer.ToUpper().Equals("STOP"))
                                Console.WriteLine("Write 'stop' to stop trading");
                        }
                        controller.TradeController.StopTrading();
                        Console.WriteLine("You have stoped trading. Write start to continue ^_^");
                        break;

                    case "chinter":
                        Console.WriteLine("Write an interval in wich price will be checked:");
                        double interval = Convert.ToDouble(Console.ReadLine());
                        controller.TradeController.CheckingInterval = interval;
                        Console.WriteLine("Interval of " + interval + " ms is set.");
                        break;

                    case "show orders history":
                        try
                        {
                            var orders = controller.ModelController.Orders;
                            ShowOrders(orders);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e.Message);
                        }
                        break;

                    case "show above orders":
                        try
                        {
                            var orders = controller.ModelController.GetAllFuturesOrdersAbove();
                            ShowOrders(orders);
                            decimal quantity = 0;
                            foreach (var order in orders)
                            {
                                quantity += order.Quantity;
                            }

                            Console.WriteLine("Cumulative quantity: " + quantity);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e.Message);
                        }
                        break;

                    case "delete orders history":
                        controller.ModelController.ClearBinanceFuturesOrders();
                        Console.WriteLine("Orders history deleted )))");
                        break;

                    case "create new fictional order":
                        try
                        {
                            var client = new BinanceRestClient();
                            var order = client.UsdFuturesApi.Trading.PlaceOrderAsync("BNBUSDC", OrderSide.Sell, FuturesOrderType.Market, (decimal)0.02, null, PositionSide.Short).Result.Data;
                            Console.Write("Write price: ");
                            order.Price = Convert.ToDecimal(Console.ReadLine());

                            Console.WriteLine("Order quantity: " + order.Quantity);
                            Console.WriteLine("Order symbol: " + order.Symbol);
                            Console.WriteLine("Order price: " + order.Price);

                            controller.ModelController.WriteNewOrderDown(order);
                            Console.WriteLine("Order is written down to databse :)");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error: " + e.Message);
                        }
                        break;

                    case "exp":
                        #region Some experimental stuff

                        try
                        {
                            var client = new BinanceRestClient();
                            var order = client.UsdFuturesApi.Trading.PlaceOrderAsync("BNBUSDC", OrderSide.Sell, FuturesOrderType.Market, (decimal)0.02, null, PositionSide.Short).Result.Data;
                            Console.Write("Write price: ");
                            order.Price = Convert.ToDecimal(Console.ReadLine());

                            Console.WriteLine("Order quantity: " + order.Quantity);
                            Console.WriteLine("Order symbol: " + order.Symbol);
                            Console.WriteLine("Order price: " + order.Price);

                            controller.ModelController.WriteNewOrderDown(order);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Error: " + e.Message);
                        }

                        #endregion
                        break;

                    default:
                        Console.WriteLine("There is no such command. Write help to get info about existing commands.");
                        break;
                }
            } while (command != "stop");
            #endregion
        }

        #region Methods

        public static void ShowOrders(List<BinanceFuturesOrder> orders)
        {
            foreach (var order in orders)
            {
                Console.WriteLine("Order symbol: " + order.Symbol);
                Console.WriteLine("Order price: " + order.Price);
                Console.WriteLine("Order quantity: " + order.Quantity);
                Console.WriteLine("Order date: " + order.UpdateTime);
                Console.WriteLine("Order position side: " + order.PositionSide);
                Console.WriteLine("Order side: " + order.Side);
            }
        }

        public static void ShowKline(Kline kline)
        {
            Console.WriteLine("Kline close: " + kline.ClosePrice);
            Console.WriteLine("Kline date: " + kline.OpenTime);
        }

        public static void ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
        
        #endregion
    }
}
