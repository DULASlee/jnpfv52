using SqlSugar;

namespace JNPF.Tests.ADR012;

/// <summary>
/// 测试用 ISqlSugarDbContextProvider — 直接返回传入的 SqlSugarClient.
/// </summary>
public class MockDbContextProvider : ISqlSugarDbContextProvider
{
    private readonly ISqlSugarClient _client;

    public MockDbContextProvider(ISqlSugarClient client)
    {
        _client = client;
    }

    public ISqlSugarClient GetDbContext() => _client;
}
