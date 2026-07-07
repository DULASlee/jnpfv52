using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using JNPF.Tests.Gate.Auth;
using Xunit;
using Xunit.Abstractions;

namespace JNPF.Tests.Gate.E2E;

/// <summary>
/// Studio API 快测 — 每条独立 filter，不跑全链、不用 simulate。
///
/// 示例：
///   dotnet test backend/tests/JNPF.Tests.Gate --filter "FullyQualifiedName~StudioApiSmokeTests.Deliverables"
///   E2E_PIPELINE_ID=294 dotnet test ... --filter "FullyQualifiedName~StudioApiSmokeTests.Deliverables"
/// </summary>
public class StudioApiSmokeTests
{
    private readonly ITestOutputHelper? _output;
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("TEST_API_URL") ?? "http://localhost:5000"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    public StudioApiSmokeTests(ITestOutputHelper? output = null) => _output = output;

    private async Task<string> AuthAsync()
    {
        var token = await JnpfTestAuthHelper.GetTokenAsync(Client);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    private static long PipelineId =>
        long.TryParse(Environment.GetEnvironmentVariable("E2E_PIPELINE_ID"), out var id) ? id : 0;

    [Fact]
    public async Task McpTools_Returns200()
    {
        if (!await ServerUp()) return;
        await AuthAsync();
        var resp = await Client.GetAsync("/api/studio/mcp/tools");
        var body = await resp.Content.ReadAsStringAsync();
        _output?.WriteLine(body);
        Assert.True(resp.IsSuccessStatusCode, $"GET /api/studio/mcp/tools → {(int)resp.StatusCode} {body}");
    }

    [Fact]
    public async Task Deliverables_List_ForPipeline()
    {
        if (!await ServerUp()) return;
        Assert.True(PipelineId > 0, "设置 E2E_PIPELINE_ID 环境变量");

        await AuthAsync();
        var resp = await Client.GetAsync($"/api/studio/pipeline/execute/{PipelineId}/deliverables");
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _output?.WriteLine(json.ToString());

        Assert.True(resp.IsSuccessStatusCode, $"deliverables HTTP {(int)resp.StatusCode}");
        Assert.True(json.TryGetProperty("items", out var items) || json.TryGetProperty("data", out _),
            "响应应含 items 或 data");
    }

    [Fact]
    public async Task IrEvents_List_ForPipeline()
    {
        if (!await ServerUp()) return;
        Assert.True(PipelineId > 0, "设置 E2E_PIPELINE_ID");

        await AuthAsync();
        var resp = await Client.GetAsync($"/api/studio/ir/{PipelineId}/events");
        Assert.True(resp.IsSuccessStatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _output?.WriteLine(json.ToString().Length > 500 ? json.ToString()[..500] + "…" : json.ToString());
    }

    private static async Task<bool> ServerUp()
    {
        try
        {
            using var c = new HttpClient { BaseAddress = Client.BaseAddress, Timeout = TimeSpan.FromSeconds(3) };
            var r = await c.GetAsync("/api/oauth/getLoginConfig");
            return r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Forbidden;
        }
        catch
        {
            return false;
        }
    }
}
