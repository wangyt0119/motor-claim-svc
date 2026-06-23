using System.Net;

namespace Motor.Claim.WebApi.Services
{
    public class GeminiDamageAssessmentException : Exception
    {
        public GeminiDamageAssessmentException(HttpStatusCode statusCode, string responseBody)
            : base($"Gemini damage assessment failed: {(int)statusCode} {responseBody}")
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
