using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;

namespace RealEstate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _service;
        private readonly IValidator<PropertyFilterDto> _validator;
        
        public PropertiesController(IPropertyService service, IValidator<PropertyFilterDto> validator)
        {
            _service = service;
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PropertyDto>>> Get(
        [FromQuery] string? name,
        [FromQuery] string? address,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortDir = "desc",
        CancellationToken ct = default)
        {
            var filter = new PropertyFilterDto(
                Name: name,
                Address: address,
                MinPrice: minPrice,
                MaxPrice: maxPrice,
                Page: page,
                PageSize: pageSize,
                SortBy: sortBy,
                SortDir: sortDir
            );

            var vr = await _validator.ValidateAsync(filter, ct);
            if (!vr.IsValid)
            {
                var msg = string.Join(" | ", vr.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                throw new ArgumentException(msg);
            }

            var result = await _service.GetPropertiesAsync(filter, ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetById([FromRoute] string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id is required.");
            }

            if (!ObjectId.TryParse(id, out _))
                throw new ArgumentException("Invalid id format. Expect a 24-hex string.");

            var dto = await _service.GetByIdAsync(id, ct);
            if (dto is null) return NotFound(); 

            return Ok(dto);
        }
    }
}
