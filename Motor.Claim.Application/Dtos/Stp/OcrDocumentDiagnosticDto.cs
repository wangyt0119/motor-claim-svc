namespace Motor.Claim.Application.Dtos.Stp
{
    public class OcrDocumentDiagnosticDto
    {
        public string DocumentName { get; set; } = string.Empty;
        public bool Provided { get; set; }
        public bool OcrSucceeded { get; set; }
        public decimal Confidence { get; set; }
        public bool ConfidencePassed { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ExtractedName { get; set; }
        public string? ExtractedVehicleNumber { get; set; }
        public string? MatchTarget { get; set; }
        public bool? IsMatched { get; set; }
        public string? MatchMessage { get; set; }
        public string? MatchSource { get; set; }
    }
}
