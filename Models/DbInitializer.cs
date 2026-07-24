using Microsoft.EntityFrameworkCore;
using Source.Models;
using BCrypt.Net;

namespace Source.Models
{
    public static class DbInitializer
    {
        private static readonly string[] UnsplashBurger = {
            "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400",
            "https://images.unsplash.com/photo-1625813506062-0aeb1d7a094b?w=400",
            "https://images.unsplash.com/photo-1551782450-17144efb9c50?w=400",
            "https://images.unsplash.com/photo-1572802419224-296b0aeee0d9?w=400",
            "https://images.unsplash.com/photo-1586816001966-79b736744398?w=400"
        };
        private static readonly string[] UnsplashPizza = {
            "https://images.unsplash.com/photo-1534308983496-4fabb1a015ee?w=400",
            "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=400",
            "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=400",
            "https://images.unsplash.com/photo-1558138838-76294be30005?w=400",
            "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400"
        };
        private static readonly string[] UnsplashChicken = {
            "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?w=400",
            "https://images.unsplash.com/photo-1562967914-608f82629710?w=400",
            "https://images.unsplash.com/photo-1527477396000-e27163b4be8f?w=400",
            "https://images.unsplash.com/photo-1598515214211-89d3c73ae83b?w=400",
            "https://images.unsplash.com/photo-1604908176997-125f25cc6f3d?w=400"
        };
        private static readonly string[] UnsplashDrink = {
            "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=400",
            "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400",
            "https://images.unsplash.com/photo-1527661591475-527312dd65f5?w=400",
            "https://images.unsplash.com/photo-1540224871915-bc8ffb782bdf?w=400",
            "https://images.unsplash.com/photo-1437414062039-6245b1e3edb4?w=400"
        };
        private static readonly string[] UnsplashSide = {
            "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400",
            "https://images.unsplash.com/photo-1585109649139-366815a0d713?w=400",
            "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=400",
            "https://images.unsplash.com/photo-1563379926898-05f4575a45d8?w=400",
            "https://images.unsplash.com/photo-1551782450-17144efb9c50?w=400"
        };
        private static readonly string[] UnsplashDessert = {
            "https://images.unsplash.com/photo-1551024601-bec78aea704b?w=400",
            "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400",
            "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400",
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400",
            "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400"
        };
        private static readonly string[] Themes = { "Gia đình", "Tiệc tùng", "Ăn vặt", "Trẻ em", "Văn phòng", "Ăn sáng" };
        private static readonly Random _rng = new();

        public static Task SeedAsync(AppDbContext context)
        {
            Seed(context);
            return Task.CompletedTask;
        }

        public static void Seed(AppDbContext context)
        {
            try
            {
                SeedCategories(context);
                SeedUsers(context);
                SeedOrders(context);
                SeedFoods(context);
                SeedCombos(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer ERROR] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void SeedCategories(AppDbContext context)
        {
            if (context.Categories.Count() >= 6) return;

            var existingNames = new HashSet<string>(context.Categories.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);
            var newCats = new List<Category>();

            if (!existingNames.Contains("Burgers"))
                newCats.Add(new Category { Name = "Burgers", Description = "Bánh burger thơm ngon" });
            if (!existingNames.Contains("Pizzas"))
                newCats.Add(new Category { Name = "Pizzas", Description = "Pizza Ý nóng hổi" });
            if (!existingNames.Contains("Gà Rán"))
                newCats.Add(new Category { Name = "Gà Rán", Description = "Gà rán giòn rụm" });
            if (!existingNames.Contains("Thức uống & Tráng miệng"))
                newCats.Add(new Category { Name = "Thức uống & Tráng miệng", Description = "Đồ uống giải khát" });
            if (!existingNames.Contains("Món Kèm"))
                newCats.Add(new Category { Name = "Món Kèm", Description = "Khoai tây, salad và các món ăn kèm" });
            if (!existingNames.Contains("Tráng Miệng"))
                newCats.Add(new Category { Name = "Tráng Miệng", Description = "Các món tráng miệng ngọt ngào" });

            if (newCats.Any())
            {
                context.Categories.AddRange(newCats);
                context.SaveChanges();
                Console.WriteLine($"[DbInitializer] Added {newCats.Count} categories. Total: {context.Categories.Count()}");
            }
        }

        private static void SeedUsers(AppDbContext context)
        {
            if (context.Users.Any()) return;

            context.Users.AddRange(
                new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11),
                    FullName = "Quản Trị Viên",
                    Email = "admin@fastfood.com",
                    PhoneNumber = "0987654321",
                    Address = "123 Đường Tô Ký, Quận 12, TP.HCM",
                    Role = "Admin"
                },
                new User
                {
                    Username = "customer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("customer123", workFactor: 11),
                    FullName = "Nguyễn Văn Khách",
                    Email = "customer@fastfood.com",
                    PhoneNumber = "0912345678",
                    Address = "456 Đường Quang Trung, Gò Vấp, TP.HCM",
                    Role = "Customer"
                }
            );
            context.SaveChanges();
        }

