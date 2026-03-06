using NiceCleanLib.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace NiceCleanLib.Services.Repositories;

public class UserRepository
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public List<User> GetAll()
    {
        return _users;
    }

    public User? GetById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
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
