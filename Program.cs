using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Json;
using VBTBotConsole3;
using VBTBotConsole3.Controllers;
using VBTBotConsole3.Indicators;

namespace VTB
{
    class VTB
    {
        const string key = "s9DzJJks344g0geEfggj7rpEzkSIC8HHhAXLcRtgoc9z4ci277U9gNUNrwHGfKyJ";
        const string secret = "za8iGvSjOgkyZZQ3fyS3wtoNizQPrYYTiB1maR2hGTy12KgqrFYAiQ4caPRUfE3Q";

        private static async Task Main()
        {
            //Configuring Binance REST client for further use
            
            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new ApiCredentials(key, secret); // <- Provide you API key/secret in these fields to retrieve data related to your account
            });

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

            //Generic Host configuration

            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders(); // ❌ Remove all logging outputs (including console)
                })
                .ConfigureServices((context, services) =>
                {
                    // Add your EF Core context
                    services.AddDbContext<Model>(options =>
                    {
                        var dbPath = Path.Combine(AppContext.BaseDirectory, "local.db");
                        options.UseSqlite($"Data Source={dbPath}");
                    });

                    // Register controllers
                    services.AddScoped<ModelController>();
                    services.AddScoped<TradeController>();
                    services.AddScoped<IndicatorController>();
                    services.AddScoped<BinanceRestClient>();
                }).Build();

            using var scope = host.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<Model>();
            db.Database.Migrate();

            var tradeController = scope.ServiceProvider.GetRequiredService<TradeController>();
            var modelController = scope.ServiceProvider.GetRequiredService<ModelController>();

            Console.WriteLine("Starting trading session");
            Log.Information("Trading session started");
            tradeController.StartTrading();

            string line;
            do
            {
                line = Console.ReadLine();
            } while (line.ToUpper().Equals("STOP") || line.ToUpper().Equals("S"));

            tradeController.StopTrading();
            Log.Information("Trading session ended");

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
                        if(modelController.GetKlines().Any())
                        {
                            var klines = modelController.GetKlines().OrderBy(k => k.OpenTime);

                            foreach (var kline in klines)
                                Console.WriteLine(kline.ToString() + "\n");
                        }
                        else
                            Console.WriteLine("No data D:");

                        break;

                    case "clrdb":
                        //Get klines and remove them
                        modelController.ClearDatabase();
                        break;

                    case "dwnBNBUSDT":
                        //Use model function to download the data from Binance server
                        await modelController.InstallKlines();
                        break;

                    case "clr":
                        //Detouches sqlite database from migration
                        Console.WriteLine("Are you sure you want to disconect current database? (Y - yes / N - no");
                        string answer = Console.ReadLine();

                        if (answer.ToUpper().Equals("Y") || answer.ToUpper().Equals("YES"))
                            modelController.DetouchDatabase();
                        else
                            Console.WriteLine("Ok, we won't disconnect current database :)");
                        break;

                    case "getEMA":
                        try
                        {
                            Console.WriteLine("Write the depth of EMA you want to have? (BNBUSDT 1 hour)");
                            var depth = Convert.ToInt32(Console.ReadLine());

                            var emas = MovingAvarage.GetEMA(modelController.GetKlines(), depth);

                            foreach (var ema in emas)
                            {
                                Console.WriteLine(ema.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                        break;


                    case "updb":
                        Console.WriteLine("Updating BNBUSDC...");

                        await modelController.InstallKlines();
                        break;

                    case "shord":
                        tradeController.ShowBalance();
                        tradeController.ShowOpenPositions();
                        break;

                    case "open position":
                        try
                        {
                            BinanceFuturesOrder filledOrder;
                            Console.Write("Symbol for your order: ");
                            var symbol = Console.ReadLine();
                            Console.Write("Amount of your order: ");
                            var amount = Convert.ToDecimal(Console.ReadLine());
                            Console.WriteLine("Are you sure you want to put position with amount of: " + amount + "? (Y/N)");
                            answer = Console.ReadLine();

                            if (answer.ToLower().Equals("y") || answer.ToLower().Equals("yes"))
                                tradeController.OpenMarketShortPosition(symbol, amount);
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
                            var symbol = Console.ReadLine();
                            var amount = Convert.ToDecimal(Console.ReadLine());
                            Console.WriteLine("Are you sure you want to put position with amount of: " + amount + "? (Y/N)");
                            answer = Console.ReadLine();
                            if (answer.ToLower().Equals("y") || answer.ToLower().Equals("yes"))
                                tradeController.CloseMarketShortPosition(symbol, amount);
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
                        tradeController.StartTrading();
                        answer = "";
                        while (!answer.ToUpper().Equals("STOP"))
                        {
                            answer = Console.ReadLine();
                            if (!answer.ToUpper().Equals("STOP"))
                                Console.WriteLine("Write 'stop' to stop trading");
                        }
                        tradeController.StopTrading();
                        Console.WriteLine("You have stoped trading. Write start to continue ^_^");
                        break;

                    case "chinter":
                        Console.WriteLine("Write an interval in wich price will be checked:");
                        double interval = Convert.ToDouble(Console.ReadLine());
                        tradeController.TimerInterval = interval;
                        Console.WriteLine("Interval of " + interval + " ms is set.");
                        break;

                    case "show orders history":
                        try
                        {
                            var orders = modelController.GetOrders();
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
                            var orders = modelController.GetOrdersAbove();
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
                        modelController.ClearBinanceFuturesOrders();
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

                            modelController.WriteNewOrderDown(order);
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

                            modelController.WriteNewOrderDown(order);
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
