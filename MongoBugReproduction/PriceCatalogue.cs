namespace MongoBugReproduction
{
    public class PriceCatalogue
    {
        public Guid Id { get; set; }
        public Dictionary<string, PricedItemMeta> PricedItemsMeta { get; set; }
        
    }
}