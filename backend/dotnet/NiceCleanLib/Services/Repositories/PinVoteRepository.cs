using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using NiceCleanLib.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Repositories;

public class PinVoteRepository : IPinVoteRepository
{
    private readonly List<PinVote> _votes = new();
    private int _nextId = 1;

    public PinVote? AddVote(PinVote vote)
    {
        // Enforce the unique constraint in-memory
        if (HasUserVoted(vote.PinId, vote.UserId))
        {
            return null; // Or throw an exception
        }

        vote.Id = _nextId++;
        _votes.Add(vote);
        return vote;
    }

    public bool HasUserVoted(int pinId, int userId)
    {
        return _votes.Any(v => v.PinId == pinId && v.UserId == userId);
    }

    public int GetVoteCount(int pinId)
    {
        var pinVotes = _votes.Where(v => v.PinId == pinId);

        int confirmed = pinVotes.Count(v => v.VoteType == VoteType.Confirmed);
        int rejected = pinVotes.Count(v => v.VoteType == VoteType.Rejected);

        return confirmed - rejected;
    }
}
