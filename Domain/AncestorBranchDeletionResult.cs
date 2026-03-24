namespace QuickGed.Domain;

public class AncestorBranchDeletionResult
{
    public int PersonId { get; set; }
    public HashSet<int> AncestorIds { get; set; } = new HashSet<int>();
    public HashSet<int> CandidateDeleteIds { get; set; } = new HashSet<int>();
    public HashSet<int> ExcludedIds { get; set; } = new HashSet<int>();
    public HashSet<int> FinalDeleteIds { get; set; } = new HashSet<int>();
    public int DeletedCount { get; set; }
}
