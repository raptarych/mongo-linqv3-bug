using System.Text.Json;
using AutoFixture;
using MongoBugReproduction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

ConventionRegistry.Register("Example", new ConventionPack
{
    new IgnoreExtraElementsConvention(true)
}, _ => true);

var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://localhost:27017"));

// this is important: bug won't reproduce if LinqProvider is V2
settings.LinqProvider = LinqProvider.V3;

var database = new MongoClient(settings).GetDatabase("Example");

await database.DropCollectionAsync("PriceList");
var collection = database.GetCollection<PriceCatalogue>("PriceCatalogues");
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
/*

Actual query with LinqProvider.V3 looks like:
 
db.getCollection('PriceCatalogues').aggregate([
    { "$project" : { "PricedItemsMeta" : { "$objectToArray" : "$PricedItemsMeta" }} }, 
    { "$unwind" : "$PricedItemsMeta" }, 
    { "$project" : { 
        "_v" : { 
            "_id" : "$PricedItemsMeta.k", 
            "PricedItemMeta" : "$PricedItemsMeta.v", 
            "PriceId" : "$_id" 
        }, 
        "_id" : 0 
    }}
])



 */

var aggregationBsonList = aggregationBson.ToList();
var aggregationList = aggregationBson.As<PricedItemModel>().ToList();

Console.WriteLine("Aggregation with 'As<PricedItemModel>' in pipeline result:");
Console.WriteLine();
Console.WriteLine(JsonSerializer.Serialize(aggregationList));
Console.WriteLine();
Console.WriteLine("Aggregation with BsonDocument result + manual serialization to PricedItemModel:");
Console.WriteLine();
Console.WriteLine(JsonSerializer.Serialize(aggregationBsonList.Select(x => BsonSerializer.Deserialize<PricedItemModel>(x))));