namespace Api.GraphQL.Resolvers;

public class ProductStat
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastOrder { get; set; }
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public long Revenue { get; set; }
}

public class StoreStatistics
{
    [GraphQLIgnore]
    public DateTime? StartDate { get; }
    public DateTime? PreviousDate { get; }

    // Nhận tham số days từ Root Query
    public StoreStatistics(int? days)
    {
        if (days.HasValue)
        {
            StartDate = DateTime.UtcNow.Date.AddDays(-days.Value);
            PreviousDate = DateTime.UtcNow.Date.AddDays(-2 * days.Value);
        }
        else
        {
            StartDate = null;
            PreviousDate = null;
        }
    }
}
