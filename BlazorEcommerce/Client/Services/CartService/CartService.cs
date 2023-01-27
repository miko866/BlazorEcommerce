namespace BlazorEcommerce.Client.Services.CartService;

using Blazored.LocalStorage;

public class CartService : ICartService
{
    private readonly ILocalStorageService _localStorageService;
    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public CartService(ILocalStorageService localStorageService, HttpClient httpClient, AuthenticationStateProvider authenticationStateProvider)
    {
        _localStorageService = localStorageService;
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
    }
    
    public event Action? OnChange;
    
    public async Task AddToCart(CartItem cartItem)
    {
        if (await IsUserAuthenticated())
        {
            await _httpClient.PostAsJsonAsync("api/cart/add", cartItem);
        }
        else
        {
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if (cart == null)
            {
                cart = new List<CartItem>();
            }

            var sameItem = cart.Find(x => x.ProductId == cartItem.ProductId && x.ProductTypeId == cartItem.ProductTypeId);
            if (sameItem == null)
            {
                cart.Add(cartItem);
            }
            else
            {
                sameItem.Quantity += cartItem.Quantity;
            }

            await _localStorageService.SetItemAsync("cart", cart);
        }
        await GetCartItemsCount();
    }

    public async Task<List<CartProductResponse>> GetCartProducts()
    {
        if (await IsUserAuthenticated())
        {
            var response = await _httpClient.GetFromJsonAsync<ServiceResponse<List<CartProductResponse>>>("api/cart");
            return response.Data;
        }
        else
        {
            var cartItems = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if (cartItems == null) return new List<CartProductResponse>();
            
            var response = await _httpClient.PostAsJsonAsync("api/cart/products", cartItems);
            var cartProducts = await response.Content.ReadFromJsonAsync<ServiceResponse<List<CartProductResponse>>>();
            return cartProducts.Data;
        }
    }

    public async Task RemoveProductFromCart(int productId, int productTypeId)
    {
        if (await IsUserAuthenticated())
        {
            await _httpClient.DeleteAsync($"api/cart/{productId}/{productTypeId}");
        }
        else
        {
            
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            if (cart == null)
            {
                return;
            }

            var cartItem = cart.Find(x => x.ProductId == productId && x.ProductTypeId == productTypeId);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                await _localStorageService.SetItemAsync("cart", cart);
            }
        }
    }

    public async Task UpdateQuantity(CartProductResponse productResponse)
    {
        if (await IsUserAuthenticated())
        {
            var request = new CartItem
            {
                ProductId = productResponse.ProductId,
                Quantity = productResponse.Quantity,
                ProductTypeId = productResponse.ProductTypeId
            };
            await _httpClient.PutAsJsonAsync("api/cart/update-quantity", request);
        }
        else
        {
            
        var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
        if (cart == null)
        {
            return;
        }

        var cartItem = cart.Find(x => x.ProductId == productResponse.ProductId && x.ProductTypeId == productResponse.ProductTypeId);
        if (cartItem != null)
        {
            cartItem.Quantity = productResponse.Quantity;
            await _localStorageService.SetItemAsync("cart", cart);
        }
        }
    }

    public async Task StoreCartItems(bool emptylocalCart = false)
    {
        var localCart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
        if (localCart == null)
        {
            return;
        }

        await _httpClient.PostAsJsonAsync("api/cart", localCart);

        if (emptylocalCart)
        {
            await _localStorageService.RemoveItemAsync("cart");
        }
    }

    public async Task GetCartItemsCount()
    {
        if (await IsUserAuthenticated())
        {
            var result = await _httpClient.GetFromJsonAsync<ServiceResponse<int>>("api/cart/count");
            var count = result.Data;

            await _localStorageService.SetItemAsync<int>("cartItemsCount", count);
        }
        else
        {
            var cart = await _localStorageService.GetItemAsync<List<CartItem>>("cart");
            await _localStorageService.SetItemAsync<int>("cartItemsCount", cart != null ? cart.Count : 0);
        }
        
        OnChange.Invoke();
    }

    private async Task<bool> IsUserAuthenticated()
    {
        return ((await _authenticationStateProvider.GetAuthenticationStateAsync()).User.Identity.IsAuthenticated);
    }
}