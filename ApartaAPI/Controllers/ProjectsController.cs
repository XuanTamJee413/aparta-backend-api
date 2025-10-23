using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectsController(IProjectService service)
        {
            _service = service;
        }

        // GET: api/Projects
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectDto>>>> GetProjects(
            [FromQuery] ProjectQueryParameters query)
        {
            var response = await _service.GetAllAsync(query);
            // Luôn trả về 200 OK. FE sẽ đọc cờ "Succeeded"
            return Ok(response);
        }

        // GET: api/Projects/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProject(string id)
        {
            var response = await _service.GetByIdAsync(id);

            if (!response.Succeeded)
            {
                return NotFound(response); // 404
            }

            return Ok(response); // 200
        }

        // POST: api/Projects
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> PostProject([FromBody] ProjectCreateDto request)
        {
            var response = await _service.CreateAsync(request);

            if (!response.Succeeded)
            {
                return BadRequest(response); // 400
            }

            // 201 Created
            return CreatedAtAction(
                nameof(GetProject),
                new { id = response.Data!.ProjectId },
                response
            );
        }

        // PUT: api/Projects/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> PutProject(string id, [FromBody] ProjectUpdateDto request)
        {
            var response = await _service.UpdateAsync(id, request);

            if (!response.Succeeded)
            {
                if (response.Message == "SM01")
                {
                    return NotFound(response); // 404
                }

                return BadRequest(response); // 400
            }

            return Ok(response); // 200 (thay vì 204 NoContent, để trả về SM03)
        }
    }
}