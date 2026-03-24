using Api.GraphQL.Resolvers;

namespace Api.GraphQL.Queries;

[ExtendObjectType("Query")]
public class DashboardQueries
{
    public StoreStatistics GetDashboardStats(int? days = 7)
    {
        return new StoreStatistics(days);
    }
}