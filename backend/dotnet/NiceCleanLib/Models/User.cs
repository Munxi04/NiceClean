namespace NiceCleanLib.Models;

/// <summary>
/// User model representing a registered user account.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    /// <summary>
    /// Stores BCrypt-hashed password (never plaintext). BCrypt hashes are ~60 characters.
    /// </summary>
    public string Password { get; set; }
    public DateTime Age { get; set; }
    public string Nickname { get; set; }
    public int NumberOfWalks { get; set; }
    public bool IsVerified { get; set; }

    public User(int id, string email, string password, DateTime age, string nickname, int numberOfWalks, bool isVerified)
    {
        Id = id;
        Email = email;
        Password = password;
        Age = age;
        Nickname = nickname;
        NumberOfWalks = numberOfWalks;
        IsVerified = isVerified;
    }
}
