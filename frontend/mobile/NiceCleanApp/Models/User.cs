namespace NiceCleanApp.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime Age { get; set; }
    public string Nickname { get; set; }
    public int NumberOfWalks { get; set; }
    public bool IsVerified { get; set; }
}
