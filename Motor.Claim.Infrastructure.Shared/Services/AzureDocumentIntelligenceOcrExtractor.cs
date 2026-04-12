using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Motor.Claim.Application.Dtos.Stp;
using Motor.Claim.Application.Interfaces;

namespace Motor.Claim.Infrastructure.Shared.Services
{
    public class AzureDocumentIntelligenceOcrExtractor : IOcrExtractor
    {
        private const string ApiVersion = "2024-11-30";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly MockOcrExtractor _mockOcrExtractor;

        public AzureDocumentIntelligenceOcrExtractor(
            HttpClient httpClient,
            IConfiguration configuration,
            MockOcrExtractor mockOcrExtractor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _mockOcrExtractor = mockOcrExtractor;
        }

        public Task<OcrExtractionResult> ExtractIdentityDocumentAsync(string frontPath, string? backPath)
        {
            return ExtractStructuredDocumentAsync(frontPath, backPath, DocumentKind.Identity);
        }

        public Task<OcrExtractionResult> ExtractDrivingLicenseAsync(string frontPath, string? backPath)
        {
            return ExtractStructuredDocumentAsync(frontPath, backPath, DocumentKind.DrivingLicense);
        }

        public Task<OcrExtractionResult> ExtractVehicleOwnershipCertificateAsync(string filePath)
        {
            return ExtractStructuredDocumentAsync(filePath, null, DocumentKind.VehicleOwnershipCertificate);
        }

        public Task<OcrExtractionResult> ExtractPoliceReportAsync(string filePath)
        {
            return ExtractStructuredDocumentAsync(filePath, null, DocumentKind.PoliceReport);
        }

        private async Task<OcrExtractionResult> ExtractStructuredDocumentAsync(string primaryUrl, string? secondaryUrl, DocumentKind documentKind)
        {
            if (!IsConfigured())
            {
                return await _mockOcrExtractor.ExtractIdentityDocumentAsync(primaryUrl, secondaryUrl);
            }

            if (!Uri.TryCreate(primaryUrl, UriKind.Absolute, out _))
            {
                return new OcrExtractionResult
                {
                    IsSuccess = false,
                    Confidence = 0m,
                    ErrorMessage = "Document URL must be an absolute URL."
                };
            }

            var primaryResult = await AnalyzeUrlAsync(primaryUrl, documentKind);
            if (!primaryResult.IsSuccess || string.IsNullOrWhiteSpace(secondaryUrl))
            {
                return primaryResult;
            }

            if (!Uri.TryCreate(secondaryUrl, UriKind.Absolute, out _))
            {
                return primaryResult;
            }

            var secondaryResult = await AnalyzeUrlAsync(secondaryUrl, documentKind);
            if (!secondaryResult.IsSuccess)
            {
                return primaryResult;
            }

            return Merge(primaryResult, secondaryResult);
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(GetEndpoint()) &&
                   !string.IsNullOrWhiteSpace(GetApiKey());
        }

        private async Task<OcrExtractionResult> AnalyzeUrlAsync(string documentUrl, DocumentKind documentKind)
        {
            try
            {
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, BuildAnalyzeUri());
                    request.Headers.Add("Ocp-Apim-Subscription-Key", GetApiKey());
                    request.Content = new StringContent(
                        JsonSerializer.Serialize(new { urlSource = documentUrl }),
                        Encoding.UTF8,
                        "application/json");

                    using var response = await _httpClient.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(GetRetryDelay(response, attempt));
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return new OcrExtractionResult
                        {
                            IsSuccess = false,
                            Confidence = 0m,
                            ErrorMessage = $"Azure OCR analyze request failed with status {(int)response.StatusCode}."
                        };
                    }

                    if (!response.Headers.TryGetValues("Operation-Location", out var values))
                    {
                        return new OcrExtractionResult
                        {
                            IsSuccess = false,
                            Confidence = 0m,
                            ErrorMessage = "Azure OCR did not return Operation-Location."
                        };
                    }

