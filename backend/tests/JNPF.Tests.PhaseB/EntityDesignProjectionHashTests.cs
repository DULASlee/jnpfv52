using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using Xunit;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// S5.3 投影 hash 回归测试。
///
/// 验证 EntityDesignProjector 的确定性：
///   ① 相同 IR snapshot 投影两次，hash 必须一致（确定性）
///   ② hash 仅依赖实体字段内容，与三元组无关（内容驱动）
///   ③ 字段级投影数据在增量场景下保持一致（增量 vs 全量等价）
/// </summary>
public class EntityDesignProjectionHashTests
{
    /// <summary>
    /// T1 — 确定性：同一 snapshot + 同一 options 投影两次，ProjectionHash 必须相同。
    /// 同时验证每行的 ProjectionHash 与整体 ProjectionHash 一致。
    /// </summary>
    [Fact]
    public void T1_SameSnapshotTwice_SameHash()
    {
        var snapshot = CreateLeaveRequestSnapshot();
        var options = new EntityDesignProjectionOptions
        {
            TenantId = "test-tenant",
            ProjectId = "test-project",
            PipelineId = "1",
        };

        // Act: project twice independently
        var p1 = EntityDesignProjector.Project(snapshot, options);
        var p2 = EntityDesignProjector.Project(snapshot, options);

        // Assert: overall hash deterministic
        Assert.Equal(p1.ProjectionHash, p2.ProjectionHash);
        Assert.NotEmpty(p1.ProjectionHash);
        Assert.Equal(64, p1.ProjectionHash.Length); // SHA-256 hex = 64 chars

        // Assert: per-row hash consistent with overall hash
        Assert.All(p1.Fields, f => Assert.Equal(p1.ProjectionHash, f.ProjectionHash));

        // Assert: field count equal
        Assert.Equal(p1.Fields.Count, p2.Fields.Count);
    }

    /// <summary>
    /// T2 — 内容驱动：相同实体数据、不同三元组 → hash 一致。
    /// hash 仅依赖实体字段内容（EntityName/TableName/FieldName/类型/约束等），
    /// 三元组 (TenantId/ProjectId/PipelineId) 不参与 hash 计算。
    /// </summary>
    [Fact]
    public void T2_HashIsContentBased_IndependentOfTripleKey()
    {
        var snapshot = CreateLeaveRequestSnapshot();

        var optionsA = new EntityDesignProjectionOptions
        {
            TenantId = "tenant-alpha",
            ProjectId = "project-alpha",
            PipelineId = "100",
        };
        var optionsB = new EntityDesignProjectionOptions
        {
            TenantId = "tenant-beta",
            ProjectId = "project-beta",
            PipelineId = "200",
        };

        var pA = EntityDesignProjector.Project(snapshot, optionsA);
        var pB = EntityDesignProjector.Project(snapshot, optionsB);

        // Hash 相同（三元组不在 hash 输入中）
        Assert.Equal(pA.ProjectionHash, pB.ProjectionHash);

        // 但每行的 TenantId/ProjectId/PipelineId 携带各自的三元组
        Assert.All(pA.Fields, f => Assert.Equal("tenant-alpha", f.TenantId));
        Assert.All(pA.Fields, f => Assert.Equal("project-alpha", f.ProjectId));
        Assert.All(pA.Fields, f => Assert.Equal("100", f.PipelineId));

        Assert.All(pB.Fields, f => Assert.Equal("tenant-beta", f.TenantId));
        Assert.All(pB.Fields, f => Assert.Equal("project-beta", f.ProjectId));
        Assert.All(pB.Fields, f => Assert.Equal("200", f.PipelineId));
    }

