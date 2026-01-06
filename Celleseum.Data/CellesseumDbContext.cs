
namespace Celleseum.Data
{
    using Microsoft.EntityFrameworkCore;
    
    public class CellesseumDbContext : DbContext
    {
        public CellesseumDbContext(DbContextOptions<CellesseumDbContext> options)
            : base(options)
        {
        }

        public DbSet<NumberSetDbRecord> NumberSets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NumberSetDbRecord>(n =>
            {
                n.HasKey(e => e.Id);
                n.Property(e => e.DateTime).IsRequired();
                n.Property(e => e.Average).IsRequired();
                n.Property(e => e.IpAddress).HasMaxLength(45);
            });
        }
    }
}
