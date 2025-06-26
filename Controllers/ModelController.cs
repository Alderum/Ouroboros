using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

        Model model = new Model();

        #endregion

        public ModelController()
        {
            orders = GetBinanceFuturesOrders();
            klines = GetListOfAllAvailableKlines();
        }

        List<Kline> GetListOfAllAvailableKlines()
        {
            //Get Klines from the database

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
            var klines = await model.Klines
                            .OrderBy(k => k.KlineId).ToListAsync();

            model.Klines.RemoveRange(klines);
            await model.SaveChangesAsync();

            //Updating list of klines for accurate property
            klines = GetListOfAllAvailableKlines();
        }

        public async Task WriteDownSymbolInfo(string symbol, KlineInterval interval)
        {
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
                    kline = new Kline();
                    kline.Interval = interval;
                    kline.Open = k.OpenPrice;
                    kline.Close = k.ClosePrice;
                    kline.DateTime = k.OpenTime;
                    kline.High = k.HighPrice;
                    kline.Low = k.LowPrice;
                    klines.Add(kline);
                }

                if (exchangeKlines.Count < 1000)
                    incomplete = false;
            } while (incomplete);

            Program.ShowMessage("Installing...");
            //It's necesary because iterator is modified in another iteration cycle
            klines = klines.OrderBy(k => k.DateTime).ToList();

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
            model.ChangeTracker.Clear();
            //Updating list of klines for accurate property
            klines = null;
        }

        public async Task UpdateSymbol(string symbol, KlineInterval interval)
        {
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
                Program.ShowMessage("Last kline: " + lastKline.DateTime);

                timeSpan = new TimeSpan(0, 0, 1000 * 60 * 60 * iteration);
                var exchangeKlinesInfo = await binanceClient.SpotApi.ExchangeData
                    .GetKlinesAsync(symbol, interval, startTime: lastKline.DateTime + timeSpan, limit: 1000);
                Program.ShowMessage("Exchange success: " + exchangeKlinesInfo.Success + ", Error: " + exchangeKlinesInfo.Error);

                try
                {
                    var exchangeKlines = exchangeKlinesInfo.Data.ToList();
                    for (int i = 0; i < exchangeKlines.Count; i++)
                    {
                        kline = new Kline();
                        kline.Interval = interval;
                        kline.Open = exchangeKlines[i].OpenPrice;
                        kline.Close = exchangeKlines[i].ClosePrice;
                        kline.DateTime = exchangeKlines[i].OpenTime;
                        kline.High = exchangeKlines[i].HighPrice;
                        kline.Low = exchangeKlines[i].LowPrice;
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
                var update = klinesUpdate.OrderBy(k => k.DateTime);
                klinesUpdate = update.ToList();

                lastKline.Close = klinesUpdate[0].Close;
                lastKline.High = klinesUpdate[0].High;
                lastKline.Low = klinesUpdate[0].Low;
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
            model.BinanceFuturesOrders.Add(order);
            await model.SaveChangesAsync();

            //Updating order list for accurate property manifestation
            orders = GetBinanceFuturesOrders();
        }

        List<BinanceFuturesOrder> GetBinanceFuturesOrders()
        {
            try
            {
                List<BinanceFuturesOrder> orders = model.BinanceFuturesOrders.OrderBy(k => k.CreateTime).ToList();
                return orders;
            }
            catch (Exception e)
            {
                Program.ShowMessage("DB error: " + e.Message);
            }

            return null;
        }

        public List<BinanceFuturesOrder> GetAllFuturesOrdersAbove()
        {
            var ordersAbove =
            from o in orders
                where o.Price > klines.LastOrDefault().Close
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
            foreach (var order in orders)
            {
                model.BinanceFuturesOrders.Remove(order);
            }
            model.SaveChanges();

            //Updating order list for accurate property manifestation
            this.orders = GetBinanceFuturesOrders();
        }
    }
}
