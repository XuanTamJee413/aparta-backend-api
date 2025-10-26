using ApartaAPI.DTOs.Visitors;
using ApartaAPI.Models;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitorsController : ControllerBase 
    {
        private readonly IVisitorService _service; 

        public VisitorsController(IVisitorService service) 
        {
            _service = service;
        }
        [HttpPost("fast-checkin")]
        public async Task<ActionResult<VisitorDto>> CreateVisit([FromBody] VisitorCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdVisitor = await _service.CreateVisitAsync(dto);
                return Ok(createdVisitor);
            }
            catch (ValidationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi máy chủ nội bộ: {ex.Message}");
            }
        }

    }
}