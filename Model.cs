using Microsoft.EntityFrameworkCore;
using Binance.Net.Objects.Models.Futures;

namespace VBTBotConsole3
{
    class Model : DbContext
    {        
        public DbSet<Kline> Klines { get; set; }
        public DbSet<BinanceFuturesOrder> BinanceFuturesOrders { get; set; }

        public Model(DbContextOptions<Model> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Kline>()
                .HasKey(k => new { k.SymbolId, k.OpenTime });

            // Drop the uniqueness:
            builder.Entity<Kline>()
                .HasIndex(k => new { k.SymbolId, k.OpenTime })
                .IsUnique(false);
        }
    }
}
