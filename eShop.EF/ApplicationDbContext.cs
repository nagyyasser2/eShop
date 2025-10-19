using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using eShop.EF.Configurations;
using eShop.Core.Models;

namespace eShop.EF
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // PRODUCT CONFIGURATION WITH INDEXES
            // ============================================
            modelBuilder.Entity<Product>(entity =>
            {
                // Relationships
                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Single Column Indexes
                entity.HasIndex(p => p.Sku)
                    .IsUnique()
                    .HasDatabaseName("IX_Products_SKU");

                entity.HasIndex(p => p.IsActive)
                    .HasDatabaseName("IX_Products_IsActive");

                entity.HasIndex(p => p.IsFeatured)
                    .HasDatabaseName("IX_Products_IsFeatured");

                entity.HasIndex(p => p.Price)
                    .HasDatabaseName("IX_Products_Price");

                entity.HasIndex(p => p.CreatedAt)
                    .HasDatabaseName("IX_Products_CreatedAt");

                // Composite Indexes for common filter combinations
                entity.HasIndex(p => new { p.IsActive, p.IsFeatured })
                    .HasDatabaseName("IX_Products_IsActive_IsFeatured");

                entity.HasIndex(p => new { p.IsActive, p.CategoryId })
                    .HasDatabaseName("IX_Products_IsActive_CategoryId");

                entity.HasIndex(p => new { p.IsActive, p.Price })
                    .HasDatabaseName("IX_Products_IsActive_Price");

                entity.HasIndex(p => new { p.CategoryId, p.Price })
                    .HasDatabaseName("IX_Products_CategoryId_Price");

                entity.HasIndex(p => new { p.IsActive, p.IsFeatured, p.CreatedAt })
                    .HasDatabaseName("IX_Products_IsActive_IsFeatured_CreatedAt");

                // For search queries (Name contains)
                entity.HasIndex(p => p.Name)
                    .HasDatabaseName("IX_Products_Name");
            });

            // ============================================
            // IMAGE CONFIGURATION WITH INDEXES
            // ============================================
            new ProductImageEntityTypeConfiguration().Configure(modelBuilder.Entity<Image>());

            // ============================================
            // VARIANT CONFIGURATION WITH INDEXES
            // ============================================
            modelBuilder.Entity<Variant>(entity =>
            {
                entity.HasIndex(v => v.SKU)
                    .IsUnique()
                    .HasDatabaseName("IX_Variants_SKU");

                entity.HasIndex(v => v.ProductId)
                    .HasDatabaseName("IX_Variants_ProductId");
            });

            // ============================================
            // CATEGORY CONFIGURATION
            // ============================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.ChildCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index for finding active categories
                entity.HasIndex(c => c.IsActive)
                    .HasDatabaseName("IX_Categories_IsActive");

                // Index for hierarchical queries
                entity.HasIndex(c => c.ParentCategoryId)
                    .HasDatabaseName("IX_Categories_ParentCategoryId");
            });

            // ============================================
            // ORDER CONFIGURATION WITH INDEXES
            // ============================================
            modelBuilder.Entity<Order>(entity =>
            {
                // Index for user orders lookup
                entity.HasIndex(o => o.UserId)
                    .HasDatabaseName("IX_Orders_UserId");

                // Index for order status queries
                entity.HasIndex(o => o.ShippingStatus)
                    .HasDatabaseName("IX_Orders_ShippingStatus");

                // Composite index for user's orders by date
                entity.HasIndex(o => new { o.UserId, o.CreatedAt })
                    .HasDatabaseName("IX_Orders_UserId_CreatedAt");

                // Index for order date range queries
                entity.HasIndex(o => o.CreatedAt)
                    .HasDatabaseName("IX_Orders_CreatedAt");
            });

            // ============================================
            // ORDER ITEM CONFIGURATION
            // ============================================
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasIndex(oi => oi.OrderId)
                    .HasDatabaseName("IX_OrderItems_OrderId");

                entity.HasIndex(oi => oi.ProductId)
                    .HasDatabaseName("IX_OrderItems_ProductId");
            });

            // ============================================
            // IDENTITY ROLES SEED DATA
            // ============================================
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "2",
                    Name = "User",
                    NormalizedName = "USER"
                }
            );
        }

        public DbSet<Banner> Banners { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<Setting> Settings { get; set; }
    }
}