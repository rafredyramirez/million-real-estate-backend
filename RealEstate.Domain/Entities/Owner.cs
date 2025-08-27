namespace RealEstate.Domain.Entities
{
    public class Owner
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? Photo { get; set; }
        public DateTime? Birthday { get; set; }
    }
}
