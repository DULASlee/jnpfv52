using System.Net;
using System.Text;
using System.Text.Json;
using JNPF.InteAssistant.Skills.Cognitive.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// R5 MCP HTTP 传输升级契约测试（施工包 21 R5）.
/// </summary>
public static class McpR5Tests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static async Task RunAllAsync()
    {
        await T1_RoutingMcpClient_DefaultsToInProc();
        T2_HttpMcpTransport_ParsesRestfulWrapper();
        await T3_InProcVsHttp_ParityThroughSimulatedGateway();
    }

    private static async Task T1_RoutingMcpClient_DefaultsToInProc()
    {
        var inProc = new InProcMcpClient(
            new IMcpToolHandler[] { new EchoTool() },
            NullLogger<InProcMcpClient>.Instance);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var routing = new RoutingMcpClient(
            inProc,
            new HttpMcpTransport(new FakeHttpClientFactory(), NullLogger<HttpMcpTransport>.Instance),
            config,
            NullLogger<RoutingMcpClient>.Instance);

        var tools = routing.ListTools();
        if (tools.Count != 1 || tools[0].Name != "test.echo")
            throw new Exception("T1 manifest 应含 test.echo");

        var result = await routing.CallToolAsync("test.echo", """{"msg":"r5"}""");
        if (!result.IsSuccess || !result.ContentJson.Contains("r5"))
            throw new Exception("T1 inproc 路由失败");
    }

    private static void T2_HttpMcpTransport_ParsesRestfulWrapper()
    {
        var wrapped = JsonSerializer.Serialize(new
        {
            code = 200,
            data = new { isSuccess = true, contentJson = """{"ok":true}""" },
        }, JsonOptions);

        var parsed = HttpMcpTransport.ParseToolResult(wrapped, "test.echo");
        if (!parsed.IsSuccess || !parsed.ContentJson.Contains("ok"))
            throw new Exception("T2 RESTfulResult 包装解析失败");

        var direct = JsonSerializer.Serialize(new { isSuccess = false, error = "boom" }, JsonOptions);
        var fail = HttpMcpTransport.ParseToolResult(direct, "test.echo");
        if (fail.IsSuccess || fail.Error != "boom")
            throw new Exception("T2 直出 McpToolResult 解析失败");
    }

    private static async Task T3_InProcVsHttp_ParityThroughSimulatedGateway()
    {
        var inProc = new InProcMcpClient(
            new IMcpToolHandler[] { new EchoTool() },
            NullLogger<InProcMcpClient>.Instance);

        var handler = new SimulatedGatewayHandler(inProc);
        var http = new HttpMcpTransport(
            new FakeHttpClientFactory(handler),
            NullLogger<HttpMcpTransport>.Instance);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpTools:GatewayBaseUrl"] = "http://127.0.0.1:5000",
                ["McpTools:Tools:test.echo:Transport"] = "http",
            })
            .Build();

        var routing = new RoutingMcpClient(
            inProc, http, config, NullLogger<RoutingMcpClient>.Instance);

        const string args = """{"msg":"parity"}""";
        var direct = await inProc.CallToolAsync("test.echo", args);
        var viaHttp = await routing.CallToolAsync("test.echo", args);

        if (direct.IsSuccess != viaHttp.IsSuccess
            || direct.ContentJson != viaHttp.ContentJson)
        {
            throw new Exception(
                $"T3 InProc/HTTP 行为不一致: direct={direct.ContentJson} http={viaHttp.ContentJson}");
        }
    }

    private sealed class EchoTool : IMcpToolHandler
    {
        public McpToolDescriptor Descriptor { get; } = new()
        {
            Name = "test.echo",
            Description = "R5 契约测试回显",
        };

        public Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
            => Task.FromResult(McpToolResult.Ok(argumentsJson));
    }

    private sealed class SimulatedGatewayHandler : HttpMessageHandler
    {
        private readonly InProcMcpClient _inProc;

        public SimulatedGatewayHandler(InProcMcpClient inProc) => _inProc = inProc;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            var call = JsonSerializer.Deserialize<McpCallRequest>(body, JsonOptions)
                ?? throw new InvalidOperationException("invalid call");

            var result = await _inProc.CallToolAsync(call.ToolName, call.ArgumentsJson, cancellationToken);
            var wrapped = JsonSerializer.Serialize(new { code = 200, data = result }, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(wrapped, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler? handler = null)
            => _handler = handler ?? new HttpClientHandler();

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
