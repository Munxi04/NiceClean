namespace NiceCleanApp.Services;

public interface ICredentialService
{
    Task SaveCredentialsAsync(string email, string password);
    Task<(string? Email, string? Password)> GetCredentialsAsync();
    void ClearCredentials();
}