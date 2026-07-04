using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace JNPF.Tests.PhaseB;

/// <summary>G3 D8 — TenantGuard 跨租户拒绝（无第二账号时的代码层证据）</summary>
public static class IrPhase3TenantTests
{
    public static void RunTenantGuardIsolation()
    {
        var guard = new TenantGuard(new HttpContextAccessor(), NullLogger<TenantGuard>.Instance);
        var pipeline = new AiPipelineEntity { TenantId = "100001" };

        if (guard.VerifyOwnership(pipeline, "100001") != true)
            throw new InvalidOperationException("same-tenant VerifyOwnership should pass");

        if (guard.VerifyOwnership(pipeline, "100002") != false)
            throw new InvalidOperationException("cross-tenant VerifyOwnership must return false");

        Console.WriteLine("[Phase3] TenantGuard cross-tenant isolation passed.");
    }
}
