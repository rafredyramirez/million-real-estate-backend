using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RealEstate.Domain.Entities;

namespace RealEstate.Infraestructure.Mongo
{
    public class MongoContext
    {
        public IMongoDatabase Database { get; }
        public IMongoCollection<Property> Properties { get; }
        public IMongoCollection<Owner> Owners { get; }
        public IMongoCollection<PropertyImage> PropertyImages { get; }
        public IMongoCollection<PropertyTrace> PropertyTraces { get; }

        public MongoContext(IOptions<MongoOptions> opt)
        {
            var client = new MongoClient(opt.Value.ConnectionString);
            Database = client.GetDatabase(opt.Value.Database);
            Properties = Database.GetCollection<Property>(opt.Value.Collections.Properties);
            Owners = Database.GetCollection<Owner>(opt.Value.Collections.Owners);
            PropertyImages = Database.GetCollection<PropertyImage>(opt.Value.Collections.PropertyImages);
            PropertyTraces = Database.GetCollection<PropertyTrace>(opt.Value.Collections.PropertyTraces);
        }
    }
}
