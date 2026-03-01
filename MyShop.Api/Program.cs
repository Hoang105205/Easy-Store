using Core.Data;
using Microsoft.EntityFrameworkCore;
using MyShop.Api.GraphQL;

// Vo bang https://localhost:7052/graphql/

var builder = WebApplication.CreateBuilder(args);

var connectionString = "Host=ep-jolly-dream-a1fadbch-pooler.ap-southeast-1.aws.neon.tech;Database=easy_store_db;Username=neondb_owner;Password=npg_ih3sOck4JfwZ;SslMode=Require;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.MapGraphQL();
app.Run();

