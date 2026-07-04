using System.Diagnostics;
using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段五 P5-B01 — IrDiffEngine 单元测试。
/// </summary>
public static class IrPhase5DiffTests
{
    public static async Task RunAllAsync()
    {
        await TestSameSequence_EmptyDiffAsync();
        await TestPromote_ChangedFragmentAsync();
        await TestEventSpecChange_PropagatesDownstreamAsync();
        await TestLockedFragment_SkippedWithoutForceUnlockAsync();
        await TestPerformance_100Events_Under500msAsync();
        Console.WriteLine("[Phase5] IrDiffEngine tests passed.");
    }

    private static async Task TestSameSequence_EmptyDiffAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-diff";
        const string projectId = "9101";
        await SeedMinimalIr3ChainAsync(db, tenantId, projectId);

        var engine = new IrDiffEngine(db);
        var result = await engine.CompareAsync(projectId, tenantId, 5, 5);

        if (!result.IsEmpty)
            throw new InvalidOperationException($"Expected empty diff, got added={result.Added.Count} changed={result.Changed.Count}");
    }

    private static async Task TestPromote_ChangedFragmentAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-diff";
        const string projectId = "9102";
        await SeedMinimalIr3ChainAsync(db, tenantId, projectId);

        var engine = new IrDiffEngine(db);
        var result = await engine.CompareAsync(projectId, tenantId, 4, 6);

        if (!result.Changed.Contains($"codegen:{projectId}"))
            throw new InvalidOperationException("Promote should mark codegen fragment changed");

        if (!result.Added.Contains($"testsuite:{projectId}"))
            throw new InvalidOperationException("TestSuite should appear as added");
    }

    private static async Task TestEventSpecChange_PropagatesDownstreamAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-diff";
        const string projectId = "9103";
        await SeedMinimalIr3ChainAsync(db, tenantId, projectId);

        await InsertEventAsync(
            db,
            tenantId,
            projectId,
            7,
            IrEventTypes.EventSpecConfirmed,
            "eventspec:BE-001",
            IrFragmentTypes.EventSpec,
            2,
            """{"confirmedFields":[{"name":"Reason","type":"string","required":true},{"name":"Days","type":"int","required":true}]}""");

        var engine = new IrDiffEngine(db);
        var result = await engine.CompareAsync(projectId, tenantId, 6, 7);

        if (!result.Changed.Contains("eventspec:BE-001"))
            throw new InvalidOperationException("EventSpec revision should be in changed");

        if (!result.Invalidated.Contains("ddl:9103"))
            throw new InvalidOperationException("DDL should be invalidated after EventSpec change");

        if (!result.Invalidated.Contains("codegen:9103"))
            throw new InvalidOperationException("GeneratedCode should be invalidated after EventSpec change");

        if (result.Invalidated.Contains("arch:9103"))
            throw new InvalidOperationException("Architecture must stay unchanged for field-level EventSpec bug (D3)");
    }

    private static async Task TestLockedFragment_SkippedWithoutForceUnlockAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-diff";
        const string projectId = "9104";

        await InsertEventAsync(db, tenantId, projectId, 1, IrEventTypes.SystemDesignLocked, "system:9104", IrFragmentTypes.SystemDesign, 1, """{"locked":true}""");
        await InsertEventAsync(db, tenantId, projectId, 2, IrEventTypes.SystemDesignLocked, "system:9104", IrFragmentTypes.SystemDesign, 2, """{"locked":true,"patch":true}""");

        var engine = new IrDiffEngine(db);
        var result = await engine.CompareAsync(projectId, tenantId, 1, 2);

        if (result.Changed.Contains("system:9104"))
            throw new InvalidOperationException("Locked fragment must not appear in changed without ForceUnlock");

        var forced = await engine.CompareAsync(projectId, tenantId, 1, 2, new IrDiffOptions { ForceUnlock = true });
        if (!forced.Changed.Contains("system:9104"))
            throw new InvalidOperationException("ForceUnlock should allow locked fragment diff");
    }

    private static async Task TestPerformance_100Events_Under500msAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-diff";
        const string projectId = "9105";

        for (var i = 1; i <= 100; i++)
        {
            await InsertEventAsync(
                db,
                tenantId,
                projectId,
                i,
                IrEventTypes.SaStepCompleted,
                "skeleton:SK-001",
                IrFragmentTypes.Skeleton,
                i,
                """{"step":"DomainModel"}""",
                saStep: IrSaSteps.All[i % IrSaSteps.All.Length]);
        }

        var engine = new IrDiffEngine(db);
        var sw = Stopwatch.StartNew();
        var result = await engine.CompareAsync(projectId, tenantId, 50, 100);
        sw.Stop();

        if (sw.ElapsedMilliseconds >= 500)
            throw new InvalidOperationException($"Diff 100 events took {sw.ElapsedMilliseconds}ms, expected <500ms");

        if (result.ToSequence != 100)
            throw new InvalidOperationException("Unexpected toSequence in perf test");
    }

    private static async Task SeedMinimalIr3ChainAsync(SqlSugarClient db, string tenantId, string projectId)
    {
        await InsertEventAsync(
            db, tenantId, projectId, 1, IrEventTypes.EventSpecConfirmed,
            "eventspec:BE-001", IrFragmentTypes.EventSpec, 1,
            """{"confirmedFields":[{"name":"Reason","type":"string","required":true}]}""");

        await InsertEventAsync(
            db, tenantId, projectId, 2, IrEventTypes.DDLStabilized,
            $"ddl:{projectId}", IrFragmentTypes.DDL, 1,
            """{"tables":["LeaveRequest"]}""");

        await InsertEventAsync(
            db, tenantId, projectId, 3, IrEventTypes.ArchitectureDecisionRecorded,
            $"arch:{projectId}", IrFragmentTypes.Architecture, 1,
            """{"pattern":"single-table"}""");

        var codegenFragment = $"codegen:{projectId}";

        await InsertEventAsync(
            db, tenantId, projectId, 4, IrEventTypes.CodeGenerated,
            codegenFragment, IrFragmentTypes.GeneratedCode, 1,
            $$"""{"id":"{{codegenFragment}}","stabilityState":"draft","className":"LeaveRequest"}""");

        await InsertEventAsync(
            db,
            tenantId,
            projectId,
            5,
            IrEventTypes.CodeGeneratedStablePromoted,
            codegenFragment,
            IrFragmentTypes.GeneratedCode,
            2,
            CodegenManifestBuilder.BuildCodeGeneratedStablePromotedPayload(
                projectId,
                CodeSandboxBuildResult.Pass("BuildPass", TimeSpan.FromSeconds(1)),
                new ArchGuardScanResult { Violations = Array.Empty<ArchGuardViolation>() },
                codegenFragment));

        await InsertEventAsync(
            db,
            tenantId,
            projectId,
            6,
            IrEventTypes.TestSuiteGenerated,
            $"testsuite:{projectId}",
            IrFragmentTypes.TestSuite,
            1,
            $$"""{"id":"testsuite:{{projectId}}","stabilityState":"stable","scenarioCount":3,"derivationMode":"field-only"}""");
    }

    private static async Task InsertEventAsync(
        SqlSugarClient db,
        string tenantId,
        string projectId,
        int sequence,
        string eventType,
        string fragmentId,
        string fragmentType,
        int fragmentVersion,
        string payload,
        string? saStep = null)
    {
        var id = Guid.NewGuid().ToString("N");
        await db.Ado.ExecuteCommandAsync(
            "INSERT INTO ai_ir_events (F_Id,F_ProjectId,F_TenantId,F_EventType,F_FragmentId,F_FragmentType,F_FragmentVersion,F_Payload,F_SAStepName,F_Sequence,F_CreatedAt,F_IsRollback) VALUES (@id,@pid,@tid,@etype,@fid,@ftype,@fver,@payload,@sa,@seq,@at,0)",
            new[]
            {
                new SugarParameter("@id", id),
                new SugarParameter("@pid", projectId),
                new SugarParameter("@tid", tenantId),
                new SugarParameter("@etype", eventType),
                new SugarParameter("@fid", fragmentId),
                new SugarParameter("@ftype", fragmentType),
                new SugarParameter("@fver", fragmentVersion),
                new SugarParameter("@payload", payload),
                new SugarParameter("@sa", saStep),
                new SugarParameter("@seq", sequence),
                new SugarParameter("@at", DateTime.UtcNow.ToString("o")),
            });
    }

    private static SqlSugarClient CreateSqliteClient()
    {
        var client = new SqlSugarClient(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = "DataSource=:memory:",
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        });
        client.Open();
        client.Ado.ExecuteCommand("""
            CREATE TABLE ai_ir_events (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_EventType TEXT NOT NULL,
                F_FragmentType TEXT,
                F_FragmentId TEXT,
                F_FragmentVersion INTEGER NOT NULL DEFAULT 1,
                F_Payload TEXT NOT NULL,
                F_SkillId TEXT,
                F_SAStepName TEXT,
                F_Sequence INTEGER NOT NULL,
                F_CreatedAt TEXT NOT NULL,
                F_IsRollback INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE ai_ir_fragment_snapshots (
                F_Id TEXT PRIMARY KEY,
                F_ProjectId TEXT NOT NULL,
                F_TenantId TEXT NOT NULL,
                F_FragmentId TEXT NOT NULL,
                F_FragmentType TEXT NOT NULL,
                F_CurrentVersion INTEGER NOT NULL,
                F_StabilityState TEXT NOT NULL DEFAULT 'draft',
                F_IrContent TEXT NOT NULL,
                F_SAStepsCompleted TEXT,
                F_LastEventId TEXT NOT NULL,
                F_UpdatedAt TEXT NOT NULL,
                F_DeleteMark INTEGER NOT NULL DEFAULT 0
            );
            """);
        return client;
    }
}