    /// <summary>
    /// T3 — 增量等价：在已有实体基础上新增实体，原有实体的字段投影数据不变。
    /// 验证 (N 实体投影) 的字段 ⊆ (N+1 实体投影) 的字段。
    /// </summary>
    [Fact]
    public void T3_IncrementalProjection_PreservesExistingFields()
    {
        var options = new EntityDesignProjectionOptions
        {
            TenantId = "test",
            ProjectId = "test",
            PipelineId = "1",
        };

        // 单实体快照（LeaveRequest only）
        var singleSnapshot = CreateLeaveRequestSnapshot();
        var singleProjection = EntityDesignProjector.Project(singleSnapshot, options);

        // 双实体快照（LeaveRequest + Approval）
        var multiSnapshot = CreateMultiEntitySnapshot();
        var multiProjection = EntityDesignProjector.Project(multiSnapshot, options);

        // 双实体投影应包含单实体投影的所有字段
        Assert.True(multiProjection.Fields.Count > singleProjection.Fields.Count,
            "多实体投影应比单实体投影含更多字段");

        foreach (var singleField in singleProjection.Fields)
        {
            var match = multiProjection.Fields.FirstOrDefault(f =>
                f.EntityName == singleField.EntityName
                && f.FieldName == singleField.FieldName);

            Assert.NotNull(match);
            Assert.Equal(singleField.TableName, match!.TableName);
            Assert.Equal(singleField.CSharpType, match.CSharpType);
            Assert.Equal(singleField.SqlType, match.SqlType);
            Assert.Equal(singleField.IsPrimaryKey, match.IsPrimaryKey);
            Assert.Equal(singleField.IsRequired, match.IsRequired);
        }

        // Hash 不同（因为整体内容变了）
        Assert.NotEqual(singleProjection.ProjectionHash, multiProjection.ProjectionHash);
    }

