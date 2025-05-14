using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using System.Net;
using System.Numerics;
using CryptoExchange.Net.CommonObjects;
using Binance.Net.Objects.Models.Futures;

namespace VBTBotConsole3
{
    class Model : DbContext
    {        
        public DbSet<Kline> Klines { get; set; }
        public DbSet<BinanceFuturesOrder> BinanceFuturesOrders { get; set; }

        public string DbPath { get; }

        public Model()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "blogging.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
