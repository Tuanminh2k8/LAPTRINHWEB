using Source.Models;
using Xunit;

namespace PolyFood.Tests
{
    public class OrderStatusTransitionTests
    {
        [Theory]
        [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Preparing)]
        [InlineData(OrderStatus.Preparing, OrderStatus.ReadyForPickup)]
        [InlineData(OrderStatus.ReadyForPickup, OrderStatus.DriverAssigned)]
        [InlineData(OrderStatus.DriverAssigned, OrderStatus.PickedUp)]
        [InlineData(OrderStatus.PickedUp, OrderStatus.Shipping)]
        [InlineData(OrderStatus.Shipping, OrderStatus.Arriving)]
        [InlineData(OrderStatus.Arriving, OrderStatus.Delivered)]
        public void HappyPath_Transitions_AreValid(string from, string to)
        {
            Assert.True(OrderStatus.IsValidTransition(from, to),
                $"'{from}' -> '{to}' nên là hợp lệ.");
        }

        [Theory]
        [InlineData(OrderStatus.Delivered, OrderStatus.Preparing)] // không được quay lui
        [InlineData(OrderStatus.Shipping, OrderStatus.Pending)]
        [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]   // nhảy cóc
        [InlineData(OrderStatus.PickedUp, OrderStatus.Confirmed)]
        public void BackwardOrSkip_Transitions_AreInvalid(string from, string to)
        {
            Assert.False(OrderStatus.IsValidTransition(from, to),
                $"'{from}' -> '{to}' nên bị chặn.");
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Confirmed)]
        public void Cancel_From_Early_States_IsAllowed(string from)
        {
            Assert.True(OrderStatus.IsValidTransition(from, OrderStatus.Cancelled));
        }

        [Theory]
        [InlineData(OrderStatus.Shipping)]
        [InlineData(OrderStatus.Delivered)]
        public void Cancel_From_Late_States_IsBlocked(string from)
        {
            Assert.False(OrderStatus.IsValidTransition(from, OrderStatus.Cancelled));
        }

        [Theory]
        [InlineData(OrderStatus.Delivered, OrderStatus.RefundPending)]
        [InlineData(OrderStatus.RefundPending, OrderStatus.Refunded)]
        public void Refund_Flow_IsValid(string from, string to)
        {
            Assert.True(OrderStatus.IsValidTransition(from, to));
        }

        [Fact]
        public void InDelivery_Contains_Only_TransportStates()
        {
            Assert.Equal(new[] { OrderStatus.DriverAssigned, OrderStatus.PickedUp, OrderStatus.Shipping, OrderStatus.Arriving },
                OrderStatus.InDelivery);
        }

        [Fact]
        public void GetLabel_Returns_Vietnamese_For_Every_Status()
        {
            foreach (var status in OrderStatus.All)
            {
                var label = OrderStatus.GetLabel(status);
                Assert.False(string.IsNullOrWhiteSpace(label));
                Assert.False(label.StartsWith("["), $"'{status}' chưa có nhãn tiếng Việt.");
            }
        }

        [Fact]
        public void GetIcon_Returns_Valid_FontAwesome_Class()
        {
            foreach (var status in OrderStatus.All)
            {
                var icon = OrderStatus.GetIcon(status);
                Assert.StartsWith("fa-", icon);
            }
        }
    }
}