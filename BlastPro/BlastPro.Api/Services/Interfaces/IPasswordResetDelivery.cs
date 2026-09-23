namespace BlastPro.Api.Services.Interfaces;

public interface IPasswordResetDelivery
{
    Task PrepareAsync();
    Task SendAsync(string email, string encodedToken);
}