        private static void SeedOrders(AppDbContext context)
        {
            if (context.Orders.Any()) return;

            var customer = context.Users.First(u => u.Username == "customer");
            var admin = context.Users.First(u => u.Username == "admin");

            var orders = new List<Order>
            {
                new Order
                {
                    UserId = customer.Id, OrderDate = DateTime.Now.AddDays(-5), TotalAmount = 205000,
                    Status = "Delivered", ReceiverName = customer.FullName, ReceiverPhone = customer.PhoneNumber,
                    ReceiverAddress = customer.Address, PaymentMethod = "COD", ShippingFee = 0, Discount = 0,
                    Note = "Giao hàng giờ hành chính", UpdatedAt = DateTime.Now.AddDays(-3)
                },
                new Order
                {
                    UserId = customer.Id, OrderDate = DateTime.Now.AddDays(-2), TotalAmount = 150000,
                    Status = "Shipping", ReceiverName = customer.FullName, ReceiverPhone = customer.PhoneNumber,
                    ReceiverAddress = customer.Address, PaymentMethod = "COD", ShippingFee = 15000, Discount = 10000,
                    Note = "Giao nhanh nếu có thể", UpdatedAt = DateTime.Now.AddDays(-1)
                },
                new Order
                {
                    UserId = admin.Id, OrderDate = DateTime.Now.AddDays(-1), TotalAmount = 235000,
                    Status = "Preparing", ReceiverName = admin.FullName, ReceiverPhone = admin.PhoneNumber,
                    ReceiverAddress = admin.Address, PaymentMethod = "COD", ShippingFee = 0, Discount = 0,
                    Note = "Không hàng sốt", UpdatedAt = DateTime.Now.AddHours(-12)
                },
                new Order
                {
                    UserId = customer.Id, OrderDate = DateTime.Now.AddHours(-3), TotalAmount = 110000,
                    Status = "Pending", ReceiverName = customer.FullName, ReceiverPhone = customer.PhoneNumber,
                    ReceiverAddress = customer.Address, PaymentMethod = "COD", ShippingFee = 0, Discount = 5000,
                    Note = "", UpdatedAt = DateTime.Now.AddHours(-2)
                },
                new Order
                {
                    UserId = customer.Id, OrderDate = DateTime.Now.AddDays(-10), TotalAmount = 35000,
                    Status = "Cancelled", ReceiverName = customer.FullName, ReceiverPhone = customer.PhoneNumber,
                    ReceiverAddress = customer.Address, PaymentMethod = "COD", ShippingFee = 0, Discount = 0,
                    Note = "Khách hủy do thay đổi ý định", UpdatedAt = DateTime.Now.AddDays(-9)
                }
            };
            context.Orders.AddRange(orders);
            context.SaveChanges();

            var foods = context.FastFoods.Take(7).ToList();
            var orderDetails = new List<OrderDetail>
            {
                new OrderDetail { OrderId = orders[0].Id, FastFoodId = foods[0].Id, Quantity = 2, Price = foods[0].Price },
                new OrderDetail { OrderId = orders[0].Id, FastFoodId = foods[3].Id, Quantity = 1, Price = foods[3].Price },
                new OrderDetail { OrderId = orders[0].Id, FastFoodId = foods[5].Id, Quantity = 2, Price = foods[5].Price },
                new OrderDetail { OrderId = orders[1].Id, FastFoodId = foods[1].Id, Quantity = 1, Price = foods[1].Price },
                new OrderDetail { OrderId = orders[1].Id, FastFoodId = foods[4].Id, Quantity = 2, Price = foods[4].Price },
                new OrderDetail { OrderId = orders[1].Id, FastFoodId = foods[6].Id, Quantity = 2, Price = foods[6].Price },
                new OrderDetail { OrderId = orders[2].Id, FastFoodId = foods[0].Id, Quantity = 2, Price = foods[0].Price },
                new OrderDetail { OrderId = orders[2].Id, FastFoodId = foods[2].Id, Quantity = 2, Price = foods[2].Price },
                new OrderDetail { OrderId = orders[2].Id, FastFoodId = foods[3].Id, Quantity = 1, Price = foods[3].Price },
                new OrderDetail { OrderId = orders[3].Id, FastFoodId = foods[1].Id, Quantity = 1, Price = foods[1].Price },
                new OrderDetail { OrderId = orders[3].Id, FastFoodId = foods[5].Id, Quantity = 1, Price = foods[5].Price },
                new OrderDetail { OrderId = orders[4].Id, FastFoodId = foods[4].Id, Quantity = 1, Price = foods[4].Price }
            };
            context.OrderDetails.AddRange(orderDetails);
            context.SaveChanges();
        }

