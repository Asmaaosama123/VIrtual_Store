namespace Vstore.Services
{
    public interface IAuthService
    {
        Task<AuthModel> RegisterAsync(NewUser newUser);
    }
}
