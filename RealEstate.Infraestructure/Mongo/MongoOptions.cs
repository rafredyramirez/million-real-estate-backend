namespace RealEstate.Infraestructure.Mongo
{
    public class MongoOptions
    {
        public string ConnectionString { get; set; } = default!;
        public string Database { get; set; } = default!;
        public CollectionsNames Collections { get; set; } = new();
        public class CollectionsNames
        {
            public string Properties { get; set; } = "Properties";
            public string Owners { get; set; } = "Owners";
            public string PropertyImages { get; set; } = "PropertyImages";
            public string PropertyTraces { get; set; } = "PropertyTraces";
        }
    }
}
