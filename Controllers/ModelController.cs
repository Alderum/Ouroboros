using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.Tasks;
using Program = VTB.VTB;

namespace VBTBotConsole3.Controllers
{
    class ModelController
    {
        #region Fields and properties

        List<BinanceFuturesOrder> orders;
        public List<BinanceFuturesOrder> Orders {
            get
            {
                return orders;
            }
        }

        List<Kline> klines;
        public List<Kline> Klines
        {
            get
            {
                return GetListOfAllAvailableKlines();
            }
        }

        #endregion

        public ModelController()
        {
            orders = GetBinanceFuturesOrders();
            klines = GetListOfAllAvailableKlines();
        }

        List<Kline> GetListOfAllAvailableKlines()
        {
            //Get Klines from the database
            using var model = new Model();
            try
            {
                List<Kline> klines = model.Klines.OrderBy(k => k.KlineId).ToList();
                return klines;
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while getting list of all available klines from the database.");
                Program.ShowMessage("DB error: " + e.Message);
            }

            return null;
        }

        public async void ClearDatabase()
        {
            using var model = new Model();

            var klines = await model.Klines
                            .OrderBy(k => k.KlineId).ToListAsync();

            model.Klines.RemoveRange(klines);
            await model.SaveChangesAsync();

            //Updating list of klines for accurate property
            klines = GetListOfAllAvailableKlines();
        }

        public void DetouchDatabase()
        {
            using var model = new Model();

            model.ChangeTracker.Clear();
            //Updating list of klines for accurate property
            klines = null;
        }

        public async Task WriteNewOrderDown(BinanceFuturesOrder order)
        {
            using var model = new Model();

            model.BinanceFuturesOrders.Add(order);
            await model.SaveChangesAsync();

            //Updating order list for accurate property manifestation
            orders = GetBinanceFuturesOrders();
        }

        List<BinanceFuturesOrder> GetBinanceFuturesOrders()
        {
            using var model = new Model();

            try
            {
                List<BinanceFuturesOrder> orders = model.BinanceFuturesOrders.OrderBy(k => k.CreateTime).ToList();
                
                if (orders != null)
                    return orders;
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while getting binance futures orders from Binance service.");
                Program.ShowMessage("DB error: " + e.Message);
            }

            return null;
        }

        public List<BinanceFuturesOrder> GetAllFuturesOrdersAbove()
        {
            var ordersAbove =
            from o in orders
                where o.Price > klines.LastOrDefault().ClosePrice
                select o;

            List<BinanceFuturesOrder> ordersAboveInList = new List<BinanceFuturesOrder>();
            foreach (var order in ordersAbove)
            {
                ordersAboveInList.Add(order);
            }

            return ordersAboveInList;
        }

        public void ClearBinanceFuturesOrders()
        {
            using var model = new Model();

            var rows = orders;
            foreach (var row in rows)
            {
                model.BinanceFuturesOrders.Remove(row);
            }
            model.SaveChanges();

            //Updating order list for accurate property manifestation
            orders = GetBinanceFuturesOrders();
        }

        public void ClearBinanceFuturesOrders(List<BinanceFuturesOrder> orders)
        {
            using var model = new Model();

            foreach (var order in orders)
            {
                model.BinanceFuturesOrders.Remove(order);
            }
            model.SaveChanges();

            //Updating order list for accurate property manifestation
            this.orders = GetBinanceFuturesOrders();
        }

