if (args.Length > 0 && args[0] == "phase3-maxcalls")
{
    JNPF.Tests.PhaseB.IrPhase3Tests.RunMaxCallsOnly();
    return 0;
}

if (args.Length > 0 && args[0] == "phase3-tenant-isolation")
{
    JNPF.Tests.PhaseB.IrPhase3TenantTests.RunTenantGuardIsolation();
    return 0;
}

if (args.Length > 0 && args[0] == "generate-hashes")
    return JNPF.Tests.PhaseB.TemplateRenderSamplesTests.GenerateExpectedHashes();

if (args.Length > 0 && args[0] == "sandbox-gate")
{
    await JNPF.Tests.PhaseB.CodegenSandboxGateTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "developer-skill")
{
    await JNPF.Tests.PhaseB.IrPhase4DeveloperTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "developer-orchestrator")
{
    await JNPF.Tests.PhaseB.IrPhase4OrchestratorTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "arch-guard")
{
    await JNPF.Tests.PhaseB.IrPhase4ArchGuardTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "arch-guard-q2")
{
    var profile = args.Length > 2 && args[1] == "--profile" ? args[2] : null;
    await JNPF.Tests.PhaseB.IrPhase4ArchGuardQ2Tests.RunAllAsync(profile);
    return 0;
}

if (args.Length > 0 && args[0] == "host-demo")
{
    await JNPF.Tests.PhaseB.CodegenHostDemoTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "host-demo-build")
{
    await JNPF.Tests.PhaseB.CodegenHostDemoTests.RunFullBuildAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "ir3-promote")
{
    await JNPF.Tests.PhaseB.IrPhase4PromoteTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "phase5-diff")
{
    await JNPF.Tests.PhaseB.IrPhase5DiffTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "phase5-bugfix")
{
    await JNPF.Tests.PhaseB.IrPhase5BugfixTests.RunAllAsync();
    return 0;
}

if (args.Length > 0 && args[0] == "cognitive-r0")
{
    await JNPF.Tests.PhaseB.CognitiveSkillR0Tests.RunAllAsync();
    Console.WriteLine("  ✅ cognitive-r0 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "pm-r1")
{
    JNPF.Tests.PhaseB.PmSkillR1Tests.RunAll();
    Console.WriteLine("  ✅ pm-r1 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "analyst-r2")
{
    JNPF.Tests.PhaseB.AnalystSkillR2Tests.RunAll();
    Console.WriteLine("  ✅ analyst-r2 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "design-r3")
{
    JNPF.Tests.PhaseB.DesignSkillR3Tests.RunAll();
    Console.WriteLine("  ✅ design-r3 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "experience-r4")
{
    JNPF.Tests.PhaseB.ExperienceR4Tests.RunAll();
    Console.WriteLine("  ✅ experience-r4 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "mcp-r5")
{
    await JNPF.Tests.PhaseB.McpR5Tests.RunAllAsync();
    Console.WriteLine("  ✅ mcp-r5 全部通过");
    return 0;
}

if (args.Length > 0 && args[0] == "business-p0")
{
    JNPF.Tests.PhaseB.BusinessP0Tests.RunAll();
    Console.WriteLine("  ✅ business-p0 全部通过");
    return 0;
}

return await JNPF.Tests.PhaseB.TestRunner.Main(args);
