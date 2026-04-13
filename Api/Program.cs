using Api.GraphQL;
using Api.GraphQL.Mutations;
using Api.GraphQL.Queries;
using Api.GraphQL.Resolvers;
using Core.Data;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// Vo bang https://localhost:7052/graphql/

var builder = WebApplication.CreateBuilder(args);

// 1. LẤY CẤU HÌNH: Ưu tiên lấy từ Command Line (UI truyền xuống)
// .NET sẽ tự động map tham số --ConnectionStrings:DefaultConnection vào đây
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Kiểm tra nếu không có chuỗi kết nối thì báo lỗi rõ ràng
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[ERROR] ConnectionString is missing! Please provide it from UI.");
}

// 2. ĐĂNG KÝ SERVICES
builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Tự động thử lại nếu Neon bị lag (thời gian chờ 30s)
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
        });
    }
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddTypeExtension<UserQueries>()
    .AddTypeExtension<ProductQueries>()
    .AddTypeExtension<CategoryQueries>()
    .AddTypeExtension<ImportQueries>()
    .AddTypeExtension<OrderQueries>()
    .AddTypeExtension<DashboardQueries>()
    .AddTypeExtension<StatisticsQuery>()
    .AddTypeExtension<StoreStatisticsResolvers>()
    .AddMutationType<Mutation>()
    .AddTypeExtension<AuthMutation>()
    .AddTypeExtension<ProductMutation>()
    .AddTypeExtension<CategoryMutation>()
    .AddTypeExtension<ImportMutation>()
    .AddTypeExtension<OrderMutation>()
    .AddTypeExtension<UserMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    //.ModifyCostOptions(opt =>
    //{
    //    opt.MaxFieldCost = 5000;
    //    opt.MaxTypeCost = 5000;
    //    opt.EnforceCostLimits = false; 
    //})
    .DisableIntrospection(false);

var app = builder.Build();

// 3. CẤU HÌNH HTTP PIPELINE
app.MapGraphQL();

// Ép API chạy đúng cổng 5000 để UI biết đường mà gọi
app.Run("http://localhost:5000");

