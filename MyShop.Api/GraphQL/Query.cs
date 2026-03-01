using Core;
using Core.Data;
using Core.Models;
using HotChocolate;

namespace MyShop.Api.GraphQL;

public class Query
{
    public IQueryable<Category> GetCategories([Service] AppDbContext context)
    {
        return context.Categories;
    }

    public IQueryable<User> GetUsers([Service] AppDbContext context)
    {
        return context.Users;
    }
}