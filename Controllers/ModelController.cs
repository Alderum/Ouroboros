using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Program = VTB.VTB;

namespace VBTBotConsole3.Controllers
{
    static class ModelController
    {
        #region Fields and properties

        static List<BinanceFuturesOrder> orders;
        public static List<BinanceFuturesOrder> Orders {
            get
            {
                return new Model().BinanceFuturesOrders.ToList();
            }
        }

        static List<Kline> klines;
        public static List<Kline> Klines
        {
            get
            {
                return new Model().Klines.ToList();
            }
        }

        #endregion


        #region Methods

        #region Get methods

        //Returns list of klines stored in Entity Framework database
        static List<Kline> GetListOfAllAvailableKlines()
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

        //Returns list of binance futures orders stored in Entity Framework database
        static List<BinanceFuturesOrder> GetBinanceFuturesOrders()
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

        //Returns list of binance futures orders stored in database that are above of current price
        public static List<BinanceFuturesOrder> GetAllFuturesOrdersAbove()
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

        //Returns instance of Kline class stored in the database with the latest open time property
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

        #endregion

        #region Add methods

        //Adds new binance futures order to Entity Framework database
        public static async Task WriteNewOrderDown(BinanceFuturesOrder order)
        {
            using var model = new Model();

            model.BinanceFuturesOrders.Add(order);
            await model.SaveChangesAsync();

            //Updating order list for accurate property manifestation
            orders = GetBinanceFuturesOrders();
        }

        //Adds new klines to the database. If there are no klines: we install all info till the end.
        //If there are klines in the database: we update last and install new ones if needed.
        public static async Task InstallKlines()
        {
            try
            {
                using var model = new Model();

                Program.ShowMessage("Start installing...");

                //Last available kline in the database
                var lastDBKline = GetLastDbKline();

                //If there is last kline in the database we should update it and install new ones
                if (lastDBKline != null)
                {
                    using (var client = new BinanceRestClient())
                    {
                        //Update first kline
                        var binanceKline = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDT", KlineInterval.OneHour, startTime: lastDBKline.OpenTime, limit: 1);

                        lastDBKline.Update(binanceKline.Data.First());

                        UpdateKline(lastDBKline);

                        //Install new klines if needed
                        var areKlines = false;
                        do
                        {
                            lastDBKline = GetLastDbKline();
                            var binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDT", KlineInterval.OneHour, startTime: lastDBKline.CloseTime);

                            areKlines = binanceKlines.Data.Any();
                            if (binanceKlines.Data.Any())
                            {
                                WriteDownKlinesList(binanceKlines.Data.ToList());
                            }

                        } while (areKlines);
                        Program.ShowMessage("Klines installed :)");
                    }
                }
                //If there are no klines in the database we should install only new ones until there
                //are no more in the Binance API database
                else
                {
                    using (var client = new BinanceRestClient())
                    {
                        Program.ShowMessage("New klines installing");

                        //Install new klines if needed
                        var areKlines = false;
                        do
                        {
                            lastDBKline = GetLastDbKline();

                            WebCallResult<IBinanceKline[]> binanceKlines;
                            if (lastDBKline == null)
                            {
                                //First install
                                binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDC", KlineInterval.OneHour, startTime: new DateTime(2007, 1, 31));
                            }
                            else
                            {
                                //Continue installing
                                binanceKlines = await client.SpotApi.ExchangeData.GetKlinesAsync(
                            "BNBUSDC", KlineInterval.OneHour, startTime: lastDBKline.CloseTime);
                                Program.ShowMessage($"First kline in database: {lastDBKline.ToString()}");
                            }

                            areKlines = binanceKlines.Data.Any();
                            if (binanceKlines.Data.Any())
                            {
                                WriteDownKlinesList(binanceKlines.Data.ToList());
                            }

                        } while (areKlines);
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

        //Adds new klines to the database
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

        #endregion

        #region Delete methods

        //Deletes all klines stored in the database
        public static async void ClearDatabase()
        {
            using var model = new Model();

            var klines = await model.Klines
                            .OrderBy(k => k.KlineId).ToListAsync();

            model.Klines.RemoveRange(klines);
            await model.SaveChangesAsync();

            //Updating list of klines for accurate property
            klines = GetListOfAllAvailableKlines();
        }

        //Detouches all enetities from the database
        public static void DetouchDatabase()
        {
            using var model = new Model();

            model.ChangeTracker.Clear();
            //Updating list of klines for accurate property
            klines = null;
        }

        //Deletes all binanace fututres orders from Entity Framework database
        public static void ClearBinanceFuturesOrders()
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

        //Deletes given binance futures orders from the database
        public static void ClearBinanceFuturesOrders(List<BinanceFuturesOrder> orders)
        {
            using var model = new Model();

            foreach (var order in orders)
            {
                model.BinanceFuturesOrders.Remove(order);
            }
            model.SaveChanges();

            //Updating order list for accurate property manifestation
            orders = GetBinanceFuturesOrders();
        }

        #endregion

        #region Update methods

        //Updates kline in the database not touching its ID
        static void UpdateKline(Kline kline)
        {
            try
            {
                using var model = new Model();

                var updatekline = model.Klines.Where(k => k.OpenTime == kline.OpenTime).First();
                if (updatekline != null)
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

        #endregion

        #endregion
    }
}