                    var operationLocation = values.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(operationLocation))
                    {
                        return new OcrExtractionResult
                        {
                            IsSuccess = false,
                            Confidence = 0m,
                            ErrorMessage = "Azure OCR returned an empty Operation-Location."
                        };
                    }

                    return await PollAnalyzeResultAsync(operationLocation, documentKind);
                }

                return new OcrExtractionResult
                {
                    IsSuccess = false,
                    Confidence = 0m,
                    ErrorMessage = "Azure OCR analyze request hit rate limits repeatedly (429)."
                };
            }
            catch (Exception ex)
            {
                return new OcrExtractionResult
                {
                    IsSuccess = false,
                    Confidence = 0m,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<OcrExtractionResult> PollAnalyzeResultAsync(string operationLocation, DocumentKind documentKind)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                request.Headers.Add("Ocp-Apim-Subscription-Key", GetApiKey());
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(GetRetryDelay(response, attempt));
                    continue;
                }

                var payload = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new OcrExtractionResult
                    {
                        IsSuccess = false,
                        Confidence = 0m,
                        ErrorMessage = $"Azure OCR result request failed with status {(int)response.StatusCode}."
                    };
                }

                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;
                var status = root.GetProperty("status").GetString();

                if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    return ParseAnalyzeResult(root, documentKind);
                }

                if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    return new OcrExtractionResult
                    {
                        IsSuccess = false,
                        Confidence = 0m,
                        ErrorMessage = "Azure OCR analysis failed."
                    };
                }

                await Task.Delay(1000);
            }

            return new OcrExtractionResult
            {
                IsSuccess = false,
                Confidence = 0m,
                ErrorMessage = "Azure OCR analysis timed out."
            };
        }

        private static OcrExtractionResult ParseAnalyzeResult(JsonElement root, DocumentKind documentKind)
        {
            var analyzeResult = root.GetProperty("analyzeResult");
            var rawText = analyzeResult.TryGetProperty("content", out var contentElement)
                ? contentElement.GetString() ?? string.Empty
                : string.Empty;

            return new OcrExtractionResult
            {
                IsSuccess = !string.IsNullOrWhiteSpace(rawText),
                Confidence = GetAverageConfidence(analyzeResult),
                Name = ExtractName(rawText, documentKind),
                ICNumber = ExtractIcNumber(rawText),
                VehicleNumber = ExtractVehicleNumber(rawText, documentKind),
                RawText = rawText,
                ErrorMessage = string.IsNullOrWhiteSpace(rawText) ? "No OCR text extracted." : null
            };
        }

        private static decimal GetAverageConfidence(JsonElement analyzeResult)
        {
            if (!analyzeResult.TryGetProperty("pages", out var pagesElement) || pagesElement.ValueKind != JsonValueKind.Array)
            {
                return 0m;
            }

            decimal total = 0m;
            var count = 0;

            foreach (var page in pagesElement.EnumerateArray())
            {
                if (!page.TryGetProperty("words", out var wordsElement) || wordsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var word in wordsElement.EnumerateArray())
                {
                    if (!word.TryGetProperty("confidence", out var confidenceElement))
                    {
                        continue;
                    }

                    total += confidenceElement.GetDecimal();
                    count++;
                }
            }

            return count == 0 ? 0m : total / count;
        }

        private static OcrExtractionResult Merge(OcrExtractionResult primary, OcrExtractionResult secondary)
        {
            return new OcrExtractionResult
            {
                IsSuccess = primary.IsSuccess || secondary.IsSuccess,
                Confidence = Math.Max(primary.Confidence, secondary.Confidence),
                Name = primary.Name ?? secondary.Name,
                ICNumber = primary.ICNumber ?? secondary.ICNumber,
                VehicleNumber = primary.VehicleNumber ?? secondary.VehicleNumber,
                RawText = string.Join(Environment.NewLine, new[] { primary.RawText, secondary.RawText }.Where(x => !string.IsNullOrWhiteSpace(x))),
                ErrorMessage = primary.ErrorMessage ?? secondary.ErrorMessage
            };
        }

        private static string? ExtractName(string rawText, DocumentKind documentKind)
        {
            if (documentKind == DocumentKind.Identity)
            {
                var identityPatterns =
                    new[]
                    {
                        @"(?is)\b\d{6}[- ]?\d{2}[- ]?\d{4}\b\s+([A-Z][A-Z\s'/-]{3,})\s+(?:PTD|JALAN|LOT|NO\.?|WARGANEGARA|LELAKI|PEREMPUAN)",
                        @"(?im)^\s*([A-Z][A-Z\s'/-]{3,})\s*$[\r\n]+\s*(?:PTD|JALAN|LOT|NO\.?|WARGANEGARA|LELAKI|PEREMPUAN)"
                    };

                foreach (var pattern in identityPatterns)
                {
                    var match = Regex.Match(rawText, pattern);
                    if (match.Success)
                    {
                        return CleanupName(match.Groups[1].Value);
                    }
                }

                var lines = rawText
                    .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                for (var i = 0; i < lines.Count; i++)
                {
                    if (!Regex.IsMatch(lines[i], @"\b\d{6}[- ]?\d{2}[- ]?\d{4}\b|\b\d{12}\b"))
                    {
                        continue;
                    }

                    for (var j = i + 1; j < Math.Min(i + 5, lines.Count); j++)
                    {
                        if (IsLikelyPersonName(lines[j]))
                        {
                            return CleanupName(lines[j]);
                        }
                    }
                }
            }

            if (documentKind == DocumentKind.PoliceReport)
            {
                var complainantPatterns =
                    new[]
                    {
                        @"(?im)^\s*butir-butir\s+pengadu.*$[\r\n]+(?:.*[\r\n]+){0,6}?\s*nama\s*:\s*([A-Z][A-Z\s'./-]*?)(?=\s{2,}|No\s+Personel|Pangkat|No\s*K/P|Bahasa|$)",
                        @"(?im)^\s*nama\s*:\s*([A-Z][A-Z\s'./-]*?)(?=\s{2,}|No\s+Personel|Pangkat|No\s*K/P|Bahasa|$)"
                    };

                foreach (var pattern in complainantPatterns)
                {
                    var match = Regex.Match(rawText, pattern);
                    if (match.Success)
                    {
                        return CleanupName(match.Groups[1].Value);
                    }
                }
            }

            if (documentKind == DocumentKind.DrivingLicense)
            {
                var licensePatterns =
                    new[]
                    {
                        @"(?is)MALAYSIA\s+([A-Z][A-Z\s'/-]{3,})\s+WARGANEGARA\s*/\s*NATIONALITY",
                        @"(?im)^\s*([A-Z][A-Z\s'/-]{3,})\s*$[\r\n]+\s*WARGANEGARA\s*/\s*NATIONALITY"
                    };

                foreach (var pattern in licensePatterns)
                {
                    var match = Regex.Match(rawText, pattern);
                    if (match.Success)
                    {
                        return CleanupName(match.Groups[1].Value);
                    }
                }
            }

            var patterns =
                new[]
                {
                    @"(?im)^\s*(?:name|nama|insured\s+name|driver\s+name)\s*[:\-]\s*(.+?)\s*$",
                    @"(?im)^\s*(?:holder|owner)\s*[:\-]\s*(.+?)\s*$"
                };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(rawText, pattern);
                if (match.Success)
                {
                    return CleanupName(match.Groups[1].Value);
                }
            }

            var uppercaseLine = rawText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(IsLikelyPersonName)
                .OrderByDescending(GetNameCandidateScore)
                .FirstOrDefault();

            return uppercaseLine is null ? null : CleanupName(uppercaseLine);
        }

        private static string? ExtractIcNumber(string rawText)
        {
            var match = Regex.Match(rawText, @"\b\d{6}[- ]?\d{2}[- ]?\d{4}\b|\b\d{12}\b");
            return match.Success ? Normalize(match.Value) : null;
        }

        private static string? ExtractVehicleNumber(string rawText, DocumentKind documentKind)
        {
            var patterns =
                new[]
                {
                    @"(?im)^\s*(?:no\.\s*pendaftaran)\s*[:\-]\s*([A-Z0-9\s-]+)\s*$",
                    @"(?im)^\s*(?:vehicle\s*(?:registration)?\s*(?:number|no)|reg(?:istration)?\s*(?:number|no)|plate\s*(?:number|no))\s*[:\-]\s*([A-Z0-9\s-]+)\s*$",
                    @"\b[A-Z]{1,3}\s?\d{1,4}\s?[A-Z]{0,3}\b"
                };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(rawText.ToUpperInvariant(), pattern);
                if (match.Success)
                {
                    return Normalize(match.Groups[1].Success ? match.Groups[1].Value : match.Value);
                }
            }

            return null;
        }

        private static string CleanupName(string value)
        {
            var cleaned = value
                .Replace("  ", " ")
                .Trim(' ', ':', '-', '.', ',');

            cleaned = Regex.Replace(cleaned, @"\b(?:NO\s+PERSONEL|PANGKAT|NO\s*K/?P|BAHASA|NO\s+POLIS/TENTERA)\b.*$", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
            return cleaned;
        }

        private static bool IsLikelyPersonName(string value)
        {
            if (value.Length < 4 || value.Any(char.IsDigit))
            {
                return false;
            }

            if (value.Count(char.IsWhiteSpace) == 0)
            {
                return false;
            }

            if (!value.All(c => char.IsLetter(c) || char.IsWhiteSpace(c) || c is '\'' or '/' or '-'))
            {
                return false;
            }

            return !IgnoredNameLines.Contains(value.ToUpperInvariant());
        }

        private static int GetNameCandidateScore(string value)
        {
            var uppercase = value.ToUpperInvariant();
            var score = 0;

            if (uppercase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length is >= 2 and <= 5)
            {
                score += 4;
            }

            if (uppercase.Contains("BIN") || uppercase.Contains("BINTI"))
            {
                score += 2;
            }

            if (uppercase.Length is >= 8 and <= 40)
            {
                score += 2;
            }

            return score;
        }

        private static int GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            if (response.Headers.RetryAfter?.Delta is { } retryAfter && retryAfter > TimeSpan.Zero)
            {
                return (int)retryAfter.TotalMilliseconds;
            }

            return 1500 * (attempt + 1);
        }

        private enum DocumentKind
        {
            Identity,
            DrivingLicense,
            VehicleOwnershipCertificate,
            PoliceReport
        }

        private static readonly HashSet<string> IgnoredNameLines =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "KAD PENGENALAN",
                "MALAYSIA",
                "IDENTITY CARD",
                "MYKAD",
                "LESEN MEMANDU",
                "DRIVING LICENCE",
                "DRIVING LICENSE",
                "WARGANEGARA",
                "PEREMPUAN",
                "LELAKI",
                "POLIS DIRAJA MALAYSIA",
                "REPOT POLIS",
                "SIJIL PEMILIKAN KENDERAAN",
                "VEHICLE OWNERSHIP CERTIFICATE",
                "JABATAN PENGANGKUTAN JALAN MALAYSIA",
                "JOHOR",
                "MALAYSIA JOHOR"
            };

        private static string Normalize(string value)
        {
            return string.Concat(value.ToUpperInvariant().Where(char.IsLetterOrDigit));
        }

        private string BuildAnalyzeUri()
        {
            var endpoint = GetEndpoint()!.TrimEnd('/');
            var modelId = _configuration["Ocr:AzureDocumentIntelligence:ModelId"] ?? "prebuilt-layout";
            return $"{endpoint}/documentintelligence/documentModels/{modelId}:analyze?_overload=analyzeDocument&api-version={ApiVersion}";
        }

        private string? GetEndpoint()
        {
            return _configuration["Ocr:AzureDocumentIntelligence:Endpoint"];
        }

        private string? GetApiKey()
        {
            return _configuration["Ocr:AzureDocumentIntelligence:ApiKey"];
        }
    }
}
