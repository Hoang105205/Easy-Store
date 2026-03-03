using Core.Data;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class UserQueries
{
    public IQueryable<User> GetUsers([Service] AppDbContext dbContext)
    {
        return dbContext.Users;
    }
}
