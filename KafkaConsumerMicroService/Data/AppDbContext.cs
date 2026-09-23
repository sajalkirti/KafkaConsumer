using Microsoft.EntityFrameworkCore;
using System;

namespace KafkaConsumerMicroService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<MarketRecord> MarketRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MarketRecord>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.SourceSystem).HasMaxLength(200);
                b.Property(x => x.RawPayload).HasColumnType("nvarchar(max)");
                b.Property(x => x.Metadata).HasColumnType("nvarchar(max)");

                // Indexes to help high-throughput querying by source, account, validity, and time
                b.HasIndex(x => x.SourceSystem);
                b.HasIndex(x => x.AccountNumber);
                b.HasIndex(x => x.IsValid);
                b.HasIndex(x => x.ReceivedAt);
                // Composite index to accelerate queries that filter by IsValid and sort by ReceivedAt
                b.HasIndex(x => new { x.IsValid, x.ReceivedAt });
            });
        }
    }
}
