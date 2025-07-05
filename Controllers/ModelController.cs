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
    class ModelController
    {
        #region Fields and properties

        private readonly Model model;
        private readonly BinanceRestClient restClient;

        #endregion

        public ModelController(Model model, BinanceRestClient restClient)
        {
            this.model = model;
            this.restClient = restClient;
        }

        #region Methods

        #region Get methods

        //Returns list of binance futures orders stored in the database
        public List<BinanceFuturesOrder> GetOrders()
        {
            return model.BinanceFuturesOrders.OrderBy(o => o.CreateTime).ToList();
        }

        //Returns list of klines stored in the database
        public List<Kline> GetKlines()
        {
            return model.Klines.OrderBy(k => k.OpenTime).ToList();
        }

        //Returns list of binance futures orders stored in database that are above of current price
        public List<BinanceFuturesOrder> GetOrdersAbove()
        {
            try
            {
                var ordersAbove =
                from o in GetOrders()
                where o.Price > GetKlines().Last().ClosePrice
                select o;

                return ordersAbove.ToList();
            }
            catch(Exception e)
            {
                Log.Error(e, "An error occurred while getting binance futures orders above the present price");
                Program.ShowMessage("DB error in GetOrdersAbove(): " + e.Message);
            }

            return null;
        }

        //Returns instance of Kline class stored in the database with the latest open time property
        Kline GetLastKline()
        {
            try
            {
                if (model.Klines.Any())
                {
                    var lastKline = model.Klines.OrderBy(k => k.OpenTime).Last();

                    Program.ShowMessage("Latest kline in the database:\n" + lastKline.ToString());

                    return lastKline;
                }

                Program.ShowMessage("There are no klines in database.");
                return null;
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
        public void WriteNewOrderDown(BinanceFuturesOrder order)
        {
            model.BinanceFuturesOrders.Add(order);
            model.SaveChanges();
        }

        //Adds new klines to the database. If there are no klines: we install all info till the end.
        //If there are klines in the database: we update last and install new ones if needed.
        public async Task InstallKlines()
        {
            try
            {
                //Last available kline in the database
                var lastKline = GetLastKline();

                //If there is last kline in the database we should update it and install new ones
                if (lastKline != null)
                {
                    //Update first kline
                    var binanceKline = await restClient.SpotApi.ExchangeData.GetKlinesAsync(
                        "BNBUSDT", KlineInterval.OneHour, startTime: lastKline.OpenTime, limit: 1);

                    lastKline.Update(binanceKline.Data.First());

                    UpdateKline(lastKline);

                    //Install new klines if needed
                    var areKlines = false;
                    do
                    {
                        lastKline = GetLastKline();
                        var binanceKlines = await restClient.SpotApi.ExchangeData.GetKlinesAsync(
                        "BNBUSDT", KlineInterval.OneHour, startTime: lastKline.CloseTime);

                        areKlines = binanceKlines.Data.Any();
                        if (binanceKlines.Data.Any())
                        {
                            List<Kline> klines = new List<Kline>();
                            foreach (var kline in binanceKlines.Data.ToList())
                            {
                                klines.Add(new Kline(kline, "BNBUSDC"));
                            }
                            WriteDownKlinesList(klines);
                        }

                    } while (areKlines);
                }
                //If there are no klines in the database we should install only new ones until there
                //are no more in the Binance API database
                else
                {
                    Program.ShowMessage("New klines installing");

                    //Install new klines if needed
                    var areKlines = false;
                    do
                    {
                        lastKline = GetLastKline();

                        WebCallResult<IBinanceKline[]> binanceKlines;
                        if (lastKline == null)
                        {
                            //First install
                            binanceKlines = await restClient.SpotApi.ExchangeData.GetKlinesAsync(
                        "BNBUSDC", KlineInterval.OneHour, startTime: new DateTime(2007, 1, 31));
                        }
                        else
                        {
                            //Continue installing
                            binanceKlines = await restClient.SpotApi.ExchangeData.GetKlinesAsync(
                        "BNBUSDC", KlineInterval.OneHour, startTime: lastKline.CloseTime);
                            Program.ShowMessage($"First kline in database:\n {lastKline.ToString()}");
                        }

                        areKlines = binanceKlines.Data.Any();
                        if (binanceKlines.Data.Any())
                        {
                            List<Kline> klines = new List<Kline>();
                            foreach(var kline in binanceKlines.Data.ToList())
                                {
                                klines.Add(new Kline(kline, "BNBUSDC"));
                                }
                            WriteDownKlinesList(klines);
                        }

                    } while (areKlines);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while installing the klines.");
                Program.ShowMessage("DB error in  InstallKlines: " + e.Message);
            }
        }

        //Adds new klines to the database
        void WriteDownKlinesList(List<Kline> klines)
        {
            try
            {
                var existing = model.Klines
                    .Select(k => new { k.SymbolId, k.OpenTime })
                    .ToHashSet();

                var toAdd = klines
                    .Where(k => !existing.Contains(new { k.SymbolId, k.OpenTime }))
                    .ToList();

                if (toAdd.Any())
                {
                    model.Klines.AddRange(toAdd);
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

        #region Delete methods

        //Deletes all klines stored in the database
        public void ClearDatabase()
        {
            model.Klines.ExecuteDelete();
            model.SaveChanges();
        }

        //Detouches all enetities from the database
        public void DetouchDatabase()
        {
            model.ChangeTracker.Clear();
        }

        //Deletes all binanace fututres orders from Entity Framework database
        public void ClearBinanceFuturesOrders()
        {
            model.BinanceFuturesOrders.ExecuteDelete();
            model.SaveChanges();
        }

        //Deletes given binance futures orders from the database
        public void ClearBinanceFuturesOrders(List<BinanceFuturesOrder> orders)
        {
            model.BinanceFuturesOrders.RemoveRange(orders);
            model.SaveChanges();
        }

        #endregion
        
        #region Update methods

        //Updates kline in the database not touching its ID
        void UpdateKline(Kline kline)
        {
            try
            {
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

        public void Migrate()
        {
            model.Database.Migrate();
        }

        #endregion
    }
}
