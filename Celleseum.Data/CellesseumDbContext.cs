
namespace Celleseum.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class CellesseumDbContext : IdentityDbContext<ApplicationUser>
    {
        public CellesseumDbContext(DbContextOptions<CellesseumDbContext> options)
            : base(options)
        {
        }

        public DbSet<NumberSetDbRecord> NumberSets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
