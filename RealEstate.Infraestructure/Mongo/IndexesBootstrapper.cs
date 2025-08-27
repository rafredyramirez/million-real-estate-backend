using MongoDB.Driver;
using RealEstate.Domain.Entities;

namespace RealEstate.Infraestructure.Mongo
{
    public static class IndexesBootstrapper
    {
        public static async Task EnsureIndexesAsync(MongoContext ctx, CancellationToken ct)
        {
            // Properties
            await ctx.Properties.Indexes.CreateManyAsync(new[]
            {
            new CreateIndexModel<Property>(
                Builders<Property>.IndexKeys.Text(p => p.Name).Text(p => p.Address)),
            new CreateIndexModel<Property>(
                Builders<Property>.IndexKeys.Ascending(p => p.CodeInternal),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<Property>(
                Builders<Property>.IndexKeys.Ascending(p => p.Price)),
            new CreateIndexModel<Property>(
                Builders<Property>.IndexKeys.Descending(p => p.CreatedAt))
        }, ct);

            // PropertyImages
            await ctx.PropertyImages.Indexes.CreateOneAsync(
                new CreateIndexModel<PropertyImage>(
                    Builders<PropertyImage>.IndexKeys
                        .Ascending(i => i.IdProperty)
                        .Ascending(i => i.Enabled)),
                cancellationToken: ct
            );

            // PropertyTraces
            await ctx.PropertyTraces.Indexes.CreateManyAsync(new[]
            {
            new CreateIndexModel<PropertyTrace>(
                Builders<PropertyTrace>.IndexKeys
                    .Ascending(t => t.IdProperty)
                    .Descending(t => t.DateSale))
        }, ct);
        }
    }
}
