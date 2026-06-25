using JNPF.Tests.Gate.Auth;
using JNPF.Tests.Gate.Infrastructure;

namespace JNPF.Tests.Gate;

/// <summary>
/// JNPF.Tests.Gate — Sprint 0-A Day 3 后端安全 PoC 集成测试.
/// 退出码：0=全部通过，1=有失败.
/// </summary>
class Program
{
    static async Task<int> Main()
    {
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine("  JNPF.Tests.Gate — Security PoC Tests");
        Console.WriteLine("  Sprint 0-A Day 3");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();

        // Outbox PoC 2 (需要 SQL Server)
        await OutboxSqlServerPoC.RunAsync();

        Console.WriteLine();

        // JwtHandler 集成测试 (纯逻辑，无 DB 依赖)
        JwtHandlerIntegrationTests.Run();

        Console.WriteLine();

        var totalPassed = OutboxSqlServerPoC.Passed + JwtHandlerIntegrationTests.Passed;
        var totalFailed = OutboxSqlServerPoC.Failed + JwtHandlerIntegrationTests.Failed;

        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"  Total: {totalPassed} passed, {totalFailed} failed");
        Console.WriteLine("═══════════════════════════════════════════════");

        return totalFailed > 0 ? 1 : 0;
    }
}
