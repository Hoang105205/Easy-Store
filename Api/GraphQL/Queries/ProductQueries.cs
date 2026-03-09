using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using System.Linq;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ProductQueries
{
    // Thẻ [UsePaging] kích hoạt Cursor-based pagination mặc định của HotChocolate.
    // [UseProjection] cho phép tự động ánh xạ các trường con của Product khi truy vấn.
    [UsePaging(DefaultPageSize = 20)]
    [UseProjection]
    public IQueryable<Product> GetProducts([Service] AppDbContext dbContext)
    {
        return dbContext.Products;
    }
}