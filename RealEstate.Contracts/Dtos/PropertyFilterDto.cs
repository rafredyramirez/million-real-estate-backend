namespace RealEstate.Contracts.Dtos
{
    public record PropertyFilterDto(string? Name, string? Address, decimal? MinPrice, decimal? MaxPrice,
                                int Page = 1, int PageSize = 10, string? SortBy = "CreatedAt", string? SortDir = "desc");
}
