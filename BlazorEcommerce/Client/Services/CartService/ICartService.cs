namespace BlazorEcommerce.Client.Services.CartService;

public interface ICartService
{
    event Action OnChange;
    Task AddToCart(CartItem cartItem);
    Task<List<CartProductResponse>> GetCartProducts();
    Task RemoveProductFromCart(int productId, int productTypeId);
    Task UpdateQuantity(CartProductResponse productResponse);
    Task StoreCartItems(bool emptylocalCart);
    Task GetCartItemsCount();
}