namespace Motor.Claim.Application.Dtos.Stp
{
    public class OcrExtractionResult
    {
        public bool IsSuccess { get; set; }
        public decimal Confidence { get; set; }
        public string? Name { get; set; }
        public string? ICNumber { get; set; }
        public string? VehicleNumber { get; set; }
        public string? RawText { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
