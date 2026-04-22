using NiceCleanLib.Data;
using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class PinVoteRepositoryDB : IPinVoteRepository
{
    private readonly NiceCleanDbContext _context;

    public PinVoteRepositoryDB(NiceCleanDbContext context)
    {
        _context = context;
    }

    public PinVote? AddVote(PinVote vote)
    {
        try
        {
            if (HasUserVoted(vote.PinId, vote.UserId))
            {
                return null;
            }

            _context.PinVotes.Add(vote);
            _context.SaveChanges();
            return vote;
        }
        catch
        {
            return null;
        }
    }

    public bool HasUserVoted(int pinId, int userId)
    {
        return _context.PinVotes.Any(v => v.PinId == pinId && v.UserId == userId);
    }

    public int GetVoteCount(int pinId)
    {
        int confirmed = _context.PinVotes.Count(v => v.PinId == pinId && v.VoteType == VoteType.Confirmed);
        int rejected = _context.PinVotes.Count(v => v.PinId == pinId && v.VoteType == VoteType.Rejected);

        return confirmed - rejected;
    }
}
