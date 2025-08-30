using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RealEstate.Domain.Entities
{
    public class Property
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdOwner { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
        public string CodeInternal { get; set; } = default!;
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonIgnoreIfDefault] public string? ImageUrl { get; set; }
    }
}
