using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;
using RealEstate.Domain.Entities;

namespace RealEstate.Application.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IPropertyRepository _repo;
        public PropertyService(IPropertyRepository repo) => _repo = repo;

        //public async Task<PagedResult<PropertyDto>> GetPropertiesAsync(PropertyFilterDto filter, CancellationToken ct)
        //{
        //    var page = Math.Max(1, filter.Page);
        //    var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        //    if (filter.MinPrice is > 0 && filter.MaxPrice is > 0 && filter.MinPrice > filter.MaxPrice)
        //        throw new ArgumentException("MinPrice no puede ser mayor que MaxPrice.");

        //    var (items, total) = await _repo.GetPagedAsync(filter with { Page = page, PageSize = pageSize }, ct);
        //    var dtos = items.Select(p => new PropertyDto(p.Id, p.IdOwner, p.Name, p.Address, p.Price, p.ImageUrl ?? string.Empty));
        //    var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        //    return new PagedResult<PropertyDto>(dtos, page, pageSize, total, totalPages);
        //}
        public async Task<PagedResult<PropertyDto>> GetPropertiesAsync(PropertyFilterDto filter, CancellationToken ct)
        {
            filter ??= new PropertyFilterDto(null, null, null, null, 1, 10, "CreatedAt", "desc");

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            if (filter.MinPrice is not null && filter.MaxPrice is not null && filter.MinPrice > filter.MaxPrice)
                throw new ArgumentException("MinPrice no puede ser mayor que MaxPrice.");

            var normalized = new PropertyFilterDto(filter.Name, filter.Address, filter.MinPrice, filter.MaxPrice, page, pageSize, filter.SortBy, filter.SortDir);

            var (items, total) = await _repo.GetPagedAsync(normalized, ct);

            var dtos = items.Select(p =>
                new PropertyDto(p.Id, p.IdOwner, p.Name, p.Address, p.Price, p.ImageUrl ?? string.Empty));

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            return new PagedResult<PropertyDto>(dtos, page, pageSize, total, totalPages);
        }

        public async Task<PropertyDto?> GetByIdAsync(string id, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            return p is null ? null : new PropertyDto(p.Id, p.IdOwner, p.Name, p.Address, p.Price, p.ImageUrl ?? string.Empty);
        }
    }
}
