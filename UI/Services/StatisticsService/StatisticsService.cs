using StrawberryShake;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UI.Services.StatisticsService;

public class StatisticsService
{
    private readonly IEasyStoreClient _client;

    public StatisticsService(IEasyStoreClient client)
    {
        _client = client;
    }

    public async Task<IOperationResult<IGetStatisticsResult>> GetStatisticsAsync(DateTime from, DateTime to)
    {
        return await _client.GetStatistics.ExecuteAsync(from, to);
    }
}
