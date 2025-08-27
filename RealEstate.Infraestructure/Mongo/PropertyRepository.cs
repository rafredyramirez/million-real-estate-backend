using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.Application.Interfaces;
using RealEstate.Contracts.Dtos;
using RealEstate.Domain.Entities;

namespace RealEstate.Infraestructure.Mongo
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly MongoContext _ctx;
        public PropertyRepository(MongoContext ctx) => _ctx = ctx;

        public async Task<(IReadOnlyList<Property> Items, long Total)> GetPagedAsync(PropertyFilterDto f, CancellationToken ct)
        {
            var fb = Builders<Property>.Filter;
            var filters = new List<FilterDefinition<Property>>();

            // --- UN solo $text con ambos términos ---
            if (!string.IsNullOrWhiteSpace(f.Name) || !string.IsNullOrWhiteSpace(f.Address))
            {
                var terms = new List<string>();
                if (!string.IsNullOrWhiteSpace(f.Name)) terms.Add($"\"{f.Name}\"");
                if (!string.IsNullOrWhiteSpace(f.Address)) terms.Add($"\"{f.Address}\"");

                // Combina términos: busca como frases (entre comillas) en el índice de texto (Name+Address)
                var textSearch = string.Join(" ", terms);
                filters.Add(fb.Text(textSearch));
            }

            // Rango de precios
            if (f.MinPrice is not null) filters.Add(fb.Gte(p => p.Price, f.MinPrice.Value));
            if (f.MaxPrice is not null) filters.Add(fb.Lte(p => p.Price, f.MaxPrice.Value));

            var finalFilter = filters.Count > 0 ? fb.And(filters) : FilterDefinition<Property>.Empty;

            var total = await _ctx.Properties.CountDocumentsAsync(finalFilter, cancellationToken: ct);

            var sort = (f.SortBy?.ToLowerInvariant(), f.SortDir?.ToLowerInvariant()) switch
            {
                ("price", "asc") => Builders<Property>.Sort.Ascending(p => p.Price),
                ("price", "desc") => Builders<Property>.Sort.Descending(p => p.Price),
                ("name", "asc") => Builders<Property>.Sort.Ascending(p => p.Name),
                ("name", "desc") => Builders<Property>.Sort.Descending(p => p.Name),
                ("createdat", "asc") => Builders<Property>.Sort.Ascending(p => p.CreatedAt),
                _ => Builders<Property>.Sort.Descending(p => p.CreatedAt)
            };

            var skip = (Math.Max(1, f.Page) - 1) * Math.Clamp(f.PageSize, 1, 100);
            var limit = Math.Clamp(f.PageSize, 1, 100);

            var docs = await _ctx.Properties.Aggregate()
                .Match(finalFilter).Sort(sort).Skip(skip).Limit(limit)
                .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument {
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
                //.Project(new BsonDocument {
                //{ "Id", "$_id" }, { "IdOwner", 1 }, { "Name", 1 }, { "Address", 1 }, { "Price", 1 },
                //{ "CodeInternal", 1 }, { "Year", 1 }, { "CreatedAt", 1 }, { "UpdatedAt", 1 },
                //{ "ImageUrl", new BsonDocument("$ifNull", new BsonArray {
                //    new BsonDocument("$arrayElemAt", new BsonArray { "$images.File", 0 }), BsonNull.Value
                //})}
                //})
                .Project(new BsonDocument
                {
                    { "Id", new BsonDocument("$toString", "$_id") },
                    { "IdOwner", new BsonDocument("$toString", "$IdOwner") },
                    { "Name", 1 },
                    { "Address", 1 },
                    { "Price", 1 },
                    { "CodeInternal", 1 },
                    { "Year", 1 },
                    { "CreatedAt", 1 },
                    { "UpdatedAt", 1 },
                    { "ImageUrl", new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$images.File", 0 }),
                            BsonNull.Value
                        })
                    }
                })
                .ToListAsync(ct);

            var items = docs.Select(d => new Property
            {
                //Id = d["Id"].AsObjectId.ToString(),
                //IdOwner = d["IdOwner"].AsString,
                //Name = d["Name"].AsString,
                //Address = d["Address"].AsString,
                //Price = d["Price"].ToDecimal(),
                //CodeInternal = d.GetValue("CodeInternal", "").AsString,
                //Year = d.GetValue("Year", 0).ToInt32(),
                //CreatedAt = d["CreatedAt"].ToUniversalTime(),
                //UpdatedAt = d["UpdatedAt"].ToUniversalTime(),
                //ImageUrl = d.GetValue("ImageUrl", BsonNull.Value).IsBsonNull ? null : d["ImageUrl"].AsString
                Id = d["Id"].AsString,
                IdOwner = d["IdOwner"].AsString,
                Name = d["Name"].AsString,
                Address = d["Address"].AsString,
                Price = d["Price"].ToDecimal(),
                CodeInternal = d.GetValue("CodeInternal", BsonNull.Value).IsBsonNull ? "" : d["CodeInternal"].AsString,
                Year = d.GetValue("Year", 0).ToInt32(),
                CreatedAt = d["CreatedAt"].ToUniversalTime(),
                UpdatedAt = d["UpdatedAt"].ToUniversalTime(),
                ImageUrl = d.GetValue("ImageUrl", BsonNull.Value).IsBsonNull ? null : d["ImageUrl"].AsString
            }).ToList();

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
                //.Project(new BsonDocument {
                //{ "Id", "$_id" }, { "IdOwner", 1 }, { "Name", 1 }, { "Address", 1 }, { "Price", 1 },
                //{ "CodeInternal", 1 }, { "Year", 1 }, { "CreatedAt", 1 }, { "UpdatedAt", 1 },
                //{ "ImageUrl", new BsonDocument("$ifNull", new BsonArray {
                //    new BsonDocument("$arrayElemAt", new BsonArray { "$images.File", 0 }), BsonNull.Value
                //})}
                //})
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
                //Id = doc["Id"].AsObjectId.ToString(),
                //IdOwner = doc["IdOwner"].AsString,
                //Name = doc["Name"].AsString,
                //Address = doc["Address"].AsString,
                //Price = doc["Price"].ToDecimal(),
                //CodeInternal = doc.GetValue("CodeInternal", "").AsString,
                //Year = doc.GetValue("Year", 0).ToInt32(),
                //CreatedAt = doc["CreatedAt"].ToUniversalTime(),
                //UpdatedAt = doc["UpdatedAt"].ToUniversalTime(),
                //ImageUrl = doc.GetValue("ImageUrl", BsonNull.Value).IsBsonNull ? null : doc["ImageUrl"].AsString
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
