using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ProductQueries
{
    [UsePaging(DefaultPageSize = 20)]
    [UseProjection]
    [UseSorting]
    public IQueryable<Product> GetProducts(
            [Service] AppDbContext context,
            string? searchTerm = null,
            Guid? categoryId = null
        )
    {
        var query = context.Products.AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string searchPattern = $"%{searchTerm}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, searchPattern) ||
                EF.Functions.ILike(p.SKU, searchPattern));
        }

        // Return IQueryable
        return query;
    }

    [UseProjection]
    public Product? GetProductById(Guid id, [Service] AppDbContext dbContext)
    {
        return dbContext.Products.Include(p => p.Category).Include(p => p.Images).FirstOrDefault(p => p.Id == id);
    }
}