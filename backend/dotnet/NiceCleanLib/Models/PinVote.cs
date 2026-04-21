using NiceCleanLib.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Models;

public class PinVote
{
    public int Id { get; set; }
    public int PinId { get; set; }
    public int UserId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime CreatedAt { get; set; }

    public PinVote(int id, int pinId, int userId, VoteType voteType, DateTime createdAt)
    {
        Id = id;
        PinId = pinId;
        UserId = userId;
        VoteType = voteType;
        CreatedAt = createdAt;
    }
}
