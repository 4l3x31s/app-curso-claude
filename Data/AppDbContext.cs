using app_curso_claude.Models;
using Microsoft.EntityFrameworkCore;

namespace app_curso_claude.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Purchase> Purchases => Set<Purchase>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(c => c.FirstName).HasMaxLength(100);
                entity.Property(c => c.LastName).HasMaxLength(100);
                entity.Property(c => c.Email).HasMaxLength(256);
                entity.Property(c => c.Phone).HasMaxLength(30);
                entity.Property(c => c.Address).HasMaxLength(200);
                entity.Property(c => c.City).HasMaxLength(100);
                entity.Property(c => c.Country).HasMaxLength(100);

                entity.HasIndex(c => c.Email).IsUnique();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Sku).HasMaxLength(32);
                entity.Property(p => p.Name).HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Category).HasMaxLength(100);
                entity.Property(p => p.Price).HasPrecision(18, 2);

                // The database default makes the rows that existed before the column active.
                entity.Property(p => p.IsActive).HasDefaultValue(true);

                entity.HasIndex(p => p.Sku).IsUnique();
            });

            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.Property(p => p.UnitPrice).HasPrecision(18, 2);

                // Customers and products that already have purchases cannot be deleted.
                entity.HasOne(p => p.Customer)
                    .WithMany(c => c.Purchases)
                    .HasForeignKey(p => p.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Product)
                    .WithMany(p => p.Purchases)
                    .HasForeignKey(p => p.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
