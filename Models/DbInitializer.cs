using Microsoft.EntityFrameworkCore;
using Source.Models;
using BCrypt.Net;

namespace Source.Models
{
    public static class DbInitializer
    {
        // Ảnh Unsplash thật theo từng chủng loại (verify tồn tại); seed giúp mỗi món có ảnh riêng.
        private static string RemoteImg(string keyword, int seed) =>
            UnsplashImage.ForKeyword(keyword, seed);

        private static readonly string[] Themes = { "Gia đình", "Tiệc tùng", "Ăn vặt", "Trẻ em", "Văn phòng", "Ăn sáng" };
        private static readonly Random _rng = new();
        private static int _imgSeed = 1000;

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
                SeedModifiers(context);
                SeedCombos(context);
                SeedBranches(context);
                SeedPromoCodes(context);
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
            if (!existingNames.Contains("Đồ ăn sáng"))
                newCats.Add(new Category { Name = "Đồ ăn sáng", Description = "Bữa sáng tiện lợi, đầy đủ dinh dưỡng" });
            if (!existingNames.Contains("Salad & Wrap"))
                newCats.Add(new Category { Name = "Salad & Wrap", Description = "Salad tươi mát và bánh cuốn healthy" });

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
            if (context.FastFoods.Count() >= 150) return;

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

            void AddIfNotExists((string Name, int Price, string Desc)[] items, int catId, string keyword)
            {
                foreach (var (name, price, desc) in items)
                {
                    if (!existingNames.Contains(name))
                    {
                        foodList.Add(new FastFood
                        {
                            Name = name,
                            Price = price,
                            Description = desc,
                            ImageUrl = RemoteImg(keyword, _imgSeed++),
                            CategoryId = catId,
                            Theme = Themes[_rng.Next(Themes.Length)],
                            IsAvailable = true,
                            IsBestSeller = _rng.Next(100) < 30,
                            SoldCount = _rng.Next(0, 400)
                        });
                        existingNames.Add(name);
                    }
                }
            }

            AddIfNotExists(burgers, catBurgers, "burger");
            AddIfNotExists(pizzas, catPizzas, "pizza");
            AddIfNotExists(chickens, catChicken, "friedchicken");
            AddIfNotExists(drinks, catDrinks, "drink");
            AddIfNotExists(sides, catSides, "fries");
            AddIfNotExists(desserts, catDessert, "dessert");

            context.FastFoods.AddRange(foodList);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Added {foodList.Count} curated foods. Total: {context.FastFoods.Count()}");

            // Đảm bảo có ít nhất 150 món có thể đặt được (sinh tự động trong code)
            EnsureMinFoods(context, existingNames, 150);
            Console.WriteLine($"[DbInitializer] Total foods after ensure: {context.FastFoods.Count()}");

            // Đảm bảo MỌI món đều có thể đặt được (yêu cầu: tất cả món đều đặt được)
            var unavailable = context.FastFoods.Where(f => !f.IsAvailable).ToList();
            if (unavailable.Any())
            {
                foreach (var f in unavailable) f.IsAvailable = true;
                context.SaveChanges();
                Console.WriteLine($"[DbInitializer] Set {unavailable.Count} foods to Available (tất cả đặt được).");
            }
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
                    ImageUrl = RemoteImg(imgType, 500 + comboIdx)
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

        private static void SeedPromoCodes(AppDbContext context)
        {
            if (context.PromoCodes.Any()) return;

            context.PromoCodes.AddRange(
                new PromoCode
                {
                    Code = "POLYFOOD20",
                    Description = "Giảm 20% cho đơn hàng, tối đa 50.000 ₫ (đơn tối thiểu 50.000 ₫)",
                    DiscountType = "Percent",
                    DiscountValue = 20,
                    MinOrderAmount = 50000,
                    MaxDiscountAmount = 50000,
                    MaxUsage = 0,
                    StartDate = DateTime.Now.AddDays(-1),
                    EndDate = DateTime.Now.AddYears(1),
                    IsActive = true
                },
                new PromoCode
                {
                    Code = "GIAM30K",
                    Description = "Giảm thẳng 30.000 ₫ cho đơn từ 150.000 ₫",
                    DiscountType = "Amount",
                    DiscountValue = 30000,
                    MinOrderAmount = 150000,
                    MaxDiscountAmount = 0,
                    MaxUsage = 100,
                    StartDate = DateTime.Now.AddDays(-1),
                    EndDate = DateTime.Now.AddMonths(3),
                    IsActive = true
                },
                new PromoCode
                {
                    Code = "HETHAN",
                    Description = "Mã đã hết hạn (dùng để kiểm thử)",
                    DiscountType = "Percent",
                    DiscountValue = 50,
                    MinOrderAmount = 0,
                    StartDate = DateTime.Now.AddMonths(-2),
                    EndDate = DateTime.Now.AddMonths(-1),
                    IsActive = true
                });
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Seeded promo codes. Total: {context.PromoCodes.Count()}");
        }

