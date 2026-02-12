using System.Net.Http.Json;
using microbians.io.web.Client.Models;

namespace microbians.io.web.Client.Services;

public class CmsAdminService
{
    private readonly HttpClient _httpClient;

    public CmsAdminService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CmsDashboardStats?> GetDashboardStatsAsync()
    {
        return await _httpClient.GetFromJsonAsync<CmsDashboardStats>("api/cms/admin/stats");
    }
}
