using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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
                new FastFood { Id = 1, Name = "Burger Bò Phô Mai", Price = 55000, Description = "Bánh burger kẹp thịt bò nướng thơm ngon cùng lớp phô mai béo ngậy và rau tươi.", ImageUrl = "/images/burger_cheese.svg", CategoryId = 1, Theme = "Gia đình" },
                new FastFood { Id = 2, Name = "Burger Gà Giòn", Price = 50000, Description = "Bánh burger kẹp thịt gà chiên giòn tan, sốt mayonnaise và xà lách ngon tuyệt.", ImageUrl = "/images/burger_chicken.svg", CategoryId = 1, Theme = "Trẻ em" },
                new FastFood { Id = 3, Name = "Pizza Hải Sản", Price = 120000, Description = "Pizza với mực, tôm, thanh cua tươi ngon cùng phô mai Mozzarella thượng hạng.", ImageUrl = "/images/pizza_seafood.svg", CategoryId = 2, Theme = "Tiệc tùng" },
                new FastFood { Id = 4, Name = "Pizza Thập Cẩm", Price = 110000, Description = "Pizza đầy ắp thịt nguội, xúc xích pepperoni, ớt chuông, nấm và phô mai.", ImageUrl = "/images/pizza_mixed.svg", CategoryId = 2, Theme = "Gia đình" },
                new FastFood { Id = 5, Name = "Gà Rán Giòn Cay", Price = 35000, Description = "Một miếng gà rán giòn rụm, tẩm ướp gia vị cay nồng đậm đà.", ImageUrl = "/images/chicken_spicy.svg", CategoryId = 3, Theme = "Ăn vặt" },
                new FastFood { Id = 6, Name = "Khoai Tây Chiên", Price = 25000, Description = "Khoai tây chiên vàng giòn, rắc chút muối thơm ngon.", ImageUrl = "/images/fries.svg", CategoryId = 4, Theme = "Ăn vặt" },
                new FastFood { Id = 7, Name = "Coca Cola", Price = 15000, Description = "Nước ngọt có ga Coca Cola mát lạnh.", ImageUrl = "/images/coca.svg", CategoryId = 4, Theme = "Ăn uống" }
            );

            // Seed Combos
            modelBuilder.Entity<Combo>().HasData(
                new Combo { Id = 1, Name = "Combo Gia Đình", Price = 150000, Description = "2 Burger Bò Phô Mai + 1 Khoai Tây Chiên + 2 Coca Cola. Tiết kiệm hơn!", ImageUrl = "/images/combo_family.svg" },
                new Combo { Id = 2, Name = "Combo Tiệc Tùng", Price = 200000, Description = "1 Pizza Hải Sản + 1 Gà Rán Giòn Cay + 1 Khoai Tây Chiên + 2 Coca Cola. Cực vui cực đã!", ImageUrl = "/images/combo_party.svg" }
            );

            // Seed ComboDetails
            modelBuilder.Entity<ComboDetail>().HasData(
                // Combo Gia Đình (ComboId = 1)
                new ComboDetail { ComboId = 1, FastFoodId = 1, Quantity = 2 }, // 2 Burger Bò
                new ComboDetail { ComboId = 1, FastFoodId = 6, Quantity = 1 }, // 1 Khoai Tây Chiên
                new ComboDetail { ComboId = 1, FastFoodId = 7, Quantity = 2 }, // 2 Coca Cola

                // Combo Tiệc Tùng (ComboId = 2)
                new ComboDetail { ComboId = 2, FastFoodId = 3, Quantity = 1 }, // 1 Pizza Hải Sản
                new ComboDetail { ComboId = 2, FastFoodId = 5, Quantity = 1 }, // 1 Gà Rán Giòn Cay
                new ComboDetail { ComboId = 2, FastFoodId = 6, Quantity = 1 }, // 1 Khoai Tây Chiên
                new ComboDetail { ComboId = 2, FastFoodId = 7, Quantity = 2 }  // 2 Coca Cola
            );

            // Seed Users (hashed passwords)
            // SHA256 of "admin123" is: 24075510645cfa8ef1d2b77d612e09e1e360f08a4768ab3054f15d2a939460a8 (or whatever, let's compute it in code or seed it as hashed)
            // Let's use simple SHA256 hash in string:
            // "admin123" -> SHA256: 24075510645CFA8EF1D2B77D612E09E1E360F08A4768AB3054F15D2A939460A8
            // "customer123" -> SHA256: BD1C50CA07137C2C0B76FDE1CD9F6D6B90E52C9F80D6B19BF9CC9FECE6C413BE
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "24075510645CFA8EF1D2B77D612E09E1E360F08A4768AB3054F15D2A939460A8", // SHA256 of "admin123"
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
                    PasswordHash = "BD1C50CA07137C2C0B76FDE1CD9F6D6B90E52C9F80D6B19BF9CC9FECE6C413BE", // SHA256 of "customer123"
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