        /// <summary>Sinh tự động thêm món cho đến khi đạt ít nhất minCount món có thể đặt được.</summary>
        private static void EnsureMinFoods(AppDbContext context, HashSet<string> existingNames, int minCount)
        {
            var cats = context.Categories.ToList();
            if (context.FastFoods.Count() >= minCount) return;

            var flavors = new Dictionary<string, string[]>
            {
                ["Burgers"] = new[] { "Sốt BBQ", "Phô Mai Xanh", "Tỏi Ớt", "Hạt Tiêu", "Nấm Truffle", "Cay Hàn Quốc", "Teriyaki", "Tôm", "Bơ Lạt", "Xông Khói", "Trứng Bác", "Rau Sống" },
                ["Pizzas"] = new[] { "Hải Sản", "Thập Cẩm", "Pepperoni", "Phô Mai", "Rau Củ", "Gà BBQ", "Bò Nướng", "Xúc Xích", "Nấm", "Hawaii", "Carbonara", "Cá Ngừ" },
                ["Gà Rán"] = new[] { "Giòn Cay", "Truyền Thống", "Sốt Mật Ong", "Phô Mai", "Tỏi Bơ", "Hàn Quốc", "Muối Ớt", "Nước Mắm", "Chua Ngọt", "Không Xương", "Teriyaki", "Popcorn" },
                ["Thức uống & Tráng miệng"] = new[] { "Đào", "Matcha", "Sữa Trân Châu", "Cam", "Chanh", "Bơ", "Xoài", "Cà Phê Sữa", "Cà Phê Đen", "Soda", "Sữa Chua", "Táo" },
                ["Món Kèm"] = new[] { "Phô Mai", "Lắc Cay", "Lắc Phô Mai", "Salad", "Súp", "Vòng Hành Tây", "Que", "Khoai Lang", "Nghiền", "Chả Giò", "Bánh Mì Tỏi", "Địa Trung Hải" },
                ["Tráng Miệng"] = new[] { "Vani", "Socola", "Dâu", "Donut", "Tart Trứng", "Brownie", "Pudding", "Ba Màu", "Cheesecake", "Flan", "Crepe", "Xôi Dừa" },
                ["Đồ ăn sáng"] = new[] { "Bánh Mì Trứng", "Xôi", "Bánh Cuốn", "Phở", "Mì", "Trứng Ốp La", "Ngũ Cốc", "Sữa", "Bánh Ngọt", "Trà Sáng" },
                ["Salad & Wrap"] = new[] { "Gà", "Bò", "Tôm", "Chay", "Caesar", "Địa Trung Hải", "Trái Cây", "Đậu Gà", "Thịt Nướng", "Cá Ngừ" }
            };

            int seed = 3000;
            int safety = 0;
            while (context.FastFoods.Count() < minCount && safety < 3000)
            {
                safety++;
                foreach (var cat in cats)
                {
                    if (context.FastFoods.Count() >= minCount) break;
                    if (!flavors.TryGetValue(cat.Name, out var pool)) continue;

                    var flavor = pool[_rng.Next(pool.Length)];
                    var noun = cat.Name switch
                    {
                        "Burgers" => "Burger",
                        "Pizzas" => "Pizza",
                        "Gà Rán" => "Gà",
                        "Thức uống & Tráng miệng" => "Nước",
                        "Món Kèm" => "Món",
                        "Tráng Miệng" => "Món",
                        "Đồ ăn sáng" => "Món Sáng",
                        "Salad & Wrap" => "Salad",
                        _ => cat.Name
                    };
                    var name = $"{noun} {flavor}";
                    if (existingNames.Contains(name)) continue;

                    var (price, keyword) = cat.Name switch
                    {
                        "Pizzas" => (90000 + _rng.Next(0, 70000), "pizza"),
                        "Gà Rán" => (25000 + _rng.Next(0, 30000), "friedchicken"),
                        "Thức uống & Tráng miệng" => (10000 + _rng.Next(0, 25000), "drink"),
                        "Tráng Miệng" => (15000 + _rng.Next(0, 20000), "dessert"),
                        "Đồ ăn sáng" => (20000 + _rng.Next(0, 30000), "breakfast"),
                        "Salad & Wrap" => (35000 + _rng.Next(0, 40000), "salad"),
                        _ => (30000 + _rng.Next(0, 50000), "food")
                    };

                    context.FastFoods.Add(new FastFood
                    {
                        Name = name,
                        Price = price,
                        Description = $"{name} thơm ngon, chế biến tươi nóng hổi từ bếp PolyFood.",
                        ImageUrl = RemoteImg(keyword, seed++),
                        CategoryId = cat.Id,
                        Theme = Themes[_rng.Next(Themes.Length)],
                        IsAvailable = true,
                        IsBestSeller = _rng.Next(100) < 25,
                        SoldCount = _rng.Next(0, 300)
                    });
                    existingNames.Add(name);
                }
                context.SaveChanges();
            }
        }

