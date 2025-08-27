using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;

namespace RealEstate.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _service;
        public PropertiesController(IPropertyService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<PagedResult<PropertyDto>>> Get(
        [FromQuery] string? name, [FromQuery] string? address,
        [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "CreatedAt", [FromQuery] string? sortDir = "desc",
        CancellationToken ct = default)
        {
            var filter = new PropertyFilterDto(name, address, minPrice, maxPrice, page, pageSize, sortBy, sortDir);
            return Ok(await _service.GetPropertiesAsync(filter, ct));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetById(string id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }
    }
}
