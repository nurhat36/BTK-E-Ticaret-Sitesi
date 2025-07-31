using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using BTKETicaretSitesi.Models;


namespace BTKETicaretSitesi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tablolar
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreStaff> StoreStaff { get; set; }

        // Ürün ile ilgili tablolar
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductReviewAnalysis> ProductReviewAnalyses { get; set; }

        // Alışveriş sepeti
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        // Sipariş yönetimi
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

        // Kullanıcı etkileşimleri
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }

        // İndirim ve kuponlar
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponCategory> CouponCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // StoreStaff için composite primary key
            builder.Entity<StoreStaff>(entity =>
            {
                entity.HasKey(ss => new { ss.StoreId, ss.UserId });

                // VARCHAR(450) olarak ayarla (IdentityUser Id'si varsayılan boyutu)
                entity.Property(ss => ss.UserId)
                    .HasMaxLength(450);

                // Cascade sorununu çözmek için NO ACTION ayarla
                entity.HasOne(ss => ss.Store)
                    .WithMany(s => s.Staff)
                    .HasForeignKey(ss => ss.StoreId)
                    .OnDelete(DeleteBehavior.ClientCascade); // veya DeleteBehavior.NoAction

                entity.HasOne(ss => ss.User)
                    .WithMany()
                    .HasForeignKey(ss => ss.UserId)
                    .OnDelete(DeleteBehavior.ClientCascade); // veya DeleteBehavior.NoAction
            });
            builder.Entity<Address>()
        .HasOne(a => a.User)
        .WithMany(u => u.Addresses)
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.Cascade);

            // Configure default shipping address relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.DefaultShippingAddress)
                .WithMany()
                .HasForeignKey(u => u.DefaultShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure default billing address relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.DefaultBillingAddress)
                .WithMany()
                .HasForeignKey(u => u.DefaultBillingAddressId)
                .OnDelete(DeleteBehavior.Restrict);
            // Category self-referencing ilişki
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product ilişkileri
            builder.Entity<Product>()
                .HasOne(p => p.Store)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.StoreId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductImage ilişkisi
            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariant ilişkisi
            builder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShoppingCart ilişkisi
            builder.Entity<ShoppingCart>()
                .HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem ilişkileri
            builder.Entity<CartItem>()
                .HasOne(ci => ci.ShoppingCart)
                .WithMany(sc => sc.Items)
                .HasForeignKey(ci => ci.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Variant)
                .WithMany()
                .HasForeignKey(ci => ci.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order ilişkileri
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem ilişkileri
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Variant)
                .WithMany()
                .HasForeignKey(oi => oi.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Store)
                .WithMany()
                .HasForeignKey(oi => oi.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderStatusHistory ilişkileri
            builder.Entity<OrderStatusHistory>()
                .HasOne(osh => osh.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(osh => osh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderStatusHistory>()
                .HasOne(osh => osh.ChangedByUser)
                .WithMany()
                .HasForeignKey(osh => osh.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductReview ilişkileri
            builder.Entity<ProductReview>()
                .HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // FavoriteProduct ilişkileri
            builder.Entity<FavoriteProduct>()
       .HasKey(fp => fp.Id); // Artık tek bir ID kullanıyoruz

            // UserId ve ProductId kombinasyonu için unique index
            builder.Entity<FavoriteProduct>()
                .HasIndex(fp => new { fp.UserId, fp.ProductId })
                .IsUnique();

            // İlişkileri NO ACTION ile ayarla
            builder.Entity<FavoriteProduct>()
                .HasOne(fp => fp.User)
                .WithMany()
                .HasForeignKey(fp => fp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<FavoriteProduct>()
                .HasOne(fp => fp.Product)
                .WithMany()
                .HasForeignKey(fp => fp.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Coupon ilişkileri
            builder.Entity<Coupon>()
                .HasOne(c => c.Store)
                .WithMany()
                .HasForeignKey(c => c.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // CouponCategory composite key
            builder.Entity<CouponCategory>()
                .HasKey(cc => new { cc.CouponId, cc.CategoryId });

            builder.Entity<CouponCategory>()
                .HasOne(cc => cc.Coupon)
                .WithMany(c => c.ApplicableCategories)
                .HasForeignKey(cc => cc.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CouponCategory>()
                .HasOne(cc => cc.Category)
                .WithMany()
                .HasForeignKey(cc => cc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Veritabanı indeksleri
            builder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            builder.Entity<Store>()
                .HasIndex(s => s.Slug)
                .IsUnique();

            builder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // OrderNumber için unique index
            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();
        }
    }
}