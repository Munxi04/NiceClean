namespace NiceCleanApp.Services;

public class UserSession : IUserSession
{
    public User? CurrentUser { get; set; }
    public bool IsLoggedIn => CurrentUser != null;

    public void Clear()
    {
        CurrentUser = null;
    }
}