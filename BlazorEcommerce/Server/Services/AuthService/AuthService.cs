namespace BlazorEcommerce.Server.Services.AuthService;

using System.Security.Cryptography;

public class AuthService : IAuthService
{
    private readonly DataContext _dataContext;

    public AuthService(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<ServiceResponse<int>> Register(User user, string password)
    {
        if (await UserExists(user.Email))
        {
            return new ServiceResponse<int> { Success = false, Message = "user already exists." }; 
        }
        
        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;

        _dataContext.Users.Add(user);
        await _dataContext.SaveChangesAsync();

        return new ServiceResponse<int> { Data = user.Id };
    }

    public async Task<bool> UserExists(string email)
    {
        if (await _dataContext.Users.AnyAsync(user => user.Email.ToLower().Equals(email.ToLower())))
        {
            return true;
        }

        return false;
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}