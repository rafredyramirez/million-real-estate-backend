using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;
using RealEstate.Domain.Entities;
using System.Text.RegularExpressions;

namespace RealEstate.Infraestructure.Mongo
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly MongoContext _ctx;
        public PropertyRepository(MongoContext ctx) => _ctx = ctx;

        private static SortDefinition<Property> BuildSort(string? sortBy, string? sortDir)
        {
            // dir: asc/desc (default: desc)
            var isAsc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

            // campo: CreatedAt | Price | Name (default: CreatedAt)
            var by = (sortBy ?? "CreatedAt").Trim().ToLowerInvariant();

            var s = Builders<Property>.Sort;

            return by switch
            {
                "price" => isAsc ? s.Ascending(p => p.Price) : s.Descending(p => p.Price),
                "name" => isAsc ? s.Ascending(p => p.Name) : s.Descending(p => p.Name),
                _ => isAsc ? s.Ascending(p => p.CreatedAt) : s.Descending(p => p.CreatedAt),
            };
        }
        public async Task<(IReadOnlyList<Property> Items, long Total)> GetPagedAsync(PropertyFilterDto f, CancellationToken ct)
        {
            var filter = Builders<Property>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(f.Name))
            {
                var pattern = Regex.Escape(f.Name.Trim()); 
                filter &= Builders<Property>.Filter.Regex(
                    x => x.Name,
                    new BsonRegularExpression(pattern, "i"));
            }

            if (!string.IsNullOrWhiteSpace(f.Address))
            {
                var pattern = Regex.Escape(f.Address.Trim());
                filter &= Builders<Property>.Filter.Regex(
                    x => x.Address,
                    new BsonRegularExpression(pattern, "i"));
            }

            if (f.MinPrice.HasValue)
                filter &= Builders<Property>.Filter.Gte(x => x.Price, f.MinPrice.Value);

            if (f.MaxPrice.HasValue)
                filter &= Builders<Property>.Filter.Lte(x => x.Price, f.MaxPrice.Value);

            // Orden
            var sort = BuildSort(f.SortBy, f.SortDir);

            var total = await _ctx.Properties.CountDocumentsAsync(filter, cancellationToken: ct);

            var items = await _ctx.Properties
                .Find(filter)
                .Sort(sort)
                .Skip((f.Page - 1) * f.PageSize)
                .Limit(f.PageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<Property?> GetByIdAsync(string id, CancellationToken ct)
        {
            var objId = ObjectId.Parse(id);
            var doc = await _ctx.Properties.Aggregate()
                .Match(Builders<Property>.Filter.Eq("_id", objId))
                .AppendStage<BsonDocument>(/* mismo $lookup */ new BsonDocument("$lookup", new BsonDocument {
                { "from", _ctx.PropertyImages.CollectionNamespace.CollectionName },
                { "let", new BsonDocument("pid", "$_id") },
                { "pipeline", new BsonArray {
                    new BsonDocument("$match", new BsonDocument("$expr",
                        new BsonDocument("$and", new BsonArray {
                            new BsonDocument("$eq", new BsonArray { "$IdProperty", "$$pid" }),
                            new BsonDocument("$eq", new BsonArray { "$Enabled", true })
                        }))),
                    new BsonDocument("$limit", 1)
                }},
                { "as", "images" }
                }))
                .Project(new BsonDocument
                {
                    { "Id", new BsonDocument("$toString", "$_id") },
                    { "IdOwner", new BsonDocument("$toString", "$IdOwner") },
                    { "Name", 1 }, { "Address", 1 }, { "Price", 1 },
                    { "CodeInternal", 1 }, { "Year", 1 }, { "CreatedAt", 1 }, { "UpdatedAt", 1 },
                    { "ImageUrl", new BsonDocument("$ifNull", new BsonArray
                        { new BsonDocument("$arrayElemAt", new BsonArray { "$images.File", 0 }), BsonNull.Value })
                    }
                })
                .FirstOrDefaultAsync(ct);

            if (doc is null) return null;
            return new Property
            {
                Id = doc["Id"].AsString,
                IdOwner = doc["IdOwner"].AsString,
                Name = doc["Name"].AsString,
                Address = doc["Address"].AsString,
                Price = doc["Price"].ToDecimal(),
                CodeInternal = doc.GetValue("CodeInternal", BsonNull.Value).IsBsonNull ? "" : doc["CodeInternal"].AsString,
                Year = doc.GetValue("Year", 0).ToInt32(),
                CreatedAt = doc["CreatedAt"].ToUniversalTime(),
                UpdatedAt = doc["UpdatedAt"].ToUniversalTime(),
                ImageUrl = doc.GetValue("ImageUrl", BsonNull.Value).IsBsonNull ? null : doc["ImageUrl"].AsString
            };
        }
    }
}
