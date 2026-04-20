using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;

namespace Motor.Claim.Application.Services
{
    public class WorkshopPaymentService
    {
        private readonly IWorkshopPaymentRepository _paymentRepository;
        private readonly IWorkshopPaymentProviderResolver _providerResolver;

        public WorkshopPaymentService(
            IWorkshopPaymentRepository paymentRepository,
            IWorkshopPaymentProviderResolver providerResolver)
        {
            _paymentRepository = paymentRepository;
            _providerResolver = providerResolver;
        }

        public async Task<WorkshopPaymentEntity> EnsurePaymentForApprovedEstimateAsync(WorkshopRepairEstimateEntity estimate)
        {
            if (!IsApproved(estimate))
            {
                throw new ArgumentException("Payment can only be created for approved repair estimates.");
            }

            if (estimate.Workshop == null)
            {
                throw new ArgumentException("Workshop details are required before creating a payment.");
            }

            if (string.IsNullOrWhiteSpace(estimate.Workshop.BankName)
                || string.IsNullOrWhiteSpace(estimate.Workshop.BankAccountNumber)
                || string.IsNullOrWhiteSpace(estimate.Workshop.BankAccountHolderName))
            {
                throw new ArgumentException("Workshop bank details are incomplete.");
            }

            var existingPayment = await _paymentRepository.GetByEstimateIdAsync(estimate.EstimateId);
            if (existingPayment != null)
            {
                return existingPayment;
            }

            var provider = await _providerResolver.ResolveAsync(estimate);

            var payment = new WorkshopPaymentEntity
            {
                PaymentId = Guid.NewGuid(),
                EstimateId = estimate.EstimateId,
                ClaimId = estimate.ClaimId,
                WorkshopId = estimate.WorkshopId,
                Amount = estimate.TotalAmount,
                Currency = "MYR",
                Status = provider.Status,
                Provider = provider.Provider,
                ApprovalSource = estimate.IsStpApproved ? "STP" : "ManualReview",
                ProviderReference = provider.ProviderReference,
                BankNameSnapshot = estimate.Workshop.BankName?.Trim(),
                BankAccountNumberSnapshot = estimate.Workshop.BankAccountNumber?.Trim(),
                BankAccountHolderNameSnapshot = estimate.Workshop.BankAccountHolderName?.Trim(),
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };

            return await _paymentRepository.AddAsync(payment);
        }

        public async Task<List<WorkshopPaymentEntity>> GetAllAsync()
        {
            return await _paymentRepository.GetAllWithDetailsAsync();
        }

        public async Task<List<WorkshopPaymentEntity>> GetByWorkshopIdAsync(Guid workshopId)
        {
            return await _paymentRepository.GetByWorkshopIdAsync(workshopId);
        }

        public async Task<WorkshopPaymentEntity?> GetByEstimateIdAsync(Guid estimateId)
        {
            return await _paymentRepository.GetByEstimateIdAsync(estimateId);
        }

        public static WorkshopPaymentResponse MapResponse(WorkshopPaymentEntity payment)
        {
            return new WorkshopPaymentResponse
            {
                PaymentId = payment.PaymentId,
                EstimateId = payment.EstimateId,
                ClaimId = payment.ClaimId,
                WorkshopId = payment.WorkshopId,
                WorkshopName = payment.Workshop?.Name ?? string.Empty,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                Provider = payment.Provider,
                ApprovalSource = payment.ApprovalSource,
                ProviderReference = payment.ProviderReference,
                BankNameSnapshot = payment.BankNameSnapshot,
                BankAccountNumberSnapshot = payment.BankAccountNumberSnapshot,
                BankAccountHolderNameSnapshot = payment.BankAccountHolderNameSnapshot,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                PaidAt = payment.PaidAt
            };
        }

        private static bool IsApproved(WorkshopRepairEstimateEntity estimate)
        {
            return string.Equals(estimate.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(estimate.Status, "StpApproved", StringComparison.OrdinalIgnoreCase)
                || estimate.IsStpApproved;
        }
    }
}
