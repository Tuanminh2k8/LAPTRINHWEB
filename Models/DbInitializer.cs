using Microsoft.EntityFrameworkCore;
using Source.Models;
using BCrypt.Net;

namespace Source.Models
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (!context.Users.Any())
            {
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

            if (!context.Orders.Any())
            {
                var customer = context.Users.First(u => u.Username == "customer");
                var admin = context.Users.First(u => u.Username == "admin");

                var orders = new List<Order>
                {
                    new Order
                    {
                        UserId = customer.Id,
                        OrderDate = DateTime.Now.AddDays(-5),
                        TotalAmount = 205000,
                        Status = "Delivered",
                        ReceiverName = customer.FullName,
                        ReceiverPhone = customer.PhoneNumber,
                        ReceiverAddress = customer.Address,
                        PaymentMethod = "COD",
                        ShippingFee = 0,
                        Discount = 0,
                        Note = "Giao hàng giờ hành chính",
                        UpdatedAt = DateTime.Now.AddDays(-3)
                    },
                    new Order
                    {
                        UserId = customer.Id,
                        OrderDate = DateTime.Now.AddDays(-2),
                        TotalAmount = 150000,
                        Status = "Shipping",
                        ReceiverName = customer.FullName,
                        ReceiverPhone = customer.PhoneNumber,
                        ReceiverAddress = customer.Address,
                        PaymentMethod = "COD",
                        ShippingFee = 15000,
                        Discount = 10000,
                        Note = "Giao nhanh nếu có thể",
                        UpdatedAt = DateTime.Now.AddDays(-1)
                    },
                    new Order
                    {
                        UserId = admin.Id,
                        OrderDate = DateTime.Now.AddDays(-1),
                        TotalAmount = 235000,
                        Status = "Preparing",
                        ReceiverName = admin.FullName,
                        ReceiverPhone = admin.PhoneNumber,
                        ReceiverAddress = admin.Address,
                        PaymentMethod = "COD",
                        ShippingFee = 0,
                        Discount = 0,
                        Note = "Không hàng sốt",
                        UpdatedAt = DateTime.Now.AddHours(-12)
                    },
                    new Order
                    {
                        UserId = customer.Id,
                        OrderDate = DateTime.Now.AddHours(-3),
                        TotalAmount = 110000,
                        Status = "Pending",
                        ReceiverName = customer.FullName,
                        ReceiverPhone = customer.PhoneNumber,
                        ReceiverAddress = customer.Address,
                        PaymentMethod = "COD",
                        ShippingFee = 0,
                        Discount = 5000,
                        Note = "",
                        UpdatedAt = DateTime.Now.AddHours(-2)
                    },
                    new Order
                    {
                        UserId = customer.Id,
                        OrderDate = DateTime.Now.AddDays(-10),
                        TotalAmount = 35000,
                        Status = "Cancelled",
                        ReceiverName = customer.FullName,
                        ReceiverPhone = customer.PhoneNumber,
                        ReceiverAddress = customer.Address,
                        PaymentMethod = "COD",
                        ShippingFee = 0,
                        Discount = 0,
                        Note = "Khách hủy do thay đổi ý định",
                        UpdatedAt = DateTime.Now.AddDays(-9)
                    }
                };

                context.Orders.AddRange(orders);
                context.SaveChanges();

                var orderDetails = new List<OrderDetail>();
                foreach (var order in orders)
                {
                    var foods = context.FastFoods.Take(3).ToList();
                    foreach (var food in foods)
                    {
                        orderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            FastFoodId = food.Id,
                            Quantity = new Random().Next(1, 3),
                            Price = food.Price
                        });
                    }
                }
                context.OrderDetails.AddRange(orderDetails);
                context.SaveChanges();
            }
        }
    }
}
