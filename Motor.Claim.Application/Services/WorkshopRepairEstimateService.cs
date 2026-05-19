using System.Text.Json;
using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.Application.Services
{
    public class WorkshopRepairEstimateService
    {
        private const decimal StpAmountThreshold = 2000m;
        private readonly IWorkshopRepairEstimateRepository _estimateRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly IWorkshopPaymentRepository _paymentRepository;
        private readonly WorkshopPaymentService _workshopPaymentService;

        public WorkshopRepairEstimateService(
            IWorkshopRepairEstimateRepository estimateRepository,
            IClaimRepository claimRepository,
            IWorkshopPaymentRepository paymentRepository,
            WorkshopPaymentService workshopPaymentService)
        {
            _estimateRepository = estimateRepository;
            _claimRepository = claimRepository;
            _paymentRepository = paymentRepository;
            _workshopPaymentService = workshopPaymentService;
        }

        public async Task<WorkshopRepairEstimateEntity> SubmitAsync(Guid userId, Guid workshopId, SubmitWorkshopRepairEstimateRequest request)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(request.ClaimId);
            if (claim == null)
            {
                throw new ArgumentException("Claim not found.");
            }

            if (claim.WorkshopAppointment == null || claim.WorkshopAppointment.WorkshopId != workshopId)
            {
                throw new ArgumentException("This claim is not assigned to your workshop.");
            }

            var canRepair = string.Equals(claim.ReviewStatus, "Approved", StringComparison.OrdinalIgnoreCase)
                || claim.STPStatus == StpStatus.AutoApproved
                || claim.IsSTPApproved;

            if (!canRepair)
            {
                throw new ArgumentException("Repair estimate can only be submitted for approved claims.");
            }

            if (string.IsNullOrWhiteSpace(request.ReceiptOrQuotationDocument))
            {
                throw new ArgumentException("Receipt or quotation document is required.");
            }

            if (request.TotalAmount < 0)
            {
                throw new ArgumentException("Total amount cannot be negative.");
            }

            var existing = await _estimateRepository.GetByClaimIdAsync(request.ClaimId);

            if (existing == null)
            {
                existing = new WorkshopRepairEstimateEntity
                {
                    EstimateId = Guid.NewGuid(),
                    SubmittedAt = DateTime.UtcNow,
                    ClaimId = request.ClaimId,
                    WorkshopId = workshopId,
                    SubmittedByUserId = userId,
                    Status = "Submitted",
                    ReviewMode = "ManualReview"
                };

                ApplySubmission(existing, request);
                ApplyCoverageSplit(existing, claim.Coverage);
                await _estimateRepository.AddAsync(existing);
            }
            else
            {
                if (existing.WorkshopId != workshopId)
                {
                    throw new ArgumentException("This claim already has an estimate from another workshop.");
                }

                var existingPayment = await _paymentRepository.GetByEstimateIdAsync(existing.EstimateId);
                if (existingPayment != null)
                {
                    throw new ArgumentException("This repair estimate already has a payment recorded and cannot be resubmitted.");
                }

                ApplySubmission(existing, request);
                ApplyCoverageSplit(existing, claim.Coverage);
                existing.SubmittedByUserId = userId;
                existing.SubmittedAt = DateTime.UtcNow;
                ResetReview(existing);
                await _estimateRepository.UpdateAsync(existing);
            }

            var savedEstimate = (await _estimateRepository.GetByIdWithDetailsAsync(existing.EstimateId))!;

            if (savedEstimate.IsStpApproved || string.Equals(savedEstimate.Status, "StpApproved", StringComparison.OrdinalIgnoreCase))
            {
                await _workshopPaymentService.EnsurePaymentForApprovedEstimateAsync(savedEstimate);
                savedEstimate = (await _estimateRepository.GetByIdWithDetailsAsync(savedEstimate.EstimateId))!;
            }

            return savedEstimate;
        }

        public async Task<List<WorkshopRepairEstimateEntity>> GetAllAsync()
        {
            return await _estimateRepository.GetAllWithDetailsAsync();
        }

        public async Task<List<WorkshopRepairEstimateEntity>> GetByWorkshopIdAsync(Guid workshopId)
        {
            return await _estimateRepository.GetByWorkshopIdAsync(workshopId);
        }

        public async Task<WorkshopRepairEstimateEntity> ApproveAsync(Guid estimateId, Guid officerUserId, string? reviewNote)
        {
            var estimate = await GetExistingEstimateAsync(estimateId);
            estimate.Status = "Approved";
            estimate.ReviewMode = "ManualReview";
            estimate.IsStpApproved = false;
            estimate.ReviewNote = reviewNote;
            estimate.ReviewedByUserId = officerUserId;
            estimate.ReviewedAt = DateTime.UtcNow;
            await _workshopPaymentService.EnsurePaymentForApprovedEstimateAsync(estimate);
            await _estimateRepository.UpdateAsync(estimate);
            return estimate;
        }

        public async Task<WorkshopRepairEstimateEntity> RejectAsync(Guid estimateId, Guid officerUserId, string? reviewNote)
        {
            var estimate = await GetExistingEstimateAsync(estimateId);
            estimate.Status = "Rejected";
            estimate.ReviewMode = "ManualReview";
            estimate.IsStpApproved = false;
            estimate.ReviewNote = reviewNote;
            estimate.ReviewedByUserId = officerUserId;
            estimate.ReviewedAt = DateTime.UtcNow;
            await _estimateRepository.UpdateAsync(estimate);
            return estimate;
        }

        public async Task<WorkshopRepairEstimateEntity> RequestChangesAsync(Guid estimateId, Guid officerUserId, string requestedItems, string? reviewNote)
        {
            if (string.IsNullOrWhiteSpace(requestedItems))
            {
                throw new ArgumentException("Requested items are required.");
            }

            var estimate = await GetExistingEstimateAsync(estimateId);
            estimate.Status = "RevisionRequested";
            estimate.ReviewMode = "ManualReview";
            estimate.IsStpApproved = false;
            estimate.RequestedItems = requestedItems.Trim();
            estimate.ReviewNote = reviewNote;
            estimate.ReviewedByUserId = officerUserId;
            estimate.ReviewedAt = DateTime.UtcNow;
            await _estimateRepository.UpdateAsync(estimate);
            return estimate;
        }

        private async Task<WorkshopRepairEstimateEntity> GetExistingEstimateAsync(Guid estimateId)
        {
            var estimate = await _estimateRepository.GetByIdWithDetailsAsync(estimateId);
            if (estimate == null)
            {
                throw new ArgumentException("Repair estimate not found.");
            }

            return estimate;
        }

        private static void ApplySubmission(WorkshopRepairEstimateEntity estimate, SubmitWorkshopRepairEstimateRequest request)
        {
            estimate.TotalAmount = request.TotalAmount;
            estimate.ReceiptOrQuotationDocument = NormalizeOptional(request.ReceiptOrQuotationDocument);
            estimate.SupportingDocuments = SerializeList(request.SupportingDocuments);
            estimate.Remarks = NormalizeOptional(request.Remarks);

            if (estimate.TotalAmount <= StpAmountThreshold)
            {
                estimate.Status = "StpApproved";
                estimate.ReviewMode = "STP";
                estimate.IsStpApproved = true;
                estimate.ReviewNote = $"Approved - total amount is RM {estimate.TotalAmount:0.00}";
                estimate.RequestedItems = null;
                estimate.ReviewedByUserId = null;
                estimate.ReviewedAt = estimate.SubmittedAt;
            }
            else
            {
                estimate.Status = "PendingManualReview";
                estimate.ReviewMode = "ManualReview";
                estimate.IsStpApproved = false;
                estimate.ReviewNote = null;
                estimate.RequestedItems = null;
                estimate.ReviewedByUserId = null;
                estimate.ReviewedAt = null;
            }
        }

        private static void ApplyCoverageSplit(WorkshopRepairEstimateEntity estimate, CoverageEntity coverage)
        {
            var remainingCoverageAmount = Math.Max(coverage.CoverageLimitAmount - coverage.UsedClaimAmount, 0m);
            var insurancePayableAmount = Math.Min(estimate.TotalAmount, remainingCoverageAmount);

            estimate.InsurancePayableAmount = insurancePayableAmount;
            estimate.CustomerPayableAmount = estimate.TotalAmount - insurancePayableAmount;
            estimate.IsPartialCoverage = estimate.CustomerPayableAmount > 0m;
        }

        public static WorkshopRepairEstimateResponse MapResponse(WorkshopRepairEstimateEntity estimate)
        {
            return new WorkshopRepairEstimateResponse
            {
                EstimateId = estimate.EstimateId,
                ClaimId = estimate.ClaimId,
                WorkshopId = estimate.WorkshopId,
                WorkshopName = estimate.Workshop?.Name ?? string.Empty,
                SubmittedByUserId = estimate.SubmittedByUserId,
                TotalAmount = estimate.TotalAmount,
                InsurancePayableAmount = estimate.InsurancePayableAmount,
                CustomerPayableAmount = estimate.CustomerPayableAmount,
                IsPartialCoverage = estimate.IsPartialCoverage,
                ReceiptOrQuotationDocument = estimate.ReceiptOrQuotationDocument,
                SupportingDocuments = DeserializeList(estimate.SupportingDocuments),
                Remarks = estimate.Remarks,
                Status = estimate.Status,
                ReviewMode = estimate.ReviewMode,
                IsStpApproved = estimate.IsStpApproved,
                ReviewNote = estimate.ReviewNote,
                RequestedItems = estimate.RequestedItems,
                ReviewedByUserId = estimate.ReviewedByUserId,
                SubmittedAt = estimate.SubmittedAt,
                ReviewedAt = estimate.ReviewedAt
            };
        }

        private static void ResetReview(WorkshopRepairEstimateEntity estimate)
        {
            estimate.ReviewNote = null;
            estimate.RequestedItems = null;
            estimate.ReviewedByUserId = null;
            estimate.ReviewedAt = null;
            estimate.IsStpApproved = false;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? SerializeList(List<string>? values)
        {
            var normalized = values?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
        }

        private static List<string> DeserializeList(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(payload) ?? new List<string>();
            }
            catch
            {
                return new List<string> { payload.Trim() };
            }
        }
    }
}
