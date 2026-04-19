using Microsoft.Maui.Storage;

namespace NiceCleanApp.Services;

public class CredentialService : ICredentialService
{
    private const string EmailKey = "saved_email";
    private const string PasswordKey = "saved_password";

    public async Task SaveCredentialsAsync(string email, string password)
    {
        await SecureStorage.SetAsync(EmailKey, email);
        await SecureStorage.SetAsync(PasswordKey, password);
    }

    public async Task<(string? Email, string? Password)> GetCredentialsAsync()
    {
        try
        {
            var email = await SecureStorage.GetAsync(EmailKey);
            var password = await SecureStorage.GetAsync(PasswordKey);
            return (email, password);
        }
        catch
        {
            // SecureStorage may fail if device doesn't support it
            return (null, null);
        }
    }

    public void ClearCredentials()
    {
        SecureStorage.Remove(EmailKey);
        SecureStorage.Remove(PasswordKey);
    }
}