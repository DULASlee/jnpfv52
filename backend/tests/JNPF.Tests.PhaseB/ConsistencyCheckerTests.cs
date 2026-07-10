using System.Data;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Sa;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// 一致性检查器 xUnit 测试（28 号 §5）。
/// 覆盖：4 条规则 DB 路径 + 回退路径 + sa_consistency 写入（含 PASSED 哨兵）。
/// </summary>
public static class ConsistencyCheckerTests
{
    public static async Task RunAllAsync()
    {
        await T1_Rule1_DataEntity_Conflict_SqlSuccess();
        await T2_Rule1_DataEntity_NoConflict();
        await T3_Rule1_DataEntity_SqlFails_FallbackToProjection();
        await T4_Rule2_Role_BothTriggerAndView();
        await T5_Rule2_Role_NoConflict();
        await T6_Rule3_FlowClosure_OrphanOutput();
        await T7_Rule4_Assumptions_Unconfirmed();
        await T8_PersistFindings_PASSED_Sentinel_WhenEmpty();
        await T9_PersistFindings_WithFindings();
    }

    private static void Assert(bool condition, string msg)
    {
        if (!condition) throw new Exception(msg);
    }

    /// <summary>创建 SQLite :memory: 客户端，建 sa_consistency 表。</summary>
    private static ISqlSugarClient CreateDb()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = conn.ConnectionString,
            DbType = SqlSugar.DbType.Sqlite,
            IsAutoCloseConnection = false,
        });
        db.Ado.ExecuteCommand("""
            CREATE TABLE sa_consistency (
                F_Id TEXT PRIMARY KEY,
                F_TenantId TEXT,
                F_ProjectId TEXT,
                F_PIPELINE_ID TEXT,
                F_RoundNumber INTEGER,
                F_CheckType TEXT,
                F_ConflictsJson TEXT,
                F_AssumptionsJson TEXT,
                F_GapsJson TEXT,
                F_Severity TEXT,
                F_CreatedAt TEXT
            )
            """);
        return db;
    }

    private static PipelineTriple Triple => new("t1", "p1", 100);

    // ── T1: 规则 1 — 类型冲突 ──

    private static async Task T1_Rule1_DataEntity_Conflict_SqlSuccess()
    {
        using var db = CreateDb();
        // 预建 sa_entity_fields 表模拟物化后 VIEW
        db.Ado.ExecuteCommand("""
            CREATE TABLE sa_entity_fields (
                TenantId TEXT, ProjectId TEXT, PipelineId TEXT,
                EntityName TEXT, FieldName TEXT, SqlType TEXT
            )
            """);
        db.Ado.ExecuteCommand(
            "INSERT INTO sa_entity_fields VALUES ('t1','p1','100','User','Age','int')");
        db.Ado.ExecuteCommand(
            "INSERT INTO sa_entity_fields VALUES ('t1','p1','100','User','Age','string')");

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>());
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var findings = await checker.CheckAsync(Triple, compile, fields, 3);
        Assert(findings.Any(f => f.CheckType == "DATA_ENTITY" && f.Severity == "CRITICAL"),
            "应有类型冲突 CRITICAL 发现");
    }

    // ── T2: 规则 1 — 无冲突 ──

    private static async Task T2_Rule1_DataEntity_NoConflict()
    {
        using var db = CreateDb();
        db.Ado.ExecuteCommand("""
            CREATE TABLE sa_entity_fields (
                TenantId TEXT, ProjectId TEXT, PipelineId TEXT,
                EntityName TEXT, FieldName TEXT, SqlType TEXT
            )
            """);
        db.Ado.ExecuteCommand(
            "INSERT INTO sa_entity_fields VALUES ('t1','p1','100','User','Name','string')");

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>());
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var findings = await checker.CheckAsync(Triple, compile, fields, 2);
        Assert(!findings.Any(f => f.CheckType == "DATA_ENTITY" && f.Severity == "CRITICAL"),
            "无类型冲突时不应有 CRITICAL");
    }

    // ── T3: 规则 1 — DB 失败回退 ──

    private static async Task T3_Rule1_DataEntity_SqlFails_FallbackToProjection()
    {
        using var db = CreateDb();
        // 不建 sa_entity_fields 表 → SQL 查询失败 → 回退内存投影
        var fields = new EntityDesignProjection
        {
            Fields = new List<EntityFieldDesign>
            {
                new() { EntityName = "User", FieldName = "Age", SqlType = "int" },
                new() { EntityName = "User", FieldName = "Age", SqlType = "string" },
            },
        };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>());
        var findings = await checker.CheckAsync(Triple, compile, fields, 3);

        Assert(findings.Any(f => f.CheckType == "DATA_ENTITY" && f.Message.Contains("age")),
            "回退投影应检测到 User.Age 类型冲突");
    }

    // ── T4: 规则 2 — trigger+view 权限冲突 ──

    private static async Task T4_Rule2_Role_BothTriggerAndView()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>(),
            roleMatrix: new PreAnalysisRoleMatrix
            {
                Matrix = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
                {
                    ["BE-001"] = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["admin"] = new List<string> { "trigger", "view" },
                    },
                },
            });
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var findings = await checker.CheckAsync(Triple, compile, fields, 2);

        Assert(findings.Any(f => f.CheckType == "ROLE" && f.Severity == "WARNING"),
            "admin 同时有 trigger+view 应产生 WARNING");
    }

    // ── T5: 规则 2 — 无权限冲突 ──

    private static async Task T5_Rule2_Role_NoConflict()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>(),
            roleMatrix: new PreAnalysisRoleMatrix
            {
                Matrix = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>
                {
                    ["BE-001"] = new Dictionary<string, IReadOnlyList<string>>
                    {
                        ["employee"] = new List<string> { "trigger" },
                        ["manager"] = new List<string> { "view" },
                    },
                },
            });
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var findings = await checker.CheckAsync(Triple, compile, fields, 1);

        Assert(!findings.Any(f => f.CheckType == "ROLE"),
            $"无权限重叠时不应有 ROLE 发现，实际有 {findings.Count(f => f.CheckType == "ROLE")} 条");
    }

    // ── T6: 规则 3 — 孤立依赖 ──

    private static async Task T6_Rule3_FlowClosure_OrphanOutput()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(new[]
        {
            new PreAnalysisBusinessEvent { EventId = "BE-001", EventName = "创建订单" },
            new PreAnalysisBusinessEvent { EventId = "BE-002", EventName = "审批", DependsOn = new[] { "BE-099" } },
        });
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var findings = await checker.CheckAsync(Triple, compile, fields, 3);

        Assert(findings.Any(f => f.CheckType == "FLOW_CLOSURE"),
            "BE-002 依赖不存在的 BE-099 应有 FLOW_CLOSURE 发现");
    }

    // ── T7: 规则 4 — 低置信度假设 ──

    private static async Task T7_Rule4_Assumptions_Unconfirmed()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>(), new[]
        {
            new Assumption("BE-003", "PSpec", "用户权限规则不明", 0.4m),
            new Assumption("BE-003", "DecisionTable", "边界条件待确认", 0.5m),
        });
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        var findings = await checker.CheckAsync(Triple, compile, fields, 3);

        Assert(findings.Any(f => f.CheckType == "ASSUMPTION"),
            $"低置信度假设应产生 ASSUMPTION 发现，实际 {findings.Count} 条");
    }

    // ── T8: 零发现 → PASSED 哨兵 ──

    private static async Task T8_PersistFindings_PASSED_Sentinel_WhenEmpty()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>());
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        await checker.CheckAsync(Triple, compile, fields, 1);

        var rows = db.Ado.SqlQuery<dynamic>("SELECT F_CheckType FROM sa_consistency");
        Assert(rows.Any(r => (string)r.F_CheckType == "PASSED"),
            "零发现时必须有 PASSED 哨兵行");
    }

    // ── T9: 有发现 → 写分组行 ──

    private static async Task T9_PersistFindings_WithFindings()
    {
        using var db = CreateDb();
        var compile = BuildCompileResult(Array.Empty<PreAnalysisBusinessEvent>(), new[]
        {
            new Assumption("BE-004", "PSpec", "xxx", 0.3m),
        });
        var fields = new EntityDesignProjection { Fields = new List<EntityFieldDesign>() };

        var checker = new ConsistencyChecker(db, NullLogger<ConsistencyChecker>.Instance);
        await checker.CheckAsync(Triple, compile, fields, 2);

        var rows = db.Ado.SqlQuery<dynamic>("SELECT COUNT(1) AS Cnt FROM sa_consistency WHERE F_CheckType != 'PASSED'");
        Assert(((long)rows[0].Cnt) > 0, "有发现时应写入非 PASSED 行");
    }

    // ── helpers ──

    private static SaNineViewCompileResult BuildCompileResult(
        PreAnalysisBusinessEvent[] events, Assumption[]? assumptions = null,
        PreAnalysisRoleMatrix? roleMatrix = null)
    {
        return new SaNineViewCompileResult
        {
            Source = new PreAnalysisModel
            {
                BusinessEvents = events.ToList(),
                RoleMatrix = roleMatrix,
            },
            ProjectSteps = new Dictionary<string, object>(StringComparer.Ordinal),
            EventResults = new List<SaEventResult>(),
            Assumptions = (assumptions ?? Array.Empty<Assumption>()).ToList(),
        };
    }
}
