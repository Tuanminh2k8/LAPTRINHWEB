using Microsoft.EntityFrameworkCore;

namespace Source.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<FastFood> FastFoods { get; set; } = null!;
        public DbSet<Combo> Combos { get; set; } = null!;
        public DbSet<ComboDetail> ComboDetails { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for ComboDetail
            modelBuilder.Entity<ComboDetail>()
                .HasKey(cd => new { cd.ComboId, cd.FastFoodId });

            // Relationships
            modelBuilder.Entity<ComboDetail>()
                .HasOne(cd => cd.Combo)
                .WithMany(c => c.ComboDetails)
                .HasForeignKey(cd => cd.ComboId);

            modelBuilder.Entity<ComboDetail>()
                .HasOne(cd => cd.FastFood)
                .WithMany(f => f.ComboDetails)
                .HasForeignKey(cd => cd.FastFoodId);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Burgers", Description = "Các loại bánh burger thơm ngon" },
                new Category { Id = 2, Name = "Pizzas", Description = "Pizza nóng hổi, nhiều phô mai" },
                new Category { Id = 3, Name = "Gà Rán", Description = "Gà chiên giòn rụm" },
                new Category { Id = 4, Name = "Thức uống & Tráng miệng", Description = "Nước ngọt, khoai tây chiên, kem" }
            );

            // Seed FastFoods
            modelBuilder.Entity<FastFood>().HasData(
                new FastFood { Id = 1, Name = "Burger Bò Phô Mai", Price = 55000, Description = "Bánh burger kẹp thịt bò nướng thơm ngon cùng lớp phô mai béo ngậy và rau tươi.", ImageUrl = "/images/products/burger-cheese-double.jpg", CategoryId = 1, Theme = "Gia đình" },
                new FastFood { Id = 2, Name = "Burger Gà Giòn", Price = 50000, Description = "Bánh burger kẹp thịt gà chiên giòn tan, sốt mayonnaise và xà lách ngon tuyệt.", ImageUrl = "/images/products/burger-bbq-bacon.jpg", CategoryId = 1, Theme = "Trẻ em" },
                new FastFood { Id = 3, Name = "Pizza Hải Sản", Price = 120000, Description = "Pizza với mực, tôm, thanh cua tươi ngon cùng phô mai Mozzarella thượng hạng.", ImageUrl = "/images/products/pizza-seafood.jpg", CategoryId = 2, Theme = "Tiệc tùng" },
                new FastFood { Id = 4, Name = "Pizza Thập Cẩm", Price = 110000, Description = "Pizza đầy ắp thịt nguội, xúc xích pepperoni, ớt chuông, nấm và phô mai.", ImageUrl = "/images/products/pizza-pepperoni.jpg", CategoryId = 2, Theme = "Gia đình" },
                new FastFood { Id = 5, Name = "Gà Rán Giòn Cay", Price = 35000, Description = "Một miếng gà rán giòn rụm, tẩm ướp gia vị cay nồng đậm đà.", ImageUrl = "/images/products/chicken-crispy.jpg", CategoryId = 3, Theme = "Ăn vặt" },
                new FastFood { Id = 6, Name = "Khoai Tây Chiên", Price = 25000, Description = "Khoai tây chiên vàng giòn, rắc chút muối thơm ngon.", ImageUrl = "/images/products/chicken-spicy-wings.jpg", CategoryId = 4, Theme = "Ăn vặt" },
                new FastFood { Id = 7, Name = "Coca Cola", Price = 15000, Description = "Nước ngọt có ga Coca Cola mát lạnh.", ImageUrl = "/images/products/drink-coke.jpg", CategoryId = 4, Theme = "Ăn uống" }
            );

            // Seed Combos
            modelBuilder.Entity<Combo>().HasData(
                new Combo { Id = 1, Name = "Combo Gia Đình", Price = 150000, Description = "2 Burger Bò Phô Mai + 1 Khoai Tây Chiên + 2 Coca Cola. Tiết kiệm hơn!", ImageUrl = "/images/products/burger-cheese-double.jpg" },
                new Combo { Id = 2, Name = "Combo Tiệc Tùng", Price = 200000, Description = "1 Pizza Hải Sản + 1 Gà Rán Giòn Cay + 1 Khoai Tây Chiên + 2 Coca Cola. Cực vui cực đã!", ImageUrl = "/images/products/pizza-seafood.jpg" }
            );

            // Seed ComboDetails
            modelBuilder.Entity<ComboDetail>().HasData(
                new ComboDetail { ComboId = 1, FastFoodId = 1, Quantity = 2 },
                new ComboDetail { ComboId = 1, FastFoodId = 6, Quantity = 1 },
                new ComboDetail { ComboId = 1, FastFoodId = 7, Quantity = 2 },

                new ComboDetail { ComboId = 2, FastFoodId = 3, Quantity = 1 },
                new ComboDetail { ComboId = 2, FastFoodId = 5, Quantity = 1 },
                new ComboDetail { ComboId = 2, FastFoodId = 6, Quantity = 1 },
                new ComboDetail { ComboId = 2, FastFoodId = 7, Quantity = 2 }
            );

            // Indexes for Orders
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.IsDeleted);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            // Seed Users with BCrypt hashes
            // "admin123" BCrypt hash: $2a$11$ezY8eus712l.J/TErYvnveHybjXijpr.j7gucKR7G0q3xlgK6WCc6
            // "customer123" BCrypt hash: $2a$11$YIt.Q8rHNv0BKrlePDKezedHKn7OjqQYdbTAS7EramaJSAVPn.R/6
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$ezY8eus712l.J/TErYvnveHybjXijpr.j7gucKR7G0q3xlgK6WCc6",
                    FullName = "Quản Trị Viên",
                    Email = "admin@fastfood.com",
                    PhoneNumber = "0987654321",
                    Address = "123 Đường Tô Ký, Quận 12, TP.HCM",
                    Role = "Admin"
                },
                new User
                {
                    Id = 2,
                    Username = "customer",
                    PasswordHash = "$2a$11$YIt.Q8rHNv0BKrlePDKezedHKn7OjqQYdbTAS7EramaJSAVPn.R/6",
                    FullName = "Nguyễn Văn Khách",
                    Email = "customer@fastfood.com",
                    PhoneNumber = "0912345678",
                    Address = "456 Đường Quang Trung, Gò Vấp, TP.HCM",
                    Role = "Customer"
                }
            );
        }
    }
}
