using Source.Models;

namespace Source.Services
{
    public interface ICartSessionService
    {
        List<CartItem> GetCart();
        void SaveCart(List<CartItem> cart);
        void ClearCart();
        int GetItemCount();
    }
}
