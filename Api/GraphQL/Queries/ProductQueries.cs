using Core.Data;
using Core.Models;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ProductQueries
{
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
            string cleanSearchTerm = Regex.Replace(searchTerm, @"[^\p{L}\p{N}\s]", " ");

            var words = cleanSearchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 0)
            {
                string prefixFtsQuery = string.Join(" & ", words.Select(w => $"{w}:*"));

                query = query.Where(p =>
                    EF.Functions.ToTsVector("simple", (p.Name ?? "") + " " + (p.SKU ?? ""))
                        .Matches(EF.Functions.ToTsQuery("simple", prefixFtsQuery))
                );
            }
        }

        query = query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);

        // Return IQueryable
        return query;
    }

    [UsePaging(DefaultPageSize = 20)]
    [UseProjection]
    [UseSorting]
    public IQueryable<Product> GetProductsPagination(
            [Service] AppDbContext context,
            string? searchTerm = null,
            Guid? categoryId = null,
            long? minPrice = null,
            long? maxPrice = null
        )
    {
        var query = context.Products.AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.SalePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.SalePrice <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string cleanSearchTerm = Regex.Replace(searchTerm, @"[^\p{L}\p{N}\s]", " ");

            var words = cleanSearchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 0)
            {
                string prefixFtsQuery = string.Join(" & ", words.Select(w => $"{w}:*"));

                query = query.Where(p =>
                    EF.Functions.ToTsVector("simple", (p.Name ?? "") + " " + (p.SKU ?? ""))
                        .Matches(EF.Functions.ToTsQuery("simple", prefixFtsQuery))
                );
            }
        }

        query = query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id);

        // Return IQueryable
        return query;
    }

    [UseSingleOrDefault]
    [UseProjection]
    public IQueryable<Product> GetProductById(Guid id, [Service] AppDbContext dbContext)
    {
        return dbContext.Products.Where(p => p.Id == id);
    }
}