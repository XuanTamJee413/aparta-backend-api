using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApartaAPI.DTOs.Projects;
using ApartaAPI.Services.Interfaces;

namespace ApartaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all endpoints
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectsController(IProjectService service)
        {
            _service = service;
        }

        // GET: api/Projects
        [HttpGet]
        [Authorize(Policy = "StaffOrAdmin")] // Only staff and admin can view all projects
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var projects = await _service.GetAllAsync();
            return Ok(projects);
        }

        // GET: api/Projects/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "ResidentOrAbove")] // All authenticated users can view specific project
        public async Task<ActionResult<ProjectDto>> GetProject(string id)
        {
            var project = await _service.GetByIdAsync(id);
            if (project == null) return NotFound();
            return Ok(project);
        }

        // POST: api/Projects
        [HttpPost]
        [Authorize(Policy = "AdminOnly")] // Only admin can create projects
        public async Task<ActionResult<ProjectDto>> PostProject([FromBody] ProjectCreateDto request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetProject), new { id = created.ProjectId }, created);
        }

        // PUT: api/Projects/{id}
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")] // Only admin can update projects
        public async Task<IActionResult> PutProject(string id, [FromBody] ProjectUpdateDto request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (!updated) return NotFound();
            return NoContent();
        }

        // DELETE: api/Projects/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")] // Only admin can delete projects
        public async Task<IActionResult> DeleteProject(string id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
