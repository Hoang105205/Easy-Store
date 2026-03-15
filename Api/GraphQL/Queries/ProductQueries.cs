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
    // [UsePaging] kích hoạt Cursor-based pagination mặc định của HotChocolate.
    // [UseProjection] cho phép tự động ánh xạ các trường con của Product khi truy vấn.
    [UsePaging(DefaultPageSize = 20)]
    [UseProjection]
    public IQueryable<Product> GetProducts([Service] AppDbContext dbContext)
    {
        return dbContext.Products.OrderByDescending(p => p.CreatedAt);
    }

    [UseProjection]
    public Product? GetProductById(Guid id, [Service] AppDbContext dbContext)
    {
        return dbContext.Products.Include(p => p.Category).Include(p => p.Images).FirstOrDefault(p => p.Id == id);
    }
}