using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class CategoryQueries
{
    [UseProjection]
    public IQueryable<Category> GetCategories([Service] AppDbContext context)
    {
        return context.Categories;
    }
}