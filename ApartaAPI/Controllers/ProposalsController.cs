using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Proposals;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProposalsController : ControllerBase
    {
        private readonly IProposalService _proposalService;

        public ProposalsController(IProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        private string GetUserId() => User.FindFirst("id")?.Value ?? throw new UnauthorizedAccessException();
        private bool IsStaffOrAdmin() => User.FindFirst(ClaimTypes.Role)?.Value?.Contains("staff") == true || User.FindFirst(ClaimTypes.Role)?.Value == "admin"; // Giả định Role claim

        // ============================
        // RESIDENT: TẠO PROPOSAL
        // POST: api/Proposals/create
        // ============================
        [HttpPost("create")]
        [ProducesResponseType(typeof(ApiResponse<ProposalDto>), StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<ProposalDto>>> CreateProposal([FromBody] ProposalCreateDto createDto)
        {
            try
            {
                var residentId = GetUserId();
                var createdProposal = await _proposalService.CreateProposalAsync(residentId, createDto);

                return CreatedAtAction(nameof(GetProposalById),
                                       new { id = createdProposal.ProposalId },
                                       ApiResponse<ProposalDto>.Success(createdProposal));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        // ============================
        // RESIDENT: LẤY LỊCH SỬ
        // GET: api/Proposals/my-history
        // ============================
        [HttpGet("my-history")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProposalDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProposalDto>>>> GetMyProposals()
        {
            var residentId = GetUserId();
            var proposals = await _proposalService.GetProposalsByResidentAsync(residentId);
            return Ok(ApiResponse<IEnumerable<ProposalDto>>.Success(proposals));
        }

        // ============================
        // STAFF: LẤY DANH SÁCH CHỜ XỬ LÝ
        // GET: api/Proposals/staff-list
        // ============================
        [HttpGet("staff-list")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProposalDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<PagedList<ProposalDto>>>> GetStaffProposals([FromQuery] ProposalQueryParams query)
        {
            var staffId = GetUserId();
            var response = await _proposalService.GetProposalsForStaffAsync(staffId, query);
            return Ok(response);
        }

        // ============================
        // STAFF/RESIDENT: XEM CHI TIẾT
        // GET: api/Proposals/{id}
        // ============================
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProposalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProposalDto>>> GetProposalById(string id)
        {
            try
            {
                var userId = GetUserId();
                var proposal = await _proposalService.GetProposalDetailAsync(id, userId);

                if (proposal == null) return NotFound(ApiResponse.Fail("Proposal không tồn tại."));

                return Ok(ApiResponse<ProposalDto>.Success(proposal));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(); // Trả về 403 Forbidden
            }
        }

        // ============================
        // STAFF: TRẢ LỜI PROPOSAL
        // PUT: api/Proposals/reply/{id}
        // ============================
        [HttpPut("reply/{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProposalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProposalDto>>> ReplyProposal(string id, [FromBody] ProposalReplyDto replyDto)
        {
            try
            {
                var staffId = GetUserId();
                var updatedProposal = await _proposalService.ReplyProposalAsync(id, staffId, replyDto);

                if (updatedProposal == null) return NotFound(ApiResponse.Fail("Proposal không tồn tại."));

                return Ok(ApiResponse<ProposalDto>.Success(updatedProposal, "Trả lời đề xuất thành công."));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }
}
