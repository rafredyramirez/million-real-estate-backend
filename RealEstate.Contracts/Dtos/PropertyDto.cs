namespace RealEstate.Contracts.Dtos
{
    public record PropertyDto(string Id, string IdOwner, string Name, string Address, decimal Price, string ImageUrl);
}
