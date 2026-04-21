using NiceCleanLib.Enums;
using NiceCleanLib.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NiceCleanLib.Services.Interfaces;

public interface IPinVoteRepository
{
    PinVote? AddVote(PinVote vote);
    bool HasUserVoted(int pinId, int userId);
    int GetVoteCount(int pinId, VoteType voteType);
}
