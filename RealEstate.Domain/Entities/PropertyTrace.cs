using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace RealEstate.Domain.Entities
{
    public class PropertyTrace
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdProperty { get; set; } = default!;
        public DateTime DateSale { get; set; }
        public string Name { get; set; } = default!;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Value { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Tax { get; set; }
    }
}
