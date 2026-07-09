using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B05 — CodeGeneratedStablePromoted + IR3 draft → stable 投影。
/// </summary>
public static class IrPhase4PromoteTests
{
    public static async Task RunAllAsync()
    {
        TestStablePromotedPayload_Format();
        TestMergeIr3Payload_PromotePatch();
        await TestProjection_PromoteIr3Async();
        Console.WriteLine("[Phase4] IR3 promote tests passed.");
    }

    private static void TestStablePromotedPayload_Format()
    {
        var sandbox = CodeSandboxBuildResult.Pass("BuildPass", TimeSpan.FromSeconds(1));
        var arch = new ArchGuardScanResult { Violations = Array.Empty<ArchGuardViolation>() };
        var payload = CodegenManifestBuilder.BuildCodeGeneratedStablePromotedPayload(
            "proj-1", sandbox, arch, "codegen:proj-1");

        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("stabilityState", out var st)
            || st.GetString() != IrStabilityStates.Stable)
        {
            throw new InvalidOperationException("StablePromoted payload missing stabilityState=stable");
        }

        if (!doc.RootElement.TryGetProperty("promotionGate", out var gate)
            || !gate.TryGetProperty("sandboxBuild", out var sb)
            || sb.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException("StablePromoted payload missing promotionGate.sandboxBuild");
        }
    }

    private static void TestMergeIr3Payload_PromotePatch()
    {
        var baseJson = """
            {"id":"codegen:p1","stabilityState":"draft","className":"LeaveRequest","sandboxBuild":{"passed":false}}
            """;
        var patch = CodegenManifestBuilder.BuildCodeGeneratedStablePromotedPayload(
            "p1",
            CodeSandboxBuildResult.Pass("BuildPass", TimeSpan.FromMilliseconds(500)),
            new ArchGuardScanResult { Violations = Array.Empty<ArchGuardViolation>() });

        var merged = CodegenManifestBuilder.MergeIr3Payload(baseJson, patch);
        using var doc = JsonDocument.Parse(merged);
        if (doc.RootElement.GetProperty("stabilityState").GetString() != IrStabilityStates.Stable)
            throw new InvalidOperationException("Merge should set stabilityState=stable");

        if (doc.RootElement.GetProperty("className").GetString() != "LeaveRequest")
            throw new InvalidOperationException("Merge should preserve existing fields");

        if (!doc.RootElement.TryGetProperty("promotedAt", out _))
            throw new InvalidOperationException("Merge should include promotedAt");
    }

    private static async Task TestProjection_PromoteIr3Async()
    {
        using var db = CreateSqliteClient();
        var projection = new IrProjectionEngine(db);
        const string tenantId = "_phase4-promote";
        const string projectId = "9004";
        const string fragmentId = "codegen:9004";

        var draftPayload = """
            {"id":"codegen:9004","stabilityState":"draft","className":"LeaveRequest"}
            """;

        await ProjectAsync(db, projection, tenantId, projectId, 1, IrEventTypes.CodeGenerated, fragmentId, 1, draftPayload);
        await ProjectAsync(
            db,
            projection,
            tenantId,
            projectId,
            2,
            IrEventTypes.CodegenBuildValidated,
            fragmentId,
            2,
            CodegenManifestBuilder.BuildCodegenBuildValidatedPayload(projectId, CodeSandboxBuildResult.Pass("BuildPass", TimeSpan.FromSeconds(1)), fragmentId));

        var promotePayload = CodegenManifestBuilder.BuildCodeGeneratedStablePromotedPayload(
            projectId,
            CodeSandboxBuildResult.Pass("BuildPass", TimeSpan.FromSeconds(1)),
            new ArchGuardScanResult { Violations = Array.Empty<ArchGuardViolation>() },
            fragmentId);

        await ProjectAsync(
            db,
            projection,
            tenantId,
            projectId,
            3,
            IrEventTypes.CodeGeneratedStablePromoted,
            fragmentId,
            3,
            promotePayload);

        var snap = await db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.FragmentId == fragmentId)
            .FirstAsync();

        if (snap.StabilityState != IrStabilityStates.Stable)
            throw new InvalidOperationException($"Expected stable snapshot, got {snap.StabilityState}");

        using var doc = JsonDocument.Parse(snap.IrContent);
        if (doc.RootElement.GetProperty("stabilityState").GetString() != IrStabilityStates.Stable)
            throw new InvalidOperationException("IrContent stabilityState should be stable");

        if (!doc.RootElement.GetProperty("sandboxBuild").GetProperty("passed").GetBoolean())
            throw new InvalidOperationException("IrContent should retain merged sandboxBuild.passed=true");
    }

    private static async Task ProjectAsync(
        SqlSugarClient db,
        IrProjectionEngine projection,
        string tenantId,
        string projectId,
        int sequence,
        string eventType,
        string fragmentId,
        int fragmentVersion,
        string payload)
    {
        var id = Guid.NewGuid().ToString("N");
        const string pipelineId = "pipe-promote";
        await db.Ado.ExecuteCommandAsync(
            "INSERT INTO ai_ir_events (F_Id,F_ProjectId,F_TenantId,F_PIPELINE_ID,F_EventType,F_FragmentId,F_FragmentType,F_FragmentVersion,F_Payload,F_SAStepName,F_Sequence,F_CreatedAt,F_IsRollback) VALUES (@id,@pid,@tid,@pplid,@etype,@fid,@ftype,@fver,@payload,NULL,@seq,@at,0)",
            new[]
            {
                new SugarParameter("@id", id),
                new SugarParameter("@pid", projectId),
                new SugarParameter("@tid", tenantId),
                new SugarParameter("@pplid", pipelineId),
                new SugarParameter("@etype", eventType),
                new SugarParameter("@fid", fragmentId),
                new SugarParameter("@ftype", IrFragmentTypes.GeneratedCode),
                new SugarParameter("@fver", fragmentVersion),
                new SugarParameter("@payload", payload),
                new SugarParameter("@seq", sequence),
                new SugarParameter("@at", DateTime.UtcNow.ToString("o")),
            });

        var evt = new AiIrEventEntity
        {
            Id = id,
            ProjectId = projectId,
            TenantId = tenantId,
            PipelineId = pipelineId,
            EventType = eventType,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            FragmentVersion = fragmentVersion,
            Payload = payload,
            Sequence = sequence,
            CreatedAt = DateTime.UtcNow,
        };

        await projection.ProjectEventAsync(evt);
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
                F_PIPELINE_ID TEXT NOT NULL DEFAULT '',
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
                F_PIPELINE_ID TEXT NOT NULL DEFAULT '',
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
