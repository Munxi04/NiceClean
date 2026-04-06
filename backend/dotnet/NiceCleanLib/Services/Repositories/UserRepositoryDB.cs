using NiceCleanLib.Data;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class UserRepositoryDB : IUserRepository
{
    private readonly NiceCleanDbContext _context;

    public UserRepositoryDB(NiceCleanDbContext context)
    {
        _context = context;
    }

    public List<User> GetAll()
    {
        return _context.Users.ToList();
    }

    public User? GetById(int id)
    {
        return _context.Users.Find(id);
    }

    public User Add(User user)
    {
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    public User? GetByEmail(string email)
    {
        return _context.Users.FirstOrDefault(u => u.Email == email);
    }

    public User? Update(int id, User user)
    {
        var existing = _context.Users.Find(id);

        if (existing == null)
        {
            return null;
        }

        existing.Email = user.Email;
        existing.Password = user.Password;
        existing.Age = user.Age;
        existing.Nickname = user.Nickname;

        _context.SaveChanges();
        return existing;
    }

    public User? Delete(int id)
    {
        var user = GetById(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
        return user;
    }

}