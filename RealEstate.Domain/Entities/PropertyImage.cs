using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace RealEstate.Domain.Entities
{
    public class PropertyImage
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdProperty { get; set; } = default!;
        public string File { get; set; } = default!;
        public bool Enabled { get; set; } = true;
    }
}
