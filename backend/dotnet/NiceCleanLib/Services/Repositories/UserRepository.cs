using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NiceCleanLib.Services.Repositories;

public class UserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    // Dummy data with 4 initial users for testing purposes
    public UserRepository()
    {
        Add(new User(
            id: 0,
            email: "test@test.com",
            password: "123",
            age: new DateTime(1990, 5, 15),
            nickname: "SuperUser",
            numberOfWalks: 12,
            isVerified: true
        ));

        Add(new User(
            id: 0,
            email: "bel@bel.com",
            password: "123",
            age: new DateTime(1985, 10, 20),
            nickname: "Bel",
            numberOfWalks: 2,
            isVerified: false
        ));

        Add(new User(
            id: 0,
            email: "jul@jul.com",
            password: "123",
            age: new DateTime(1995, 1, 10),
            nickname: "Jul",
            numberOfWalks: 0,
            isVerified: false
        ));

        Add(new User(
            id: 0,
            email: "pat@pat.com",
            password: "123",
            age: new DateTime(1995, 1, 10),
            nickname: "Pat",
            numberOfWalks: 0,
            isVerified: false
        ));
    }

    public List<User> GetAll()
    {
        return _users;
    }

    public User? GetById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public User? GetByEmail(string email)
    {
        return _users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public User Add(User user)
    {
        user.Id = _nextId++;
        _users.Add(user);
        return user;
    }

    public User? Update(int id, User user)
    {
        var existing = GetById(id);
        if (existing == null)
        {
            return null;
        }

        var index = _users.IndexOf(existing);

        user.Id = id;

        _users[index] = user;
        return user;
    }

    public User? Delete(int id)
    {
        User? user = GetById(id);
        if (user != null)
        {
            _users.Remove(user);
        }
        return user;
    }
}
