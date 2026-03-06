namespace NiceCleanLib.Models;

public class User
{
    public int UserId { get; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime Date { get; set; }
    public string Nickname { get; set; }
    public int NumberOfWalks { get; set; }
    public bool IsVerified { get; set; }

    public User(int userId, string email, string password, DateTime date, string nickname, int numberOfWalks, bool isVerified)
    {
        UserId = userId;
        Email = email;
        Password = password;
        Date = date;
        Nickname = nickname;
        NumberOfWalks = numberOfWalks;
        IsVerified = isVerified;
    }
}
