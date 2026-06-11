namespace Celleseum.Web.Services;

public interface IAccountEmailSender
{
    Task<bool> SendEmailConfirmationAsync(string email, string confirmationLink);
}
