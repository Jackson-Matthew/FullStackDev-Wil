namespace BlastPro.Api.Services.Interfaces;

public interface IAccountEmailDelivery
{
    Task PrepareAsync();
    Task SendInvitationAsync(string email, string encodedToken);
}