    /// <summary>
    /// T4 — 空骨架：无 entityDrafts 时返回空投影，hash 来自空列表。
    /// </summary>
    [Fact]
    public void T4_EmptySkeleton_ReturnsEmptyProjectionWithHash()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "sk-empty",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[]}""",
                },
            },
        };
        var options = new EntityDesignProjectionOptions
        {
            TenantId = "test",
            ProjectId = "test",
            PipelineId = "1",
        };

        var projection = EntityDesignProjector.Project(snapshot, options);

        Assert.Empty(projection.Fields);
        Assert.NotEmpty(projection.ProjectionHash); // hash of empty list is still a valid hash
        Assert.Equal(64, projection.ProjectionHash.Length);
    }

    /// <summary>
    /// T5 — 字段排序独立性：无论 Skeleton 字段声明顺序如何，投影输出一致排序
    /// （按 EntityName → IsPrimaryKey desc → FieldName），hash 一致。
    /// </summary>
    [Fact]
    public void T5_FieldOrderIndependence_SameHash()
    {
        var options = new EntityDesignProjectionOptions
        {
            TenantId = "test",
            ProjectId = "test",
            PipelineId = "1",
        };

        // 顺序 A：id 在前
        var snapshotA = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "sk-a",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[{"entityName":"Leave","tableName":"OA_LEAVE","fields":[{"name":"id","type":"string","primaryKey":true},{"name":"reason","type":"string"},{"name":"status","type":"string"}]}]}""",
                },
            },
        };

        // 顺序 B：id 在中间
        var snapshotB = new IrSnapshot
        {
            Fragments = new[]
            {
                new IrSnapshotFragment
                {
                    FragmentId = "sk-b",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[{"entityName":"Leave","tableName":"OA_LEAVE","fields":[{"name":"status","type":"string"},{"name":"id","type":"string","primaryKey":true},{"name":"reason","type":"string"}]}]}""",
                },
            },
        };

        var pA = EntityDesignProjector.Project(snapshotA, options);
        var pB = EntityDesignProjector.Project(snapshotB, options);

        // Hash 一致（排序归一化）
        Assert.Equal(pA.ProjectionHash, pB.ProjectionHash);
        Assert.Equal(pA.Fields.Count, pB.Fields.Count);

        // 主键字段应排在首位（排序规则：IsPrimaryKey desc）
        Assert.True(pA.Fields[0].IsPrimaryKey);
        Assert.Equal("id", pA.Fields[0].FieldName);
    }

    // ═══════════════════════════════════════════════════════════════
    // Test fixtures
    // ═══════════════════════════════════════════════════════════════

    private static IrSnapshot CreateLeaveRequestSnapshot()
    {
        return new IrSnapshot
        {
            Fragments = new IrSnapshotFragment[]
            {
                new()
                {
                    FragmentId = "sk-1",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[{"entityName":"LeaveRequest","tableName":"OA_LEAVE_REQUEST","fields":[{"name":"id","type":"string","primaryKey":true},{"name":"leaveType","type":"string"},{"name":"startDate","type":"DateTime"},{"name":"endDate","type":"DateTime"},{"name":"reason","type":"string"}]}]}""",
                },
                new()
                {
                    FragmentId = "ddl-1",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"tables":[{"entityName":"LeaveRequest","tableName":"OA_LEAVE_REQUEST","columns":[{"name":"F_Id","dataType":"NVARCHAR(50)","isPrimaryKey":true,"isNullable":false},{"name":"F_LeaveType","dataType":"NVARCHAR(50)","isNullable":true},{"name":"F_StartDate","dataType":"DATETIME","isNullable":true},{"name":"F_EndDate","dataType":"DATETIME","isNullable":true},{"name":"F_Reason","dataType":"NVARCHAR(500)","isNullable":true}]}]}""",
                },
            },
        };
    }

    /// <summary>双实体快照：LeaveRequest + ApprovalRecord</summary>
    private static IrSnapshot CreateMultiEntitySnapshot()
    {
        return new IrSnapshot
        {
            Fragments = new IrSnapshotFragment[]
            {
                new()
                {
                    FragmentId = "sk-2",
                    FragmentType = IrFragmentTypes.Skeleton,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"entityDrafts":[{"entityName":"LeaveRequest","tableName":"OA_LEAVE_REQUEST","fields":[{"name":"id","type":"string","primaryKey":true},{"name":"leaveType","type":"string"},{"name":"startDate","type":"DateTime"},{"name":"endDate","type":"DateTime"},{"name":"reason","type":"string"}]},{"entityName":"ApprovalRecord","tableName":"OA_APPROVAL_RECORD","fields":[{"name":"id","type":"string","primaryKey":true},{"name":"leaveId","type":"string","references":"LeaveRequest.id"},{"name":"approverId","type":"string"},{"name":"status","type":"string"},{"name":"comment","type":"string"}]}]}""",
                },
                new()
                {
                    FragmentId = "ddl-2",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"tables":[{"entityName":"LeaveRequest","tableName":"OA_LEAVE_REQUEST","columns":[{"name":"F_Id","dataType":"NVARCHAR(50)","isPrimaryKey":true,"isNullable":false},{"name":"F_LeaveType","dataType":"NVARCHAR(50)","isNullable":true},{"name":"F_StartDate","dataType":"DATETIME","isNullable":true},{"name":"F_EndDate","dataType":"DATETIME","isNullable":true},{"name":"F_Reason","dataType":"NVARCHAR(500)","isNullable":true}]},{"entityName":"ApprovalRecord","tableName":"OA_APPROVAL_RECORD","columns":[{"name":"F_Id","dataType":"NVARCHAR(50)","isPrimaryKey":true,"isNullable":false},{"name":"F_LeaveId","dataType":"NVARCHAR(50)","isNullable":true},{"name":"F_ApproverId","dataType":"NVARCHAR(50)","isNullable":true},{"name":"F_Status","dataType":"NVARCHAR(50)","isNullable":true},{"name":"F_Comment","dataType":"NVARCHAR(500)","isNullable":true}],"foreignKeys":[{"columnName":"F_LeaveId","referencesTable":"OA_LEAVE_REQUEST","referencesColumn":"F_Id"}]}]}""",
                },
            },
        };
    }
}
