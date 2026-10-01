namespace BlastPro.Api.Services.Interfaces;

public interface IAccountEmailDelivery
{
    Task PrepareAsync();
    Task SendConfirmationAsync(string email, string encodedToken);
    Task SendInvitationAsync(string email, string encodedToken);
}