        private static void SeedFoods(AppDbContext context)
        {
            if (context.FastFoods.Count() >= 120) return;

            var existingNames = new HashSet<string>(context.FastFoods.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

            var catBurgers = context.Categories.First(c => c.Name == "Burgers").Id;
            var catPizzas = context.Categories.First(c => c.Name == "Pizzas").Id;
            var catChicken = context.Categories.First(c => c.Name == "Gà Rán").Id;
            var catDrinks = context.Categories.First(c => c.Name == "Thức uống & Tráng miệng").Id;
            var catSides = context.Categories.First(c => c.Name == "Món Kèm").Id;
            var catDessert = context.Categories.First(c => c.Name == "Tráng Miệng").Id;

            var burgers = new (string Name, int Price, string Desc)[]
            {
                ("Burger Bò Phô Mai", 55000, "Bánh burger kẹp thịt bò nướng cùng phô mai tan chảy và rau tươi."),
                ("Burger Gà Giòn", 50000, "Burger gà chiên giòn với sốt mayonnaise béo ngậy."),
                ("Burger BBQ Bacon", 65000, "Burger thịt bò nướng BBQ với thịt xông khói giòn."),
                ("Burger Bơ", 60000, "Burger bò kẹp bơ tươi và rau xà lách."),
                ("Burger Phô Mai Kép", 70000, "Burger hai lớp thịt bò với phô mai kép."),
                ("Burger Gà Cay", 55000, "Burger gà chiên tẩm gia vị cay Hàn Quốc."),
                ("Burger Tôm", 65000, "Burger tôm chiên xù với sốt chua ngọt."),
                ("Burger Nấm", 50000, "Burger bò kẹp nấm xào thơm béo."),
                ("Burger Trứng", 45000, "Burger bò kẹp trứng ốp la và thịt xông khói."),
                ("Burger Salad", 42000, "Burger gà tươi kẹp salad rau mầm."),
                ("Burger Đậu", 38000, "Burger chay từ đậu nành và rau củ."),
                ("Burger Phô Mai Cay", 58000, "Burger bò sốt phô mai cay Mexico."),
                ("Burger Xúc Xích", 52000, "Burger kẹp xúc xích nướng và dưa chuột muối."),
                ("Burger Cá Hồi", 75000, "Burger cá hồi tươi nướng sốt thì là."),
                ("Burger Heo Nướng", 55000, "Burger thịt heo nướng sốt teriyaki."),
                ("Burger Gà Teriyaki", 60000, "Burger gà sốt teriyaki Nhật Bản."),
                ("Burger BBQ Mỹ", 68000, "Burger bò sốt BBQ Mỹ với hành tây nướng."),
                ("Burger Mini", 35000, "Burger mini nhỏ xinh cho bé."),
                ("Burger Rau Củ", 40000, "Burger chay với rau củ nướng và sốt hummus."),
                ("Burger Gà Sốt Kem", 62000, "Burger gà chiên sốt kem nấm."),
                ("Burger Double", 72000, "Burger hai tầng thịt bò và phô mai."),
                ("Burger Phô Mai Xanh", 60000, "Burger bò sốt phô mai xanh."),
                ("Burger Gà Nướng", 55000, "Burger gà nướng than hoa."),
                ("Burger Hải Sản", 70000, "Burger tôm và mực chiên giòn."),
                ("Burger Bò Úc", 85000, "Burger thịt bò Úc nhập khẩu thượng hạng.")
            };
            var pizzas = new (string Name, int Price, string Desc)[]
            {
                ("Pizza Hải Sản", 120000, "Pizza mực, tôm, thanh cua với phô mai Mozzarella."),
                ("Pizza Thập Cẩm", 110000, "Pizza đầy ắp thịt nguội, xúc xích, nấm và ớt chuông."),
                ("Pizza Pepperoni", 125000, "Pizza pepperoni cay nồng với phô mai béo ngậy."),
                ("Pizza Phô Mai", 100000, "Pizza bốn loại phô mai Ý thượng hạng."),
                ("Pizza Rau Củ", 95000, "Pizza rau củ nướng cho người ăn chay."),
                ("Pizza Gà BBQ", 115000, "Pizza gà sốt BBQ với hành tây tím."),
                ("Pizza Bò Nướng", 130000, "Pizza thịt bò nướng với sốt tiêu đen."),
                ("Pizza Xúc Xích", 105000, "Pizza xúc xích Ý và phô mai Mozzarella."),
                ("Pizza Nấm Truffle", 145000, "Pizza nấm truffle cao cấp."),
                ("Pizza Hawaii", 110000, "Pizza dứa và thịt nguội kiểu Hawaii."),
                ("Pizza Carbonara", 120000, "Pizza sốt kem carbonara với thịt xông khói."),
                ("Pizza Cá Ngừ", 115000, "Pizza cá ngừ và hành tây sốt cà chua."),
                ("Pizza Đậu", 90000, "Pizza chay với đậu gà và rau bina."),
                ("Pizza Gà Cay", 118000, "Pizza gà sốt cay Buffalo."),
                ("Pizza Sốt Pesto", 125000, "Pizza sốt pesto Ý với hạt thông."),
                ("Pizza Phô Mai Dê", 135000, "Pizza phô mai dê và mật ong."),
                ("Pizza Mộc Nhĩ", 108000, "Pizza mộc nhĩ và rau cải thìa."),
                ("Pizza Tom Yum", 130000, "Pizza sốt tom yum Thái hải sản."),
                ("Pizza Bít Tết", 150000, "Pizza thịt bò bít tết sốt rượu vang."),
                ("Pizza Mini", 65000, "Pizza mini size cá nhân.")
            };
            var chickens = new (string Name, int Price, string Desc)[]
            {
                ("Gà Rán Giòn Cay", 35000, "Gà rán giòn rụm tẩm gia vị cay nồng."),
                ("Gà Rán Truyền Thống", 32000, "Gà rán giòn tan truyền thống."),
                ("Cánh Gà Chiên", 40000, "Cánh gà chiên giòn sốt Buffalo."),
                ("Đùi Gà BBQ", 45000, "Đùi gà nướng sốt BBQ đậm đà."),
                ("Gà Xé Cay", 35000, "Gà xé trộn sốt cay kiểu Thái."),
                ("Gà Sốt Mật Ong", 42000, "Gà rán sốt mật ong thơm ngọt."),
                ("Gà Sốt Phô Mai", 45000, "Gà rán sốt phô mai kem."),
                ("Gà Popcorn", 25000, "Gà popcorn chiên giòn ăn vặt."),
                ("Gà Xốt Tỏi", 38000, "Gà rán sốt tỏi bơ."),
                ("Gà Sốt Hàn Quốc", 42000, "Gà rán sốt cay Hàn Quốc."),
                ("Cánh Gà Sốt Phô Mai", 45000, "Cánh gà sốt phô mai xanh."),
                ("Đùi Gà Chiên", 35000, "Đùi gà chiên xù giòn tan."),
                ("Gà Rán Không Xương", 40000, "Gà rán phi lê không xương."),
                ("Gà Sốt Teriyaki", 44000, "Gà nướng sốt teriyaki Nhật."),
                ("Gà Xé Salad", 38000, "Gà xé trộn salad rau tươi."),
                ("Gà Cay Đậm", 36000, "Gà rán siêu cay cho tín đồ cay."),
                ("Gà Nướng Muối Ớt", 42000, "Gà nướng muối ớt chấm sốt chanh."),
                ("Gà Chiên Nước Mắm", 40000, "Gà chiên sốt nước mắm tỏi ớt."),
                ("Gà Sốt Chua Ngọt", 38000, "Gà rán sốt chua ngọt kiểu Thái."),
                ("Gà Sốt Hạt Điều", 50000, "Gà sốt hạt điều thơm bùi béo.")
            };
            var drinks = new (string Name, int Price, string Desc)[]
            {
                ("Coca Cola", 15000, "Nước ngọt Coca Cola mát lạnh."),
                ("Pepsi", 15000, "Nước ngọt Pepsi có ga."),
                ("Trà Đào", 25000, "Trà đào tươi mát lạnh."),
                ("Matcha Latte", 35000, "Matcha latte béo ngậy."),
                ("Sữa Tươi Trân Châu", 30000, "Sữa tươi trân châu đường đen."),
                ("Nước Cam", 25000, "Nước cam ép tươi 100%."),
                ("Nước Chanh", 15000, "Nước chanh tươi mát lạnh."),
                ("Sinh Tố Bơ", 35000, "Sinh tố bơ sữa đặc."),
                ("Sinh Tố Xoài", 30000, "Sinh tố xoài tươi ngọt mát."),
                ("Cà Phê Sữa", 25000, "Cà phê sữa đá Việt Nam."),
                ("Cà Phê Đen", 20000, "Cà phê đen đá đậm đà."),
                ("Soda Chanh", 20000, "Soda chanh tươi ngọt mát."),
                ("Sữa Chua Đá", 20000, "Sữa chua đá mát lạnh."),
                ("Nước Suối", 10000, "Nước suối tinh khiết."),
                ("Trà Sữa", 30000, "Trà sữa trân châu truyền thống."),
                ("Milo Dầm", 25000, "Milo dầm đá béo ngậy."),
                ("Bia Không Độ", 20000, "Bia không độ cồn mát lạnh."),
                ("Nước Táo", 22000, "Nước táo ép tươi nguyên chất."),
                ("Sting Dâu", 15000, "Nước tăng lực Sting vị dâu."),
                ("Monster Năng Lượng", 25000, "Nước tăng lực Monster xanh.")
            };
            var sides = new (string Name, int Price, string Desc)[]
            {
                ("Khoai Tây Chiên", 25000, "Khoai tây chiên vàng giòn rụm."),
                ("Khoai Tây Lắc Phô Mai", 30000, "Khoai tây chiên lắc phô mai."),
                ("Khoai Tây Lắc Cay", 28000, "Khoai tây chiên lắc gia vị cay."),
                ("Salad Rau Trộn", 35000, "Salad rau tươi trộn dầu giấm."),
                ("Salad Caesar", 40000, "Salad Caesar với thịt gà và phô mai."),
                ("Súp Bí Đỏ", 25000, "Súp bí đỏ kem tươi."),
                ("Súp Nấm", 28000, "Súp nấm kem béo ngậy."),
                ("Vòng Hành Tây", 30000, "Vòng hành tây chiên giòn."),
                ("Phô Mai Que", 35000, "Phô mai que chiên giòn."),
                ("Salad Đậu Gà", 32000, "Salad đậu gà sốt chanh."),
                ("Súp Cua", 35000, "Súp cua rau măng tươi."),
                ("Bánh Mì Bơ Tỏi", 20000, "Bánh mì nướng bơ tỏi thơm lừng."),
                ("Khoai Lang Chiên", 28000, "Khoai lang chiên vàng giòn."),
                ("Salad Trái Cây", 35000, "Salad trái cây tươi sốt dầu giấm."),
                ("Súp Gà", 30000, "Súp gà nấm hương ấm bụng."),
                ("Bánh Bao Chiên", 25000, "Bánh bao chiên nhân thịt."),
                ("Chả Giò", 30000, "Chả giò chiên giòn nhân tôm thịt."),
                ("Salad Địa Trung Hải", 38000, "Salad kiểu Địa Trung Hải với olive."),
                ("Khoai Tây Nghiền", 25000, "Khoai tây nghiền bơ sữa."),
                ("Súp Cà Chua", 25000, "Súp cà chua kem tươi.")
            };
            var desserts = new (string Name, int Price, string Desc)[]
            {
                ("Kem Vani", 25000, "Kem vani mát lạnh."),
                ("Kem Socola", 28000, "Kem socola béo ngậy."),
                ("Kem Dâu", 25000, "Kem dâu tươi mát lạnh."),
                ("Bánh Donut", 20000, "Bánh donut phủ socola."),
                ("Bánh Tart Trứng", 25000, "Bánh tart trứng thơm ngon."),
                ("Bánh Brownie", 30000, "Bánh brownie chocolate nóng chảy."),
                ("Pudding Caramel", 25000, "Pudding caramel mịn màng."),
                ("Chè Ba Màu", 20000, "Chè ba màu ngọt mát."),
                ("Trái Cây Dĩa", 35000, "Đĩa trái cây tươi theo mùa."),
                ("Bánh Cheesecake", 35000, "Bánh cheesecake New York."),
                ("Kem Xôi Dừa", 25000, "Kem xôi dừa béo ngậy."),
                ("Bánh Flan", 20000, "Bánh flan caramel."),
                ("Chuối Chiên", 15000, "Chuối chiên bột giòn rụm."),
                ("Bánh Crepe", 30000, "Bánh crepe nhân kem trái cây."),
                ("Sữa Chua Đá Topping", 25000, "Sữa chua đá topping trái cây.")
            };

            var foodList = new List<FastFood>();
            int idx = 0;

            void AddIfNotExists((string Name, int Price, string Desc)[] items, int catId, string[] unsplash)
            {
                foreach (var (name, price, desc) in items)
                {
                    if (!existingNames.Contains(name))
                    {
                        foodList.Add(new FastFood { Name = name, Price = price, Description = desc, ImageUrl = unsplash[idx % unsplash.Length], CategoryId = catId, Theme = Themes[_rng.Next(Themes.Length)] });
                        existingNames.Add(name);
                    }
                    idx++;
                }
            }

            idx = 0; AddIfNotExists(burgers, catBurgers, UnsplashBurger);
            idx = 0; AddIfNotExists(pizzas, catPizzas, UnsplashPizza);
            idx = 0; AddIfNotExists(chickens, catChicken, UnsplashChicken);
            idx = 0; AddIfNotExists(drinks, catDrinks, UnsplashDrink);
            idx = 0; AddIfNotExists(sides, catSides, UnsplashSide);
            idx = 0; AddIfNotExists(desserts, catDessert, UnsplashDessert);

            context.FastFoods.AddRange(foodList);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Added {foodList.Count} foods. Total: {context.FastFoods.Count()}");
        }

        private static void SeedCombos(AppDbContext context)
        {
            if (context.Combos.Count() >= 8) return;

            var foods = context.FastFoods.ToList();
            if (foods.Count < 6) return;

            var burgers = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Burgers").Id).ToList();
            var pizzas = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Pizzas").Id).ToList();
            var chickens = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Gà Rán").Id).ToList();
            var drinks = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Thức uống & Tráng miệng").Id).ToList();
            var sides = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Món Kèm").Id).ToList();
            var desserts = foods.Where(f => f.CategoryId == context.Categories.First(c => c.Name == "Tráng Miệng").Id).ToList();

            var comboData = new List<(string Name, string Desc, string Type, string Img)>
            {
                ("Combo Gia Đình", "2 Burger + 1 Khoai Tây + 2 Nước. Tiết kiệm 15%", "family", "burger"),
                ("Combo Tiệc Tùng", "1 Pizza + 1 Gà Rán + 1 Khoai + 2 Nước. Tiết kiệm 20%", "party", "pizza"),
                ("Combo Cặp Đôi", "2 Burger + 2 Nước + 1 Kem. Lãng mạn & tiết kiệm", "couple", "burger"),
                ("Combo Siêu Cay", "1 Gà Cay + 1 Burger Cay + 1 Khoai Lắc Cay + 1 Nước. Dành cho tín đồ cay", "spicy", "chicken"),
                ("Combo Gà Giòn", "4 Miếng Gà Rán + 1 Khoai Tây Chiên + 2 Nước. No nê cả nhà", "chicken", "chicken"),
                ("Combo Pizza Lớn", "2 Pizza Thập Cẩm + 1 Salad + 2 Nước. Tiệc vui bất tận", "pizza", "pizza"),
                ("Combo Healthy", "1 Salad + 1 Nước Ép + 1 Tráng Miệng. Nhẹ nhàng thanh mát", "healthy", "side"),
                ("Combo Trẻ Em", "1 Burger Mini + 1 Khoai Tây + 1 Nước + 1 Kem. Bé nào cũng thích", "kids", "burger")
            };

            int comboIdx = 0;
            foreach (var (name, desc, type, imgType) in comboData)
            {
                comboIdx++;

                decimal totalPrice = 0;
                var comboDetails = new List<(int FoodId, int Qty)>();

                if (imgType == "burger")
                {
                    var b = type switch
                    {
                        "couple" => burgers.Take(2).ToList(),
                        "kids" => burgers.Where(f => f.Price <= 40000).Take(1).ToList(),
                        _ => burgers.Take(2).ToList()
                    };
                    if (!b.Any()) b = burgers.Take(1).ToList();
                    foreach (var f in b) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    if (type == "couple")
                    {
                        var d = drinks.Take(2).ToList();
                        foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var des = desserts.Take(1).ToList();
                        foreach (var f in des) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    }
                    else if (type == "kids")
                    {
                        var s = sides.Where(f => f.Price <= 30000).Take(1).ToList();
                        foreach (var f in s) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var d = drinks.Where(f => f.Price <= 20000).Take(1).ToList();
                        foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var des = desserts.Where(f => f.Price <= 25000).Take(1).ToList();
                        foreach (var f in des) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    }
                    else
                    {
                        var s = sides.Take(1).ToList();
                        foreach (var f in s) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var d = drinks.Take(2).ToList();
                        foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    }
                }
                else if (imgType == "pizza")
                {
                    var p = (type == "pizza") ? pizzas.Take(2).ToList() : pizzas.Take(1).ToList();
                    foreach (var f in p) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    if (type == "pizza")
                    {
                        var s = sides.Take(1).ToList();
                        foreach (var f in s) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var d = drinks.Take(2).ToList();
                        foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    }
                    else
                    {
                        var c = chickens.Take(1).ToList();
                        foreach (var f in c) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var s = sides.Take(1).ToList();
                        foreach (var f in s) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                        var d = drinks.Take(2).ToList();
                        foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    }
                }
                else if (imgType == "chicken")
                {
                    var c = (type == "spicy") ? chickens.Where(f => f.Name.ToLower().Contains("cay")).Take(2).ToList() : chickens.Take(4).ToList();
                    if (!c.Any()) c = chickens.Take(2).ToList();
                    foreach (var f in c) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    var s = sides.Take(1).ToList();
                    foreach (var f in s) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    var d = drinks.Take(2).ToList();
                    foreach (var f in d) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                }
                else if (imgType == "side")
                {
                    var sal = sides.Take(1).ToList();
                    foreach (var f in sal) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    var j = drinks.Where(f => f.Price >= 20000).Take(1).ToList();
                    foreach (var f in j) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                    var des = desserts.Take(1).ToList();
                    foreach (var f in des) { comboDetails.Add((f.Id, 1)); totalPrice += f.Price; }
                }

                var combo = new Combo
                {
                    Name = name,
                    Description = desc,
                    Price = (int)(totalPrice * 0.8m),
                    ImageUrl = imgType switch
                    {
                        "burger" => UnsplashBurger[comboIdx % UnsplashBurger.Length],
                        "pizza" => UnsplashPizza[comboIdx % UnsplashPizza.Length],
                        "chicken" => UnsplashChicken[comboIdx % UnsplashChicken.Length],
                        "side" => UnsplashSide[comboIdx % UnsplashSide.Length],
                        _ => UnsplashBurger[comboIdx % UnsplashBurger.Length]
                    }
                };

                context.Combos.Add(combo);
                context.SaveChanges();

                foreach (var (foodId, qty) in comboDetails)
                {
                    context.ComboDetails.Add(new ComboDetail { ComboId = combo.Id, FastFoodId = foodId, Quantity = qty });
                }
                context.SaveChanges();
            }

            Console.WriteLine($"[DbInitializer] Seeded {comboData.Count} combos. Total: {context.Combos.Count()}");
        }
    }
}
