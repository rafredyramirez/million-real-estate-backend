namespace RealEstate.Contracts.Dtos
{
    public record PagedResult<T>(IEnumerable<T> Items, int Page, int PageSize, long Total, int TotalPages);
}