        /// <summary>Sinh nhóm tùy biến (Size / Topping / Độ cay) cho các món theo chủng loại — nền tảng cho F1 (tùy biến hoàn chỉnh).</summary>
        private static void SeedModifiers(AppDbContext context)
        {
            if (context.ModifierGroups.Any()) return;

            var cats = context.Categories.ToList();
            var groups = new List<ModifierGroup>();
            int go = 0, oo = 0;

            ModifierGroup G(string name, string desc, int foodId, bool multiple, int max, params (string n, decimal p, bool d)[] opts)
            {
                var g = new ModifierGroup
                {
                    Name = name,
                    Description = desc,
                    FastFoodId = foodId,
                    IsMultiple = multiple,
                    MaxOptions = max,
                    SortOrder = ++go,
                    Options = new List<ModifierOption>()
                };
                foreach (var (n, p, d) in opts)
                    g.Options.Add(new ModifierOption { Name = n, Price = p, IsDefault = d, IsAvailable = true, SortOrder = ++oo });
                return g;
            }

            foreach (var food in context.FastFoods.AsNoTracking().ToList())
            {
                var catName = cats.First(c => c.Id == food.CategoryId).Name;
                if (catName == "Burgers")
                {
                    groups.Add(G("Size", "Chọn kích cỡ", food.Id, false, 1,
                        ("Nhỏ", 0, false), ("Vừa", 10000, true), ("Lớn", 20000, false)));
                    groups.Add(G("Topping", "Thêm nhân / phô mai", food.Id, true, 4,
                        ("Thêm phô mai", 8000, false), ("Thêm thịt bò", 15000, false), ("Thêm rau & xà lách", 3000, false)));
                }
                else if (catName == "Pizzas")
                {
                    groups.Add(G("Size", "Chọn kích cỡ", food.Id, false, 1,
                        ("Vừa (9 inch)", 0, true), ("Lớn (12 inch)", 30000, false), ("Đại (15 inch)", 55000, false)));
                }
                else if (catName == "Gà Rán")
                {
                    groups.Add(G("Độ cay", "Chọn độ cay", food.Id, false, 1,
                        ("Không cay", 0, true), ("Cay nhẹ", 0, false), ("Cay nồng", 5000, false)));
                }
                else if (catName == "Thức uống & Tráng miệng")
                {
                    groups.Add(G("Size", "Chọn kích cỡ", food.Id, false, 1,
                        ("Nhỏ", 0, false), ("Vừa", 0, true), ("Lớn", 5000, false)));
                }
            }

            context.ModifierGroups.AddRange(groups);
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Seeded {groups.Count} modifier groups, {groups.Sum(g => g.Options.Count)} options.");
        }

        /// <summary>Seed chi nhánh (phục vụ Pickup / Collection Point).</summary>
        private static void SeedBranches(AppDbContext context)
        {
            if (context.Branches.Any()) return;

            context.Branches.AddRange(
                new Branch { Name = "PolyFood Biên Hòa", Address = "Khu Công Nghệ Cao, Biên Hòa, Đồng Nai", Phone = "02513 999 111", District = "Biên Hòa", OpenTime = new TimeSpan(7, 0, 0), CloseTime = new TimeSpan(22, 0, 0), IsActive = true, SortOrder = 1 },
                new Branch { Name = "PolyFood Quận 1", Address = "12 Lê Lợi, Quận 1, TP.HCM", Phone = "028 3822 3333", District = "Quận 1", OpenTime = new TimeSpan(7, 0, 0), CloseTime = new TimeSpan(22, 0, 0), IsActive = true, SortOrder = 2 },
                new Branch { Name = "PolyFood Gò Vấp", Address = "45 Nguyễn Kiệm, Gò Vấp, TP.HCM", Phone = "028 3944 5555", District = "Gò Vấp", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(22, 0, 0), IsActive = true, SortOrder = 3 },
                new Branch { Name = "PolyFood Thủ Đức", Address = "88 Võ Văn Ngân, TP Thủ Đức, TP.HCM", Phone = "028 3722 6666", District = "Thủ Đức", OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(21, 0, 0), IsActive = true, SortOrder = 4 },
                new Branch { Name = "PolyFood Bình Dương", Address = "2 Đại lộ Bình Dương, Thủ Dầu Một", Phone = "0274 388 7777", District = "Thủ Dầu Một", OpenTime = new TimeSpan(7, 0, 0), CloseTime = new TimeSpan(22, 0, 0), IsActive = true, SortOrder = 5 }
            );
            context.SaveChanges();
            Console.WriteLine($"[DbInitializer] Seeded {context.Branches.Count()} branches.");
        }
    }
}
