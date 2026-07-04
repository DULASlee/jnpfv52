using System.Text.Json;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using JNPF.InteAssistant.Skills.Bugfix;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 阶段五 P5-B02 — bugfix-skill + AffectedFragmentsMarked。
/// </summary>
public static class IrPhase5BugfixTests
{
    public static async Task RunAllAsync()
    {
        TestBugfixSkillIds_Defined();
        TestRootCauseClassifier_EventSpec_IsIr1();
        await TestBugfixSkill_EventSpecChange_MarksDownstreamAsync();
        await TestBugfixSkill_EmptyDiff_RejectedAsync();
        await TestProjection_AffectedFragmentsMarkedAsync();
        Console.WriteLine("[Phase5] Bugfix skill tests passed.");
    }

    private static void TestBugfixSkillIds_Defined()
    {
        if (BugfixSkillIds.Bugfix != "bugfix-skill")
            throw new InvalidOperationException("BugfixSkillIds.Bugfix mismatch");
    }

    private static void TestRootCauseClassifier_EventSpec_IsIr1()
    {
        var diff = new IrDiffResult
        {
            Changed = ["eventspec:BE-001"],
            Invalidated = ["ddl:p1", "codegen:p1"],
        };
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eventspec:BE-001"] = IrFragmentTypes.EventSpec,
            ["ddl:p1"] = IrFragmentTypes.DDL,
            ["codegen:p1"] = IrFragmentTypes.GeneratedCode,
        };

