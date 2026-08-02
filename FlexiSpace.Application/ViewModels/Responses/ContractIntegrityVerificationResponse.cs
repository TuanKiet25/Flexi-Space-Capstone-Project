namespace FlexiSpace.Application.ViewModels.Responses
{
    public class ContractIntegrityVerificationResponse
    {
        public long ContractId { get; set; }
        public string? OldPostSignHash { get; set; }
        public string? NewPostSignHash { get; set; }
        public string? StoredPostSignSnapshotHash { get; set; }
        public bool IsStoredSnapshotMatched { get; set; }
        public bool IsMatched { get; set; }
        public bool IsTampered { get; set; }
        public string Verdict { get; set; } = string.Empty;
        public DateTime VerifiedAt { get; set; }
    }
}
