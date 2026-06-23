using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Motor.Claim.Application.Dtos.Coverage;
using Motor.Claim.Application.Dtos.DamageAssessment;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.WebApi.Configuration;

namespace Motor.Claim.WebApi.Services
{
    public class GeminiDamageAssessmentService : IGeminiDamageAssessmentService
    {
        private const int MaxAttempts = 4;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;

        public GeminiDamageAssessmentService(HttpClient httpClient, IOptions<GeminiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<DamageAssessmentResponse> AssessAsync(GeminiDamageAssessmentInput input, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            if (input.ImageBytes.Length == 0)
            {
                throw new ArgumentException("Image is required.");
            }

            var prompt = BuildPrompt(input.Coverage, input.CustomerMessage);
            var request = new GeminiGenerateContentRequest
            {
                Contents = new List<GeminiContent>
                {
                    new()
                    {
                        Role = "user",
                        Parts = new List<GeminiPart>
                        {
                            new()
                            {
                                InlineData = new GeminiInlineData
                                {
                                    MimeType = input.ImageMimeType,
                                    Data = Convert.ToBase64String(input.ImageBytes)
                                }
                            },
                            new()
                            {
                                Text = prompt
                            }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.2m,
                    ResponseMimeType = "application/json"
                }
            };

            using var response = await SendWithRetryAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new GeminiDamageAssessmentException(response.StatusCode, responseBody);
            }

            var responseText = ExtractResponseText(responseBody);
            var payload = DeserializePayload(responseText);

            return MapResponse(payload, responseText, input.Coverage);
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(
            GeminiGenerateContentRequest request,
            CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using var httpRequest = CreateHttpRequest(request);
                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

                if (response.IsSuccessStatusCode ||
                    !IsTransientStatusCode(response.StatusCode) ||
                    attempt == MaxAttempts)
                {
                    return response;
                }

                response.Dispose();
                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }

            throw new InvalidOperationException("Unexpected Gemini retry loop termination.");
        }

        private HttpRequestMessage CreateHttpRequest(GeminiGenerateContentRequest request)
        {
            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/models/{Uri.EscapeDataString(_options.Model)}:generateContent");
            httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);
            httpRequest.Content = JsonContent.Create(request, options: JsonOptions);
            return httpRequest;
        }

        private static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            return statusCode is
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;
        }

        private static TimeSpan GetRetryDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }

        private static string BuildPrompt(CoverageEntity coverage, string? customerMessage)
        {
            return $$"""
                You are an AI-assisted motor insurance damage assessor for a Malaysia motor claim customer portal.
                Analyze the uploaded car damage image and estimate a preliminary repair quotation in MYR.

                Coverage details:
                - Vehicle: {{coverage.Year}} {{coverage.VehicleMake}} {{coverage.VehicleModel}} {{coverage.ModelType}}
                - Vehicle no: {{coverage.VehicleNo}}
                - Coverage type: {{coverage.CoverageType}}
                - Effective date: {{coverage.EffectiveDate:yyyy-MM-dd}}
                - Expiry date: {{coverage.ExpiryDate:yyyy-MM-dd}}
                - Coverage limit: {{coverage.CoverageLimitAmount.ToString("0.00", CultureInfo.InvariantCulture)}}
                - Used claim amount: {{coverage.UsedClaimAmount.ToString("0.00", CultureInfo.InvariantCulture)}}
                - Remaining coverage amount: {{coverage.RemainingCoverageAmount.ToString("0.00", CultureInfo.InvariantCulture)}}

                Customer message:
                {{(string.IsNullOrWhiteSpace(customerMessage) ? "No extra customer message provided." : customerMessage)}}

                Return valid JSON only with this exact shape:
                {
                  "damageSummary": "brief plain-language summary",
                  "severity": "Minor|Moderate|Severe|Unknown",
                  "estimatedRepairCost": 0,
                  "confidenceScore": 0.0,
                  "detectedDamageAreas": ["front bumper"],
                  "lineItems": [
                    {
                      "area": "front bumper",
                      "damageType": "scratch/dent/crack/etc",
                      "recommendedRepair": "repair action",
                      "estimatedCost": 0
                    }
                  ],
                  "safetyNotes": ["notes if vehicle may be unsafe to drive"],
                  "disclaimer": "short reminder that this is an AI preliminary estimate and workshop/officer review is required"
                }

                Rules:
                - Use only visible image evidence and coverage context.
                - If damage is not visible or image is not a car damage photo, set severity to "Unknown" and estimatedRepairCost to 0.
                - Keep estimated costs realistic for Malaysia workshop pricing.
                - Do not decide claim approval. Only estimate repair cost.
                """;
        }

