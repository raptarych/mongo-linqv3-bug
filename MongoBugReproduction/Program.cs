using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixture;
using MongoBugReproduction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;


#region Basic mongo configuration
ConventionRegistry.Register("Example", GetConventions(), x => true);
IConventionPack GetConventions()
{
    return new ConventionPack
    {
        new DelegateMemberMapConvention("BsonIgnoreIfNull", x => x.SetIgnoreIfNull(true)),
        new IgnoreExtraElementsConvention(true)
    };
}

var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost:27017"));
settings.MaxConnectionPoolSize = 500;
var client = new MongoClient(settings);
var database = client.GetDatabase("Example");
# endregion

await database.DropCollectionAsync("PriceList");
var collection = database.GetCollection<PriceCatalogue>("PriceList");
foreach (var i in Enumerable.Range(1, 5))
{
    await collection.InsertOneAsync(new Fixture().Create<PriceCatalogue>());
}

var aggregationBson = collection.Aggregate().Project(
        new BsonDocument
        {
            {
                nameof(PriceCatalogue.PricedItemsMeta),
                new BsonDocument {{"$objectToArray", $"${nameof(PriceCatalogue.PricedItemsMeta)}"}}
            }
        })
    .Unwind(nameof(PriceCatalogue.PricedItemsMeta))
    .Project(
        x => new BsonDocument
        {
            {"_id", "$PricedItemsMeta.k"},
            {"PricedItemMeta", "$PricedItemsMeta.v"},
            {"PriceId", "$_id"}
        });
var aggregation = aggregationBson.As<PricedItemModel>();

var aggregationBsonList = aggregationBson.ToList();
var aggregationList = aggregation.ToList();

Console.WriteLine("Aggregation with 'As<PricedItemModel>' in pipeline result:");
Console.WriteLine(JsonSerializer.Serialize(aggregation.ToList()));
Console.WriteLine();
Console.WriteLine("Aggregation with BsonDocument result + manual serialization to PricedItemModel:");
Console.WriteLine(JsonSerializer.Serialize(aggregationBsonList.Select(x => BsonSerializer.Deserialize<PricedItemModel>(x))));