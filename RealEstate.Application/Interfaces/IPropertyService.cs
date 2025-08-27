using RealEstate.Contracts.Dtos;

namespace RealEstate.Application.Interfaces
{
    public interface IPropertyService
    {
        Task<PagedResult<PropertyDto>> GetPropertiesAsync(PropertyFilterDto filter, CancellationToken ct);
        Task<PropertyDto?> GetByIdAsync(string id, CancellationToken ct);
    }
}
