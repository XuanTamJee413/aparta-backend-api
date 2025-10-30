﻿using ApartaAPI.DTOs.Common; // Thêm
using ApartaAPI.DTOs.Services;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ApartaAPI.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ServiceController : ControllerBase
	{
		private readonly IServiceService _serviceService;

		public ServiceController(IServiceService serviceService)
		{
			_serviceService = serviceService;
		}

		// GET: api/Service
		// SỬA: Cập nhật endpoint để nhận tham số Query
		[HttpGet]
		[ProducesResponseType(typeof(PagedList<ServiceDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<PagedList<ServiceDto>>> GetServices(
			[FromQuery] ServiceQueryParameters parameters)
		{
			var services = await _serviceService.GetServicesAsync(parameters);

			// Bạn có thể thêm thông tin phân trang vào Response Header nếu muốn
			// Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(services.PaginationMetadata));

			return Ok(services);
		}

		// ... các endpoint GET(id), POST, PUT, DELETE giữ nguyên ...

		// GET: api/Service/{id}
		[HttpGet("{id}")]
		[ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ServiceDto>> GetService(string id)
		{
			var serviceDto = await _serviceService.GetServiceByIdAsync(id);

			if (serviceDto == null)
			{
				return NotFound();
			}

			return Ok(serviceDto);
		}

		// POST: api/Service
		[HttpPost]
		[ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ServiceDto>> PostService([FromBody] ServiceCreateDto serviceCreateDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			var createdServiceDto = await _serviceService.AddServiceAsync(serviceCreateDto);

			return CreatedAtAction(
				nameof(GetService),
				new { id = createdServiceDto.ServiceId },
				createdServiceDto
			);
		}

		// PUT: api/Service/{id}
		[HttpPut("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> PutService(string id, [FromBody] ServiceUpdateDto serviceUpdateDto)
		{

			var updatedServiceDto = await _serviceService.UpdateServiceAsync(id, serviceUpdateDto);

			if (updatedServiceDto == null)
			{
				return NotFound();
			}

			return NoContent();
		}

		// DELETE: api/Service/{id}
		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DeleteService(string id)
		{
			var result = await _serviceService.DeleteServiceAsync(id);

			if (!result)
			{
				return NotFound();
			}

			return NoContent();
		}
	}
}