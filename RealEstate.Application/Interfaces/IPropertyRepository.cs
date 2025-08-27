using RealEstate.Contracts.Dtos;
using RealEstate.Domain.Entities;

namespace RealEstate.Application.Interfaces
{
    public interface IPropertyRepository
    {
        Task<(IReadOnlyList<Property> Items, long Total)> GetPagedAsync(PropertyFilterDto filter, CancellationToken ct);
        Task<Property?> GetByIdAsync(string id, CancellationToken ct);
    }
}
    