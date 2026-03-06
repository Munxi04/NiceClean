namespace NiceCleanLib.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime Date { get; set; }
    public string Nickname { get; set; }
    public int NumberOfWalks { get; set; }
    public bool IsVerified { get; set; }

    public User(int id, string email, string password, DateTime date, string nickname, int numberOfWalks, bool isVerified)
    {
        Id = id;
        Email = email;
        Password = password;
        Date = date;
        Nickname = nickname;
        NumberOfWalks = numberOfWalks;
        IsVerified = isVerified;
    }
}
