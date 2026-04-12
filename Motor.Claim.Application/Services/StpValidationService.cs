using System.Text.Json;
using Motor.Claim.Application.Dtos.Stp;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Services
{
    public class StpValidationService
    {
        private const decimal MinimumConfidence = 0.80m;
        private readonly IOcrExtractor _ocrExtractor;

        public StpValidationService(IOcrExtractor ocrExtractor)
        {
            _ocrExtractor = ocrExtractor;
        }

        public async Task<StpValidationResultDto> ValidateAsync(ClaimEntity claim, CoverageEntity coverage)
        {
            var result = new StpValidationResultDto
            {
                IsDocumentComplete = true,
                IsIdentityMatched = false,
                IsVehicleMatched = false,
                IsPoliceReportMatched = false,
                IsDrivingLicenseMatched = false,
                AreEvidenceImagesPresent = true
            };

            var requiredDocuments = GetRequiredDocuments(claim);
            foreach (var (name, value) in requiredDocuments)
            {
                if (!HasValue(value))
                {
                    result.IsDocumentComplete = false;
                    result.Reasons.Add($"{name} is missing.");
                }
            }

            result.AreEvidenceImagesPresent = HasRequiredEvidenceImages(claim);

            OcrExtractionResult? identity = null;
            OcrExtractionResult? vehicleOwnership = null;
            OcrExtractionResult? policeReport = null;
            OcrExtractionResult? drivingLicense = null;
            var identityDiagnostic = CreateDocumentDiagnostic("Identity document", claim.IdentityDocumentFront);
            var vehicleDiagnostic = CreateDocumentDiagnostic("Vehicle ownership certificate", claim.VehicleOwnershipCertificateDocument);
            var policeDiagnostic = CreateDocumentDiagnostic("Police report", claim.PoliceReportDocument);
            var licenseDiagnostic = CreateDocumentDiagnostic("Driving license", claim.DrivingLicenseFront);

            if (HasValue(claim.IdentityDocumentFront))
            {
                identity = await _ocrExtractor.ExtractIdentityDocumentAsync(
                    claim.IdentityDocumentFront!,
                    claim.IdentityDocumentBack);
                EvaluateExtraction(result, identity, identityDiagnostic);
            }

            if (HasValue(claim.VehicleOwnershipCertificateDocument))
            {
                vehicleOwnership = await _ocrExtractor.ExtractVehicleOwnershipCertificateAsync(
                    claim.VehicleOwnershipCertificateDocument!);
                EvaluateExtraction(result, vehicleOwnership, vehicleDiagnostic);
            }

            if (HasValue(claim.PoliceReportDocument))
            {
                policeReport = await _ocrExtractor.ExtractPoliceReportAsync(claim.PoliceReportDocument!);
                EvaluateExtraction(result, policeReport, policeDiagnostic);
            }

            if (HasValue(claim.DrivingLicenseFront))
            {
                drivingLicense = await _ocrExtractor.ExtractDrivingLicenseAsync(
                    claim.DrivingLicenseFront!,
                    claim.DrivingLicenseBack);
                EvaluateExtraction(result, drivingLicense, licenseDiagnostic);
            }

            if (identity != null)
            {
                var identityMatchedByExtractedValue = HasValue(coverage.AuthorizedDriver) &&
                    StringsMatch(identity.Name, coverage.AuthorizedDriver);
                var identityMatchedByRawText = HasValue(coverage.AuthorizedDriver) &&
                    RawTextContains(identity.RawText, coverage.AuthorizedDriver);

                result.IsIdentityMatched = identityMatchedByExtractedValue || identityMatchedByRawText;
                identityDiagnostic.MatchTarget = coverage.AuthorizedDriver;
                identityDiagnostic.IsMatched = result.IsIdentityMatched;
                identityDiagnostic.MatchSource = identityMatchedByExtractedValue
                    ? "Extracted value"
                    : identityMatchedByRawText
                        ? "OCR text fallback"
                        : null;
                identityDiagnostic.MatchMessage = result.IsIdentityMatched
                    ? $"Matched Coverage.AuthorizedDriver via {identityDiagnostic.MatchSource}."
                    : "IC name does not match Coverage.AuthorizedDriver.";

                if (!result.IsIdentityMatched)
                {
                    result.Reasons.Add("IC name does not match Coverage.AuthorizedDriver.");
                }
            }

            if (vehicleOwnership != null)
            {
                var vehicleMatchedByExtractedValue = StringsMatch(vehicleOwnership.VehicleNumber, coverage.VehicleNo);
                var vehicleMatchedByRawText = RawTextContains(vehicleOwnership.RawText, coverage.VehicleNo);

                result.IsVehicleMatched = vehicleMatchedByExtractedValue || vehicleMatchedByRawText;
                vehicleDiagnostic.MatchTarget = coverage.VehicleNo;
                vehicleDiagnostic.IsMatched = result.IsVehicleMatched;
                vehicleDiagnostic.MatchSource = vehicleMatchedByExtractedValue
                    ? "Extracted value"
                    : vehicleMatchedByRawText
                        ? "OCR text fallback"
                        : null;
                vehicleDiagnostic.MatchMessage = result.IsVehicleMatched
                    ? $"Matched Coverage.VehicleNo via {vehicleDiagnostic.MatchSource}."
                    : "Vehicle number does not match Coverage.VehicleNo.";

                if (!result.IsVehicleMatched)
                {
                    result.Reasons.Add("Vehicle number does not match Coverage.VehicleNo.");
                }
            }

            if (identity != null && policeReport != null)
            {
                var policeMatchedByName = StringsMatch(policeReport.Name, identity.Name);
                var policeMatchedByIcNumber = StringsMatch(policeReport.ICNumber, identity.ICNumber);
                var policeMatchedByRawText = MatchUsingRawText(policeReport.RawText, identity.Name, identity.ICNumber);

                result.IsPoliceReportMatched =
                    policeMatchedByName ||
                    policeMatchedByIcNumber ||
                    policeMatchedByRawText;
                policeDiagnostic.MatchTarget = BuildMatchTarget(identity.Name, identity.ICNumber);
                policeDiagnostic.IsMatched = result.IsPoliceReportMatched;
                policeDiagnostic.MatchSource = policeMatchedByName
                    ? "Extracted name"
                    : policeMatchedByIcNumber
                        ? "Extracted IC number"
                        : policeMatchedByRawText
                            ? "OCR text fallback"
                            : null;
                policeDiagnostic.MatchMessage = result.IsPoliceReportMatched
                    ? $"Matched IC name or IC number via {policeDiagnostic.MatchSource}."
                    : "Police report does not match IC name or IC number.";

                if (!result.IsPoliceReportMatched)
                {
                    result.Reasons.Add("Police report does not match IC name or IC number.");
                }
            }

            if (identity != null && drivingLicense != null)
            {
                var licenseMatchedByName = StringsMatch(drivingLicense.Name, identity.Name);
                var licenseMatchedByIcNumber = StringsMatch(drivingLicense.ICNumber, identity.ICNumber);
                var licenseMatchedByRawText = MatchUsingRawText(drivingLicense.RawText, identity.Name, identity.ICNumber);

                result.IsDrivingLicenseMatched =
                    licenseMatchedByName ||
                    licenseMatchedByIcNumber ||
                    licenseMatchedByRawText;
                licenseDiagnostic.MatchTarget = BuildMatchTarget(identity.Name, identity.ICNumber);
                licenseDiagnostic.IsMatched = result.IsDrivingLicenseMatched;
                licenseDiagnostic.MatchSource = licenseMatchedByName
                    ? "Extracted name"
                    : licenseMatchedByIcNumber
                        ? "Extracted IC number"
                        : licenseMatchedByRawText
                            ? "OCR text fallback"
                            : null;
                licenseDiagnostic.MatchMessage = result.IsDrivingLicenseMatched
                    ? $"Matched IC name or IC number via {licenseDiagnostic.MatchSource}."
                    : "Driving license does not match IC name or IC number.";

                if (!result.IsDrivingLicenseMatched)
                {
                    result.Reasons.Add("Driving license does not match IC name or IC number.");
                }
            }

            result.DocumentDiagnostics.Add(identityDiagnostic);
            result.DocumentDiagnostics.Add(vehicleDiagnostic);
            result.DocumentDiagnostics.Add(policeDiagnostic);
            result.DocumentDiagnostics.Add(licenseDiagnostic);

            var criticalChecksPassed =
                result.IsDocumentComplete &&
                result.AreEvidenceImagesPresent &&
                identity != null &&
                vehicleOwnership != null &&
                policeReport != null &&
                (drivingLicense != null || IsTransientThrottleFailure(licenseDiagnostic.ErrorMessage)) &&
                result.IsIdentityMatched &&
                result.IsVehicleMatched &&
                result.IsPoliceReportMatched &&
                (result.IsDrivingLicenseMatched || IsTransientThrottleFailure(licenseDiagnostic.ErrorMessage)) &&
                HasNoCriticalExtractionFailure(result);

            result.IsApproved = criticalChecksPassed;
            result.STPStatus = criticalChecksPassed ? StpStatus.AutoApproved : StpStatus.ManualReview;

            return result;
        }

        public static string SerializeResult(StpValidationResultDto result)
        {
            return JsonSerializer.Serialize(result);
        }

        private static List<(string Name, string? Value)> GetRequiredDocuments(ClaimEntity claim)
        {
            var requiredDocuments = new List<(string Name, string? Value)>
            {
                ("PoliceReportDocument", claim.PoliceReportDocument),
                ("IdentityDocumentFront", claim.IdentityDocumentFront)
            };

            if (claim.AllClaimType == AllClaimType.VehicleClaim)
            {
                requiredDocuments.Add(("VehicleOwnershipCertificateDocument", claim.VehicleOwnershipCertificateDocument));
                requiredDocuments.Add(("DrivingLicenseFront", claim.DrivingLicenseFront));
            }

            return requiredDocuments;
        }

        private static bool HasRequiredEvidenceImages(ClaimEntity claim)
        {
            if (claim.AllClaimType == AllClaimType.VehicleClaim &&
                claim.MotorClaimType == MotorClaimType.VehicleDamages)
            {
                return true;
            }

            var hasAllEvidenceImages =
                HasValue(claim.VehicleDamageFrontLeftDocument) &&
                HasValue(claim.VehicleDamageFrontRightDocument) &&
                HasValue(claim.VehicleDamageRearLeftDocument) &&
                HasValue(claim.VehicleDamageRearRightDocument);

            return hasAllEvidenceImages;
        }

        private static void EvaluateExtraction(StpValidationResultDto result, OcrExtractionResult extraction, OcrDocumentDiagnosticDto diagnostic)
        {
            diagnostic.OcrSucceeded = extraction.IsSuccess;
            diagnostic.Confidence = extraction.Confidence;
            diagnostic.ConfidencePassed = extraction.Confidence >= MinimumConfidence;
            diagnostic.ErrorMessage = extraction.ErrorMessage;
            diagnostic.ExtractedName = extraction.Name;
            diagnostic.ExtractedVehicleNumber = extraction.VehicleNumber;

            if (!extraction.IsSuccess)
            {
                result.Reasons.Add($"{diagnostic.DocumentName} OCR failed: {extraction.ErrorMessage ?? "Unknown OCR error"}");
                return;
            }

            if (extraction.Confidence < MinimumConfidence)
            {
                result.Reasons.Add($"{diagnostic.DocumentName} OCR confidence is too low.");
            }
        }

        private static OcrDocumentDiagnosticDto CreateDocumentDiagnostic(string documentName, string? documentValue)
        {
            return new OcrDocumentDiagnosticDto
            {
                DocumentName = documentName,
                Provided = HasValue(documentValue)
            };
        }

        private static bool HasNoCriticalExtractionFailure(StpValidationResultDto result)
        {
            return !result.Reasons.Any(x =>
                (x.Contains("OCR failed", StringComparison.OrdinalIgnoreCase) && !x.Contains("429", StringComparison.OrdinalIgnoreCase)) ||
                x.Contains("confidence is too low", StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool StringsMatch(string? left, string? right)
        {
            if (!HasValue(left) || !HasValue(right))
            {
                return false;
            }

            var normalizedLeft = Normalize(left!);
            var normalizedRight = Normalize(right!);
            if (normalizedLeft == normalizedRight)
            {
                return true;
            }

            if (normalizedLeft.Contains(normalizedRight, StringComparison.Ordinal) ||
                normalizedRight.Contains(normalizedLeft, StringComparison.Ordinal))
            {
                return true;
            }

            return IsSmallOcrEditDistance(normalizedLeft, normalizedRight);
        }

        private static string Normalize(string value)
        {
            return string.Concat(value
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit));
        }

        private static bool IsSmallOcrEditDistance(string left, string right)
        {
            if (Math.Abs(left.Length - right.Length) > 2)
            {
                return false;
            }

            if (left.Length < 6 || right.Length < 6)
            {
                return false;
            }

            var distance = LevenshteinDistance(left, right);
            return distance <= 2;
        }

        private static bool RawTextContains(string? rawText, string? target)
        {
            if (!HasValue(rawText) || !HasValue(target))
            {
                return false;
            }

            var normalizedRawText = Normalize(rawText!);
            var normalizedTarget = Normalize(target!);

            return normalizedRawText.Contains(normalizedTarget, StringComparison.Ordinal) ||
                   normalizedTarget.Contains(normalizedRawText, StringComparison.Ordinal);
        }

        private static bool MatchUsingRawText(string? rawText, string? name, string? icNumber)
        {
            return RawTextContains(rawText, name) || RawTextContains(rawText, icNumber);
        }

        private static bool IsTransientThrottleFailure(string? errorMessage)
        {
            return HasValue(errorMessage) &&
                   errorMessage!.Contains("429", StringComparison.OrdinalIgnoreCase);
        }

        private static int LevenshteinDistance(string left, string right)
        {
            var matrix = new int[left.Length + 1, right.Length + 1];

            for (var i = 0; i <= left.Length; i++)
            {
                matrix[i, 0] = i;
            }

            for (var j = 0; j <= right.Length; j++)
            {
                matrix[0, j] = j;
            }

            for (var i = 1; i <= left.Length; i++)
            {
                for (var j = 1; j <= right.Length; j++)
                {
                    var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[left.Length, right.Length];
        }

        private static string? BuildMatchTarget(string? name, string? icNumber)
        {
            var parts = new List<string>();

            if (HasValue(name))
            {
                parts.Add($"Name: {name}");
            }

            if (HasValue(icNumber))
            {
                parts.Add($"IC: {icNumber}");
            }

            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }
    }
}
