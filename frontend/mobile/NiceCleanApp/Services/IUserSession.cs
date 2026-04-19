namespace NiceCleanApp.Services;

public interface IUserSession
{
    User? CurrentUser { get; set; }
    bool IsLoggedIn { get; }
    void Clear();
}