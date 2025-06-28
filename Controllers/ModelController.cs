using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
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

        public async Task WriteDownSymbolInfo(string symbol, KlineInterval interval)
        {
            using var model = new Model();

            var binanceClientCalculator = new BinanceRestClient();

            List<Kline> klines = new List<Kline>();
            Kline kline;

            DateTime dateTime = DateTime.Now;
            bool incomplete = true;
            Program.ShowMessage("Downloading...");

            int numberOfKlines = 0;
            DateTime openFirstKline = new DateTime();
            do
            {
                //Get klines from Binance database 1 time
                var exchangeKlinesInfo = await binanceClientCalculator.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, endTime: DateTime.Now - new TimeSpan(0, 0, 0, klines.Count * (int)interval), limit: 1000);
                var exchangeKlines = exchangeKlinesInfo.Data.ToList();

                foreach (var k in exchangeKlines)
                {
                    kline = new Kline(k);
                    kline.KlineId = k.GetHashCode();
                }

                if (exchangeKlines.Count < 1000)
                    incomplete = false;
            } while (incomplete);

            Program.ShowMessage("Installing...");
            //It's necesary because iterator is modified in another iteration cycle
            klines = klines.OrderBy(k => k.OpenTime).ToList();

            for (int i = 0; i < klines.Count; i++)
            {
                klines[i].KlineId = i + 1;
            }

            await model.Klines.AddRangeAsync(klines);
            await model.SaveChangesAsync();

            Program.ShowMessage("");
            Program.ShowMessage("Complited ^~^");

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

        public async Task UpdateSymbol(string symbol, KlineInterval interval)
        {
            using var model = new Model();

            Kline lastKline = model.Klines.OrderByDescending(k => k.KlineId).First();

            var binanceClient = new BinanceRestClient();

            bool incomplete = true;
            int numberOfKlines = 0;
            List<Kline> klinesUpdate = new List<Kline>();
            Kline kline;
            TimeSpan timeSpan;
            int iteration = 0;
            do
            {
                Program.ShowMessage("Last kline: " + lastKline.OpenTime);

                timeSpan = new TimeSpan(0, 0, 1000 * 60 * 60 * iteration);
                var exchangeKlinesInfo = await binanceClient.SpotApi.ExchangeData
                    .GetKlinesAsync(symbol, interval, startTime: lastKline.OpenTime + timeSpan, limit: 1000);
                Program.ShowMessage("Exchange success: " + exchangeKlinesInfo.Success + ", Error: " + exchangeKlinesInfo.Error);

                try
                {
                    var exchangeKlines = exchangeKlinesInfo.Data.ToList();
                    for (int i = 0; i < exchangeKlines.Count; i++)
                    {
                        kline = new Kline(exchangeKlines[i]);
                        kline.KlineId = exchangeKlines[i].GetHashCode();
                        klinesUpdate.Add(kline);
                    }

                    if (exchangeKlines.Count < 1000)
                        incomplete = false;
                }
                catch (Exception e)
                {
                    Program.ShowMessage("Addind klines to update error: " + e.Message);
                }

                /* Idea
                 * Я збираю клінії в один масив, не присвоюючи їм айді, а потім
                 * їх рахую та упорядковую - це допоможе по два рази не перезапитувати
                 * в бінансу масив кліній. Тож топ ідея х2 швидкості.
                 * 1) Зібрати 1к кліній
                 * 
                 * 2) Чи їх справді 1к?
                 * - так
                 * 3) Цикл не закінчений
                 * - ні
                 * 3) Цикл закінчений
                 * 
                 * 4) Присвоїти всі лінії до масиву
                 * 5) Переробити їм айпі
                 * 6) Якщо треба то повторити
                 */

                iteration++;
            } while (incomplete);

            try
            {
                var update = klinesUpdate.OrderBy(k => k.OpenTime);
                klinesUpdate = update.ToList();

                lastKline = klinesUpdate[0];
                model.Update(lastKline);
                await model.SaveChangesAsync();
            }
            catch(Exception e)
            {
                Program.ShowMessage("Updating database error: " + e.Message);
                if(e.InnerException != null)
                {
                    Program.ShowMessage("Inner exeption: " + e.InnerException);
                }
            }
            Program.ShowMessage("Last kline updated");

            for (int i = 1; i < klinesUpdate.Count; i++)
            {
                klinesUpdate[i].KlineId = i + lastKline.KlineId;
                model.Add(klinesUpdate[i]);
                Program.ShowMessage(".");
            }

            await model.SaveChangesAsync();

            //Updating field for accurate property
            klines = GetListOfAllAvailableKlines();
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

                model.Database.EnsureCreated();


            }
            catch (Exception e)
            {
                Log.Error(e, "An error occured while installing the klines.");
                Program.ShowMessage("DB error in  InstallKlines: " + e.Message);
            }
        }

        static async Task<Kline> GetFirstKlineAsync(string symbol, KlineInterval interval)
        {
            try
            {
                //
                var client = new BinanceRestClient();
                var result = await client.UsdFuturesApi.ExchangeData
                    .GetKlinesAsync(symbol, interval,
                                    startTime: new DateTime(2007, 1, 31), limit: 1);

                if (result.Success && result.Data.Any())
                {
                    var first = result.Data.First();
                    Console.WriteLine($"Earliest Kline open time: {first.OpenTime}");
                    return new Kline(result.Data.FirstOrDefault());
                }
                else
                {
                    Console.WriteLine($"Error or no data: {result.Error}");
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

        static async Task<Kline> GetLastKlineAsync()
        {
            try
            {
                using (var db = new Model())
                {
                    var lastKline = await db.Klines
                                            .OrderByDescending(k => k.OpenTime)
                                            .FirstOrDefaultAsync();

                    if (lastKline != null)
                    {
                        Console.WriteLine($"Latest kline at {lastKline.OpenTime}");
                        return lastKline;
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while getting last kline from the databse.");
                Program.ShowMessage("DB error in GetFirstKline: " + e.Message);
            }

            return null;
        }
    }
}
