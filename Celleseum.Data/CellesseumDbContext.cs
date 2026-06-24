
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

        public DbSet<Result> NumberSets { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Result>(n =>
            {
                n.HasKey(e => e.Id);
                n.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                n.Property(e => e.DateTime).IsRequired();
                n.Property(e => e.Score).IsRequired();
                n.Property(e => e.PlantsCreated).IsRequired();
                n.Property(e => e.GrazersCreated).IsRequired();
            });
        }
    }
}
