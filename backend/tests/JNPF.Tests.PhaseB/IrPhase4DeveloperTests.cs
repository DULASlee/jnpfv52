using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段四 P4-B01a — DeveloperSkill 落盘 + CodeGenerated payload。
/// </summary>
public static class IrPhase4DeveloperTests
{
    public static async Task RunAllAsync()
    {
        TestDevelopmentSkillIds_Defined();
        await TestDeveloperSkill_ValidateInputAsync();
        await TestDeveloperSkill_ReasonAsync_LeaveSimpleFixtureAsync();
        Console.WriteLine("[Phase4] Developer skill tests passed.");
    }

    private static void TestDevelopmentSkillIds_Defined()
    {
        if (DevelopmentSkillIds.Developer != "developer-skill")
            throw new InvalidOperationException("DevelopmentSkillIds.Developer mismatch");

        if (IrEventTypes.CodeGenerated != "CodeGenerated")
            throw new InvalidOperationException("IrEventTypes.CodeGenerated missing");
    }

    private static async Task TestDeveloperSkill_ValidateInputAsync()
    {
        var gate = new SystemDesignLockedCompletenessGate();
        var incomplete = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:1",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = "{}",
                },
            },
        };

        var result = await gate.ValidateAsync(incomplete);
        if (result.IsValid)
            throw new InvalidOperationException("ValidateInput should fail without SystemDesign locked");

        var skill = CreateDeveloperSkill();
        var okSnapshot = BuildLeaveSimpleSnapshot(includeSystemDesign: true);
        var ok = await skill.ValidateInputAsync(okSnapshot);
        if (!ok.IsValid)
            throw new InvalidOperationException($"ValidateInput should pass: {ok.ErrorMessage}");
    }

    private static async Task TestDeveloperSkill_ReasonAsync_LeaveSimpleFixtureAsync()
    {
        const string tenantId = "_phase4-test";
        const string projectId = "leave-simple-d4";

        var backendRoot = CodegenWorkspacePaths.ResolveBackendRoot(tenantId, projectId);
        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);

        var skill = CreateDeveloperSkill();
        var snapshot = BuildLeaveSimpleSnapshot(includeSystemDesign: true);
        var context = new SkillContext
        {
            RunId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9001,
            UserRequirement = "请假 MVP",
            Snapshot = snapshot,
        };

        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(context))
            events.Add(evt);

        var outputValidation = await skill.ValidateOutputAsync(events);
        if (!outputValidation.IsValid)
            throw new InvalidOperationException(outputValidation.ErrorMessage ?? "ValidateOutput failed");

        if (events.Count != 2)
            throw new InvalidOperationException("Expected 2 IR events");

        if (!File.Exists(Path.Combine(backendRoot, "Entitys", "LeaveRequestEntity.cs")))
            throw new InvalidOperationException("Entity file not written");

        if (!File.Exists(Path.Combine(backendRoot, "Services", "LeaveRequestService.cs")))
            throw new InvalidOperationException("Service file not written");

        using var doc = JsonDocument.Parse(events[0].Payload);
        // P9-S2：多实体 payload 改为 entityCount/fileCount（非旧 templateVersions）
        if (!doc.RootElement.TryGetProperty("entityCount", out var ecEl) || ecEl.GetInt32() <= 0)
            throw new InvalidOperationException("CodeGenerated entityCount 须 > 0");
        if (!doc.RootElement.TryGetProperty("fileCount", out var fcEl) || fcEl.GetInt32() <= 0)
            throw new InvalidOperationException("CodeGenerated fileCount 须 > 0");

        if (Directory.Exists(backendRoot))
            Directory.Delete(backendRoot, recursive: true);
    }

    public static DeveloperSkillService CreateDeveloperSkillPublic() => CreateDeveloperSkill();

    public static IrSnapshot BuildLeaveSimpleSnapshotPublic(bool includeSystemDesign) =>
        BuildLeaveSimpleSnapshot(includeSystemDesign);

    private static DeveloperSkillService CreateDeveloperSkill()
    {
        using var loggerFactory = LoggerFactory.Create(static _ => { });
        return new DeveloperSkillService(
            new TemplateContextBuilder(),
            new CodegenWorkspaceWriter(),
            new SystemDesignLockedCompletenessGate(),
            new CodegenBackendRegistry(),
            loggerFactory.CreateLogger<DeveloperSkillService>(),
            new EntityDesignRepository(CreateSqliteClientWithEntityFieldTable()));
    }

    /// <summary>内存 SQLite + ai_entity_field 表，供 PersistAsync 落表验证。</summary>
    private static SqlSugarClient CreateSqliteClientWithEntityFieldTable()
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
            CREATE TABLE ai_entity_field (
                F_Id TEXT PRIMARY KEY,
                F_TenantId TEXT NOT NULL DEFAULT '',
                F_ProjectId TEXT NOT NULL DEFAULT '',
                F_PIPELINE_ID TEXT NOT NULL DEFAULT '',
                F_SchemaVersion TEXT NOT NULL DEFAULT 'entity-field.v1',
                F_ProjectionHash TEXT NOT NULL DEFAULT '',
                F_SourceFragmentId TEXT NOT NULL DEFAULT '',
                F_SourceDdlFragmentId TEXT,
                F_EntityName TEXT NOT NULL DEFAULT '',
                F_EntityDisplayName TEXT,
                F_TableName TEXT NOT NULL DEFAULT '',
                F_FieldName TEXT NOT NULL DEFAULT '',
                F_PropertyName TEXT NOT NULL DEFAULT '',
                F_DbColumnName TEXT NOT NULL DEFAULT '',
                F_CSharpType TEXT NOT NULL DEFAULT 'string',
                F_SqlType TEXT NOT NULL DEFAULT 'NVARCHAR(255)',
                F_IsRequired INTEGER NOT NULL DEFAULT 0,
                F_IsPrimaryKey INTEGER NOT NULL DEFAULT 0,
                F_IsNullable INTEGER NOT NULL DEFAULT 1,
                F_IsIdentity INTEGER NOT NULL DEFAULT 0,
                F_References TEXT,
                F_ReferencesTable TEXT,
                F_ReferencesColumn TEXT,
                F_CreatorTime TEXT NOT NULL,
                F_LastModifyTime TEXT,
                F_DeleteMark INTEGER NOT NULL DEFAULT 0
            );
            """);
        return client;
    }

    private static IrSnapshot BuildLeaveSimpleSnapshot(bool includeSystemDesign)
    {
        var samplesDir = TemplateRenderSamplesTests.ResolveSamplesDirPublic();
        var json = File.ReadAllText(Path.Combine(samplesDir, "leave-simple.json"));
        using var doc = JsonDocument.Parse(json);
        var fragmentsEl = doc.RootElement.GetProperty("fragments");
        var fragments = new List<IrSnapshotFragment>();

        foreach (var f in fragmentsEl.EnumerateArray())
        {
            var fragmentType = f.GetProperty("fragmentType").GetString() ?? string.Empty;
            if (!includeSystemDesign && fragmentType == IrFragmentTypes.SystemDesign)
                continue;

            var payloadEl = f.GetProperty("payload");
            var payload = payloadEl.ValueKind == JsonValueKind.String
                ? payloadEl.GetString() ?? "{}"
                : payloadEl.GetRawText();

            fragments.Add(new IrSnapshotFragment
            {
                FragmentId = f.GetProperty("fragmentId").GetString() ?? string.Empty,
                FragmentType = fragmentType,
                StabilityState = f.TryGetProperty("stabilityState", out var st)
                    ? st.GetString() ?? IrStabilityStates.Stable
                    : IrStabilityStates.Stable,
                Payload = payload,
            });
        }

        return new IrSnapshot { Fragments = fragments };
    }
}