        public static async Task InstallKlines()
        {
            try
            {
                using var model = new Model();

                Program.ShowMessage("Start installing...");

                model.Database.EnsureCreated();

                //If database containes kline we have to start updating same klines and installing new ones
                var firstDbKline = GetLastDbKline();

                if(firstDbKline != null)
                {
                    using (var client = new BinanceRestClient())
                    {
                        //Update first kline
                        var binanceKline = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDT", KlineInterval.OneHour, startTime: firstDbKline.OpenTime, limit: 1);

                        firstDbKline.Update(binanceKline.Data.First());
                        
                        UpdateKline(new Kline(firstDbKline, "BNBUSDC"));

                        //Install new klines if needed
                        var isKlines = false;
                        do
                        {
                            firstDbKline = GetLastDbKline();
                            var binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDT", KlineInterval.OneHour, startTime: firstDbKline.CloseTime);

                            isKlines = binanceKlines.Data.Any();
                            if(binanceKlines.Data.Any())
                            {
                                WriteDownKlinesList(binanceKlines.Data.ToList());
                            }

                        } while (isKlines);
                        Program.ShowMessage("Klines installed :)");
                    }
                }
                else
                {
                    using (var client = new BinanceRestClient())
                    {
                        Program.ShowMessage("New klines installing");

                        //Install new klines if needed
                        var isKlines = false;
                        do
                        {
                            firstDbKline = GetLastDbKline();

                            WebCallResult<IBinanceKline[]> binanceKlines;
                            if (firstDbKline == null)
                            {
                                binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDC", KlineInterval.OneHour, startTime: new DateTime(2007, 1, 31));
                            }
                            else
                            {
                                binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDC", KlineInterval.OneHour, startTime: firstDbKline.CloseTime);
                                Program.ShowMessage($"First kline in database: {firstDbKline.ToString()}");
                            }

                            isKlines = binanceKlines.Data.Any();
                            if (binanceKlines.Data.Any())
                            {
                                WriteDownKlinesList(binanceKlines.Data.ToList());
                            }

                        } while (isKlines);
                        Program.ShowMessage("Klines installed :)");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while installing the klines.");
                Program.ShowMessage("DB error in  InstallKlines: " + e.Message);
            }
        }

        static async Task<Kline> GetFirstKlineAsync(string symbol, KlineInterval interval, DateTime dateTime = default)
        {
            try
            {
                //We get imposiible start time and get 1 kline, so it has to be the first one
                var client = new BinanceRestClient();
                var result = await client.UsdFuturesApi.ExchangeData
                    .GetKlinesAsync(symbol, interval,
                                    startTime: dateTime, limit: 1);

                if (result.Success && result.Data.Any())
                {
                    var first = result.Data.First();
                    Console.WriteLine($"Earliest Kline open time: {first.OpenTime}");
                    return new Kline(first, "BNBUSDC");
                }
                else
                {
                    throw new Exception($"Error or no data: {result.Error}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while getting the first kline of the symbol.");
                Program.ShowMessage("DB error in GetFirstKline: " + e.Message);
            }

            return null;
        }

        static Kline GetLastDbKline()
        {
            try
            {
                using (var db = new Model())
                {
                    if (db.Klines.Any())
                    {
                        var lastKline = db.Klines.OrderByDescending(k => k.OpenTime)
                                            .First();
                        Program.ShowMessage($"Latest kline at {lastKline.OpenTime}");
                        return lastKline;
                    }

                    Program.ShowMessage("There are no klines in database.");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while getting last kline from the databse.");
                Program.ShowMessage("DB error in GetLastDBKline: " + e.Message);
            }

            return null;
        }

        static void WriteDownKlinesList(List<IBinanceKline> klinesInterface)
        {
            try
            {
                using var model = new Model();

                List<Kline> klines = new List<Kline>();

                //Converts IBinanceKline list into a Kline list
                foreach (var kline in klinesInterface)
                {
                    klines.Add(new Kline(kline, "BNBUSDC"));
                }

                model.Klines.AddRange(klines);
                model.SaveChanges();
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while writing down klines list.");
                Program.ShowMessage("DB error in WriteDownKlinesList: " + e.Message);
            }
        }

        static async Task WriteDownKlineAsync(Kline kline)
        {
            try
            {
                using var model = new Model();

                await model.Klines.AddAsync(kline);
                await model.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while writing down klines list.");
                Program.ShowMessage("DB error in WriteDownKlinesList: " + e.Message);
            }
        }

        static void UpdateKline(Kline kline)
        {
            try
            {
                using var model = new Model();

                var updatekline = model.Klines.Where(k => k.OpenTime == kline.OpenTime).First();
                if(updatekline != null)
                {
                    updatekline.Update(kline);

                    model.Update(updatekline);
                    model.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while writing down klines list.");
                Program.ShowMessage("DB error in WriteDownKlinesList: " + e.Message);
            }
        }
    }
}
