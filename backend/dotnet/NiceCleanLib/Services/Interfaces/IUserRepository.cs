using NiceCleanLib.Models;

namespace NiceCleanLib.Services.Interfaces;

public interface IUserRepository
{
    User Add(User user);
    User? Delete(int id);
    List<User> GetAll();
    User? GetById(int id);
    User? Update(int id, User user);
    User? GetByEmail(string email);
}