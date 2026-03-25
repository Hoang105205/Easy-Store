using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Services.CategoryService
{
    public class DashboardService
    {
        private readonly IEasyStoreClient _client;

        public DashboardService()
        {
            // Lấy GraphQL Client từ Service Provider
            _client = App.Current.Services.GetRequiredService<IEasyStoreClient>();
        }

        public async Task<IGetDashboardStats_DashboardStats?> GetDashboardOverviewAsync(int? days = null)
        {
            try
            {
                var result = await _client.GetDashboardStats.ExecuteAsync(days);

                if (result.Errors?.Count > 0)
                {
                    throw new Exception(result.Errors[0].Message);
                }

                return result.Data?.DashboardStats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI DASHBOARD SERVICE] {ex.Message}");
                throw;
            }
        }
    }
}
