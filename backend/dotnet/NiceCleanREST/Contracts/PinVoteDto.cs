using NiceCleanLib.Enums;

namespace NiceCleanREST.Contracts;

public class PinVoteDto
{
    public int UserId { get; set; }
    public VoteType VoteType { get; set; }
}
