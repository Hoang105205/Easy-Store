using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;


namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
internal class OrderQueries
{
    [UsePaging(DefaultPageSize = 20)]
    [UseSorting]
    public IQueryable<Order> GetOrders([Service] AppDbContext context)
    {
        var query = context.Orders.AsQueryable();

        query = query.OrderByDescending(p => p.OrderDate).ThenBy(p => p.Id);

        return query;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Order> GetOrderById(Guid id, [Service] AppDbContext dbContext)
    { 
        return dbContext.Orders.Where(p => p.Id == id);
    }
}