        var layer = BugfixRootCauseClassifier.Classify(diff, map, null);
        if (layer != BugfixRootCauseClassifier.LayerIr1)
            throw new InvalidOperationException($"Expected IR-1 root cause, got {layer}");
    }

    private static async Task TestBugfixSkill_EventSpecChange_MarksDownstreamAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-bugfix";
        const string projectId = "9201";

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

        await ProjectAllEventsAsync(db);

        var skill = CreateBugfixSkill(db);
        var snapshot = await LoadSnapshotAsync(db, tenantId, projectId);
        var context = new SkillContext
        {
            RunId = "run-bf1",
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9201,
            UserRequirement = "leave bugfix",
            Snapshot = snapshot,
            Bugfix = new BugfixRunContext
            {
                FromSequence = 6,
                ToSequence = 7,
                Description = "Days 字段类型修正",
                RevisionType = EventSpecRevisionPlanner.FieldTypeOrConstraint,
            },
        };

        var events = new List<AppendIrEventRequest>();
        await foreach (var evt in skill.ReasonAsync(context))
            events.Add(evt);

        if (!events.Any(e => e.EventType == IrEventTypes.AffectedFragmentsMarked))
            throw new InvalidOperationException("Missing AffectedFragmentsMarked");

        var marked = events.First(e => e.EventType == IrEventTypes.AffectedFragmentsMarked);
        using var doc = JsonDocument.Parse(marked.Payload);
        var invalidated = doc.RootElement.GetProperty("invalidated").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();

        if (!invalidated.Contains($"ddl:{projectId}"))
            throw new InvalidOperationException("DDL must be invalidated");

        if (!invalidated.Contains($"codegen:{projectId}"))
            throw new InvalidOperationException("GeneratedCode must be invalidated");

        if (invalidated.Any(id => id.StartsWith("arch:", StringComparison.Ordinal)))
            throw new InvalidOperationException("Architecture must not be invalidated (D3)");
    }

    private static async Task TestBugfixSkill_EmptyDiff_RejectedAsync()
    {
        using var db = CreateSqliteClient();
        const string tenantId = "_phase5-bugfix";
        const string projectId = "9202";

        await SeedMinimalIr3ChainAsync(db, tenantId, projectId);
        await ProjectAllEventsAsync(db);

        var skill = CreateBugfixSkill(db);
        var snapshot = await LoadSnapshotAsync(db, tenantId, projectId);
        var context = new SkillContext
        {
            RunId = "run-bf2",
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = 9202,
            UserRequirement = "noop",
            Snapshot = snapshot,
            Bugfix = new BugfixRunContext { FromSequence = 6, ToSequence = 7 },
        };

        try
        {
            await foreach (var _ in skill.ReasonAsync(context))
            {
            }

            throw new InvalidOperationException("Expected empty diff rejection");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("空 diff", StringComparison.Ordinal))
        {
            // expected
        }
    }

    private static async Task TestProjection_AffectedFragmentsMarkedAsync()
    {
        using var db = CreateSqliteClient();
        var projection = new IrProjectionEngine(db);
        const string tenantId = "_phase5-bugfix";
        const string projectId = "9203";
        const string fragmentId = $"codegen:{projectId}";

        await SeedMinimalIr3ChainAsync(db, tenantId, projectId);
        await ProjectAllEventsAsync(db);

        var payload = BugfixManifestBuilder.BuildAffectedFragmentsMarkedPayload(
            projectId,
            "run-proj",
            new IrDiffResult
            {
                Invalidated = [fragmentId],
                FromSequence = 6,
                ToSequence = 7,
            });

        await ProjectEventAsync(
            db,
            projection,
            tenantId,
            projectId,
            99,
            IrEventTypes.AffectedFragmentsMarked,
            "bugfix:9203",
            IrFragmentTypes.EventSpec,
            3,
            payload);

        var snap = await db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.FragmentId == fragmentId)
            .FirstAsync();

        if (snap.StabilityState != IrStabilityStates.InProgress)
            throw new InvalidOperationException($"Expected in-progress after mark, got {snap.StabilityState}");
    }

    private static BugfixSkillService CreateBugfixSkill(SqlSugarClient db)
    {
        using var loggerFactory = LoggerFactory.Create(static _ => { });
        return new BugfixSkillService(new IrDiffEngine(db), loggerFactory.CreateLogger<BugfixSkillService>());
    }

    private static async Task<IrSnapshot> LoadSnapshotAsync(SqlSugarClient db, string tenantId, string projectId)
    {
        var rows = await db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .ToListAsync();

        return new IrSnapshot
        {
            Fragments = rows.Select(r => new IrSnapshotFragment
            {
                FragmentId = r.FragmentId,
                FragmentType = r.FragmentType,
                StabilityState = r.StabilityState,
                Payload = r.IrContent,
            }).ToList(),
        };
    }

    private static async Task ProjectAllEventsAsync(SqlSugarClient db)
    {
        var projection = new IrProjectionEngine(db);
        var events = await db.Queryable<AiIrEventEntity>()
            .OrderBy(x => x.Sequence)
            .ToListAsync();

        foreach (var evt in events)
            await projection.ProjectEventAsync(evt);
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
        string payload)
    {
        var id = Guid.NewGuid().ToString("N");
        await db.Ado.ExecuteCommandAsync(
            "INSERT INTO ai_ir_events (F_Id,F_ProjectId,F_TenantId,F_EventType,F_FragmentId,F_FragmentType,F_FragmentVersion,F_Payload,F_SAStepName,F_Sequence,F_CreatedAt,F_IsRollback) VALUES (@id,@pid,@tid,@etype,@fid,@ftype,@fver,@payload,NULL,@seq,@at,0)",
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
                new SugarParameter("@seq", sequence),
                new SugarParameter("@at", DateTime.UtcNow.ToString("o")),
            });
    }

    private static async Task ProjectEventAsync(
        SqlSugarClient db,
        IrProjectionEngine projection,
        string tenantId,
        string projectId,
        int sequence,
        string eventType,
        string fragmentId,
        string fragmentType,
        int fragmentVersion,
        string payload)
    {
        var evt = new AiIrEventEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            TenantId = tenantId,
            EventType = eventType,
            FragmentId = fragmentId,
            FragmentType = fragmentType,
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
