using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Workshop;
using Motor.Claim.Application.Features.Workshop.Commands;
using Motor.Claim.Application.Features.Workshop.Queries;
using Motor.Claim.Application.Features.WorkshopAppointment.Commands;
using Motor.Claim.Application.Features.WorkshopAppointment.Queries;
using Motor.Claim.Application.Services;
using System.Text.Json;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkshopController : ControllerBase
    {
        private readonly GetPanelWorkshopStatesQueryHandler _getPanelWorkshopStatesQueryHandler;
        private readonly GetPanelWorkshopsByStateQueryHandler _getPanelWorkshopsByStateQueryHandler;
        private readonly GetAllWorkshopsQueryHandler _getAllWorkshopsQueryHandler;
        private readonly GetApprovedClaimsForPanelWorkshopQueryHandler _getApprovedClaimsForPanelWorkshopQueryHandler;
        private readonly CreateWorkshopCommandHandler _createWorkshopCommandHandler;
        private readonly UpdateWorkshopCommandHandler _updateWorkshopCommandHandler;
        private readonly DeleteWorkshopCommandHandler _deleteWorkshopCommandHandler;
        private readonly CreateOrUpdateWorkshopAppointmentCommandHandler _createOrUpdateWorkshopAppointmentCommandHandler;
        private readonly GetWorkshopAppointmentByClaimQueryHandler _getWorkshopAppointmentByClaimQueryHandler;
        private readonly WorkshopRepairEstimateService _workshopRepairEstimateService;

        public WorkshopController(
            GetPanelWorkshopStatesQueryHandler getPanelWorkshopStatesQueryHandler,
            GetPanelWorkshopsByStateQueryHandler getPanelWorkshopsByStateQueryHandler,
            GetAllWorkshopsQueryHandler getAllWorkshopsQueryHandler,
            GetApprovedClaimsForPanelWorkshopQueryHandler getApprovedClaimsForPanelWorkshopQueryHandler,
            CreateWorkshopCommandHandler createWorkshopCommandHandler,
            UpdateWorkshopCommandHandler updateWorkshopCommandHandler,
            DeleteWorkshopCommandHandler deleteWorkshopCommandHandler,
            CreateOrUpdateWorkshopAppointmentCommandHandler createOrUpdateWorkshopAppointmentCommandHandler,
            GetWorkshopAppointmentByClaimQueryHandler getWorkshopAppointmentByClaimQueryHandler,
            WorkshopRepairEstimateService workshopRepairEstimateService)
        {
            _getPanelWorkshopStatesQueryHandler = getPanelWorkshopStatesQueryHandler;
            _getPanelWorkshopsByStateQueryHandler = getPanelWorkshopsByStateQueryHandler;
            _getAllWorkshopsQueryHandler = getAllWorkshopsQueryHandler;
            _getApprovedClaimsForPanelWorkshopQueryHandler = getApprovedClaimsForPanelWorkshopQueryHandler;
            _createWorkshopCommandHandler = createWorkshopCommandHandler;
            _updateWorkshopCommandHandler = updateWorkshopCommandHandler;
            _deleteWorkshopCommandHandler = deleteWorkshopCommandHandler;
            _createOrUpdateWorkshopAppointmentCommandHandler = createOrUpdateWorkshopAppointmentCommandHandler;
            _getWorkshopAppointmentByClaimQueryHandler = getWorkshopAppointmentByClaimQueryHandler;
            _workshopRepairEstimateService = workshopRepairEstimateService;
        }

        [HttpGet("states")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetStates()
        {
            var result = await _getPanelWorkshopStatesQueryHandler.Handle(new GetPanelWorkshopStatesQuery());
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetByState([FromQuery] string state)
        {
            try
            {
                var result = await _getPanelWorkshopsByStateQueryHandler.Handle(new GetPanelWorkshopsByStateQuery
                {
                    State = state
                });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _getAllWorkshopsQueryHandler.Handle(new GetAllWorkshopsQuery());
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize(Policy = "PanelWorkshopOnly")]
        public async Task<IActionResult> GetMyWorkshop([FromServices] WorkshopService workshopService)
        {
            try
            {
                var workshopId = GetCurrentWorkshopId();
                var result = await workshopService.GetMyWorkshopAsync(workshopId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("me")]
        [Authorize(Policy = "PanelWorkshopOnly")]
        public async Task<IActionResult> UpdateMyWorkshop(
            [FromServices] WorkshopService workshopService,
            [FromBody] UpdateMyWorkshopRequest request)
        {
            try
            {
                var workshopId = GetCurrentWorkshopId();
                var result = await workshopService.UpdateMyWorkshopAsync(workshopId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateWorkshopRequest request)
        {
            try
            {
                var result = await _createWorkshopCommandHandler.Handle(new CreateWorkshopCommand
                {
                    Name = request.Name,
                    State = request.State,
                    Address = request.Address,
                    Phone = request.Phone,
                    Fax = request.Fax,
                    Email = request.Email,
                    BankName = request.BankName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankAccountHolderName = request.BankAccountHolderName,
                    IsPanelWorkshop = request.IsPanelWorkshop,
                    IsActive = request.IsActive
                });

                return Ok(MapWorkshop(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("bulk")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> CreateBulk([FromBody] List<CreateWorkshopRequest> requests)
        {
            try
            {
                if (requests.Count == 0)
                {
                    return BadRequest("At least one workshop is required.");
                }

                var results = new List<WorkshopResponse>();

                foreach (var request in requests)
                {
                    var result = await _createWorkshopCommandHandler.Handle(new CreateWorkshopCommand
                    {
                        Name = request.Name,
                        State = request.State,
                        Address = request.Address,
                        Phone = request.Phone,
                        Fax = request.Fax,
                        Email = request.Email,
                        BankName = request.BankName,
                        BankAccountNumber = request.BankAccountNumber,
                        BankAccountHolderName = request.BankAccountHolderName,
                        IsPanelWorkshop = request.IsPanelWorkshop,
                        IsActive = request.IsActive
                    });

                    results.Add(MapWorkshop(result));
                }

                return Ok(results);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkshopRequest request)
        {
            try
            {
                var result = await _updateWorkshopCommandHandler.Handle(new UpdateWorkshopCommand
                {
                    WorkshopId = id,
                    Name = request.Name,
                    State = request.State,
                    Address = request.Address,
                    Phone = request.Phone,
                    Fax = request.Fax,
                    Email = request.Email,
                    BankName = request.BankName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankAccountHolderName = request.BankAccountHolderName,
                    IsPanelWorkshop = request.IsPanelWorkshop,
                    IsActive = request.IsActive
                });

                return Ok(MapWorkshop(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _deleteWorkshopCommandHandler.Handle(new DeleteWorkshopCommand
                {
                    WorkshopId = id
                });

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("appointments")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> CreateOrUpdateAppointment([FromBody] CreateWorkshopAppointmentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _createOrUpdateWorkshopAppointmentCommandHandler.Handle(new CreateOrUpdateWorkshopAppointmentCommand
                {
                    UserId = userId,
                    ClaimId = request.ClaimId,
                    WorkshopId = request.WorkshopId,
                    PreferredDate = request.PreferredDate,
                    TimeSlotStart = request.TimeSlotStart,
                    TimeSlotEnd = request.TimeSlotEnd,
                    Notes = request.Notes
                });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("appointments/claim/{claimId:guid}")]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> GetAppointment(Guid claimId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _getWorkshopAppointmentByClaimQueryHandler.Handle(new GetWorkshopAppointmentByClaimQuery
                {
                    UserId = userId,
                    ClaimId = claimId,
                    EnforceOwnership = true
                });
                return result == null ? NotFound() : Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("panel-workshop/approved-claims")]
        [Authorize(Policy = "PanelWorkshopOnly")]
        public async Task<IActionResult> GetApprovedClaimsForPanelWorkshop()
        {
            var workshopIdClaim = User.FindFirst("WorkshopId")?.Value;

            if (!Guid.TryParse(workshopIdClaim, out var workshopId))
            {
                return BadRequest("Panel workshop account is not linked to a workshop.");
            }

            var result = await _getApprovedClaimsForPanelWorkshopQueryHandler.Handle(new GetApprovedClaimsForPanelWorkshopQuery
            {
                WorkshopId = workshopId
            });

            return Ok(result);
        }

        [HttpPost("panel-workshop/repair-estimates")]
        [Authorize(Policy = "PanelWorkshopOnly")]
        public async Task<IActionResult> SubmitRepairEstimate([FromBody] SubmitWorkshopRepairEstimateRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var workshopId = GetCurrentWorkshopId();
                var result = await _workshopRepairEstimateService.SubmitAsync(userId, workshopId, request);
                return Ok(WorkshopRepairEstimateService.MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("panel-workshop/repair-estimates")]
        [Authorize(Policy = "PanelWorkshopOnly")]
        public async Task<IActionResult> GetMyRepairEstimates()
        {
            var workshopId = GetCurrentWorkshopId();
            var result = await _workshopRepairEstimateService.GetByWorkshopIdAsync(workshopId);
            return Ok(result.Select(WorkshopRepairEstimateService.MapResponse).ToList());
        }

        [HttpGet("repair-estimates/all")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> GetAllRepairEstimates()
        {
            var result = await _workshopRepairEstimateService.GetAllAsync();
            return Ok(result.Select(WorkshopRepairEstimateService.MapResponse).ToList());
        }

        [HttpPost("repair-estimates/{estimateId:guid}/approve")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> ApproveRepairEstimate(Guid estimateId, [FromBody] WorkshopRepairEstimateDecisionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _workshopRepairEstimateService.ApproveAsync(estimateId, userId, request.ReviewNote);
                return Ok(WorkshopRepairEstimateService.MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("repair-estimates/{estimateId:guid}/reject")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> RejectRepairEstimate(Guid estimateId, [FromBody] WorkshopRepairEstimateDecisionRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _workshopRepairEstimateService.RejectAsync(estimateId, userId, request.ReviewNote);
                return Ok(WorkshopRepairEstimateService.MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("repair-estimates/{estimateId:guid}/request-changes")]
        [Authorize(Policy = "OfficerOrAdmin")]
        public async Task<IActionResult> RequestRepairEstimateChanges(Guid estimateId, [FromBody] WorkshopRepairEstimateRequestChangesRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _workshopRepairEstimateService.RequestChangesAsync(
                    estimateId,
                    userId,
                    request.RequestedItems,
                    request.ReviewNote);
                return Ok(WorkshopRepairEstimateService.MapResponse(result));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static WorkshopResponse MapWorkshop(Motor.Claim.Domain.Entities.WorkshopEntity workshop)
        {
            return new WorkshopResponse
            {
                WorkshopId = workshop.WorkshopId,
                Name = workshop.Name,
                State = workshop.State,
                Address = workshop.Address,
                Phone = DeserializeOptionalList(workshop.Phone),
                Fax = workshop.Fax,
                Email = DeserializeOptionalList(workshop.Email),
                BankName = workshop.BankName,
                BankAccountNumber = workshop.BankAccountNumber,
                BankAccountHolderName = workshop.BankAccountHolderName,
                IsPanelWorkshop = workshop.IsPanelWorkshop,
                IsActive = workshop.IsActive
            };
        }

        private static List<string> DeserializeOptionalList(string? payload)
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

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing UserId claim.");
            }

            return userId;
        }

        private Guid GetCurrentWorkshopId()
        {
            var workshopIdClaim = User.FindFirst("WorkshopId")?.Value;

            if (!Guid.TryParse(workshopIdClaim, out var workshopId))
            {
                throw new UnauthorizedAccessException("Panel workshop account is not linked to a workshop.");
            }

            return workshopId;
        }
    }
}