        private static string ExtractResponseText(string responseBody)
        {
            using var document = JsonDocument.Parse(responseBody);
            var parts = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            var textParts = new List<string>();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    textParts.Add(textElement.GetString() ?? string.Empty);
                }
            }

            return string.Join(Environment.NewLine, textParts).Trim();
        }

        private static GeminiDamageAssessmentPayload DeserializePayload(string responseText)
        {
            var cleaned = responseText.Trim();
            if (cleaned.StartsWith("```", StringComparison.Ordinal))
            {
                cleaned = cleaned.Trim('`').Trim();
                if (cleaned.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned[4..].Trim();
                }
            }

            return JsonSerializer.Deserialize<GeminiDamageAssessmentPayload>(cleaned, JsonOptions)
                ?? new GeminiDamageAssessmentPayload();
        }

        private static DamageAssessmentResponse MapResponse(
            GeminiDamageAssessmentPayload payload,
            string rawResponse,
            CoverageEntity coverage)
        {
            var estimatedRepairCost = Math.Max(0m, payload.EstimatedRepairCost);
            var insurancePayable = Math.Min(estimatedRepairCost, Math.Max(0m, coverage.RemainingCoverageAmount));
            var customerPayable = Math.Max(0m, estimatedRepairCost - insurancePayable);

            return new DamageAssessmentResponse
            {
                CoverageEligibility = new DamageAssessmentCoverageEligibilityResponse
                {
                    IsEligible = true,
                    Message = "Coverage is active and matches the submitted vehicle details.",
                    Coverage = MapCoverage(coverage)
                },
                DamageSummary = payload.DamageSummary,
                Severity = payload.Severity,
                EstimatedRepairCost = estimatedRepairCost,
                InsurancePayableAmount = insurancePayable,
                CustomerPayableAmount = customerPayable,
                IsPartialCoverage = customerPayable > 0m,
                ConfidenceScore = Math.Clamp(payload.ConfidenceScore, 0m, 1m),
                DetectedDamageAreas = payload.DetectedDamageAreas ?? new List<string>(),
                SafetyNotes = payload.SafetyNotes ?? new List<string>(),
                Disclaimer = string.IsNullOrWhiteSpace(payload.Disclaimer)
                    ? "This is an AI-assisted preliminary estimate and must be reviewed by a workshop or claim officer."
                    : payload.Disclaimer,
                LineItems = (payload.LineItems ?? new List<GeminiDamageAssessmentLineItemPayload>())
                    .Select(x => new DamageAssessmentLineItemResponse
                    {
                        Item = BuildLineItemLabel(x),
                        Area = x.Area,
                        DamageType = x.DamageType,
                        RecommendedRepair = x.RecommendedRepair,
                        EstimatedCost = Math.Max(0m, x.EstimatedCost)
                    })
                    .ToList(),
                RawAiResponse = rawResponse
            };
        }

        private static string BuildLineItemLabel(GeminiDamageAssessmentLineItemPayload lineItem)
        {
            var area = lineItem.Area?.Trim() ?? string.Empty;
            var damageType = lineItem.DamageType?.Trim() ?? string.Empty;
            var recommendedRepair = lineItem.RecommendedRepair?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(area) && !string.IsNullOrWhiteSpace(recommendedRepair))
            {
                return $"{area}: {recommendedRepair}";
            }

            if (!string.IsNullOrWhiteSpace(area) && !string.IsNullOrWhiteSpace(damageType))
            {
                return $"{area}: {damageType}";
            }

            return area.Length > 0
                ? area
                : recommendedRepair.Length > 0
                    ? recommendedRepair
                    : damageType;
        }

        private static CoverageResponse MapCoverage(CoverageEntity coverage)
        {
            return new CoverageResponse
            {
                CoverageId = coverage.CoverageId,
                CreatedAt = coverage.CreatedAt,
                UserId = coverage.UserId,
                InsuredPersonName = coverage.InsuredPersonName,
                VehicleNo = coverage.VehicleNo,
                VehicleMake = coverage.VehicleMake,
                VehicleModel = coverage.VehicleModel,
                Year = coverage.Year,
                ModelType = coverage.ModelType,
                CoverageType = coverage.CoverageType,
                AuthorizedDriver = coverage.AuthorizedDriver,
                EffectiveDate = coverage.EffectiveDate,
                ExpiryDate = coverage.ExpiryDate,
                CoverageLimitAmount = coverage.CoverageLimitAmount,
                UsedClaimAmount = coverage.UsedClaimAmount,
                RemainingCoverageAmount = coverage.RemainingCoverageAmount,
                WindscreenCoverageLimitAmount = coverage.WindscreenCoverageLimitAmount,
                WindscreenUsedClaimAmount = coverage.WindscreenUsedClaimAmount,
                WindscreenRemainingCoverageAmount = coverage.WindscreenRemainingCoverageAmount
            };
        }

        private class GeminiGenerateContentRequest
        {
            [JsonPropertyName("contents")]
            public List<GeminiContent> Contents { get; set; } = new();

            [JsonPropertyName("generationConfig")]
            public GeminiGenerationConfig GenerationConfig { get; set; } = new();
        }

        private class GeminiContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "user";

            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Text { get; set; }

            [JsonPropertyName("inline_data")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public GeminiInlineData? InlineData { get; set; }
        }

        private class GeminiInlineData
        {
            [JsonPropertyName("mime_type")]
            public string MimeType { get; set; } = string.Empty;

            [JsonPropertyName("data")]
            public string Data { get; set; } = string.Empty;
        }

        private class GeminiGenerationConfig
        {
            [JsonPropertyName("temperature")]
            public decimal Temperature { get; set; }

            [JsonPropertyName("responseMimeType")]
            public string ResponseMimeType { get; set; } = "application/json";
        }

        private class GeminiDamageAssessmentPayload
        {
            public string DamageSummary { get; set; } = string.Empty;
            public string Severity { get; set; } = "Unknown";
            public decimal EstimatedRepairCost { get; set; }
            public decimal ConfidenceScore { get; set; }
            public List<string>? DetectedDamageAreas { get; set; }
            public List<GeminiDamageAssessmentLineItemPayload>? LineItems { get; set; }
            public List<string>? SafetyNotes { get; set; }
            public string Disclaimer { get; set; } = string.Empty;
        }

        private class GeminiDamageAssessmentLineItemPayload
        {
            public string Area { get; set; } = string.Empty;
            public string DamageType { get; set; } = string.Empty;
            public string RecommendedRepair { get; set; } = string.Empty;
            public decimal EstimatedCost { get; set; }
        }
    }
}
