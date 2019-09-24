﻿using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace BTCPayServer.Ethereum.Client
{
    public class EthereumClientApplicationDbContext : DbContext
    {
        public EthereumClientApplicationDbContext()
        {

        }
        public EthereumClientApplicationDbContext(DbContextOptions<EthereumClientApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EthereumClientTransactionData> EthereumClientTransactions
        {
            get; set;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var isConfigured = optionsBuilder.Options.Extensions.OfType<RelationalOptionsExtension>().Any();
            if (!isConfigured)
                optionsBuilder.UseSqlite("Data Source=temp.db");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}
