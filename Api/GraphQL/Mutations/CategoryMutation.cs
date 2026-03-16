using Core.Data;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class CategoryMutation
{
    public async Task<Category> CreateCategory(
        string name,
        [Service] AppDbContext context)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return category;
    }

    public async Task<bool> DeleteCategoryAsync(
            Guid id,
            [Service] AppDbContext context)
    {
        var category = await context.Categories.FindAsync(id);
        if (category == null)
        {
            throw new GraphQLException("Danh mục này không tồn tại hoặc đã bị xóa trước đó.");
        }

        bool hasProducts = await context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            throw new GraphQLException("Không thể xóa danh mục này vì đang có sản phẩm thuộc danh mục. Vui lòng xóa hoặc chuyển sản phẩm sang danh mục khác trước.");
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        return true;
    }
}