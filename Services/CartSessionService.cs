using Source.Helpers;
using Source.Models;

namespace Source.Services
{
    public class CartSessionService : ICartSessionService
    {
        private const string CartKey = "FastFoodCart";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartSessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public List<CartItem> GetCart() =>
            Session.GetObjectFromJson<List<CartItem>>(CartKey) ?? new List<CartItem>();

        public void SaveCart(List<CartItem> cart) =>
            Session.SetObjectAsJson(CartKey, cart);

        public void ClearCart() => Session.Remove(CartKey);

        public int GetItemCount() => GetCart().Sum(i => i.Quantity);
    }
}
