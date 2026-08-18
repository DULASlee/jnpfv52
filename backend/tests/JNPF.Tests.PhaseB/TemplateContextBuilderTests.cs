using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;

namespace JNPF.Tests.PhaseB;

/// <summary>
/// TemplateContextBuilder 严格模式负面测试（A5 追加约束）。
/// </summary>
public static class TemplateContextBuilderTests
{
    public static void RunAll()
    {
        TestMissingDdlFragment_ThrowsBah();
        TestEmptyDdlText_ThrowsBah();
        TestMissingArchitectureModule_ThrowsBahWithoutOverride();
        TestStringArchitectureModules_ResolvesNameSpace();
        TestNestedFormPageFields_BuildsColumns();
        Console.WriteLine("[A5] TemplateContextBuilder strict negative tests passed.");
    }

    private static void TestMissingDdlFragment_ThrowsBah()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                Fragment(IrFragmentTypes.Skeleton, stable: true),
                Fragment(IrFragmentTypes.Architecture, stable: true),
                Fragment(IrFragmentTypes.FormPageIR, stable: true),
            },
        };

        AssertBah(() => Build(snapshot), "IR2_DDL");
    }

    private static void TestEmptyDdlText_ThrowsBah()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                Fragment(IrFragmentTypes.Skeleton, stable: true),
                Fragment(IrFragmentTypes.Architecture, stable: true),
                Fragment(IrFragmentTypes.FormPageIR, stable: true),
                new IrSnapshotFragment
                {
                    FragmentId = "ddl:empty",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"tableNames":["T"],"ddl":""}""",
                },
            },
        };

        AssertBah(() => Build(snapshot), "无字段定义");
    }

    private static void TestMissingArchitectureModule_ThrowsBahWithoutOverride()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                Fragment(IrFragmentTypes.Skeleton, stable: true),
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:no-module",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"modules":[]}""",
                },
                Fragment(IrFragmentTypes.DDL, stable: true, withDdl: true),
                Fragment(IrFragmentTypes.FormPageIR, stable: true),
            },
        };

        AssertBah(() => Build(snapshot), "NameSpace");
    }

    private static void TestStringArchitectureModules_ResolvesNameSpace()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                Fragment(IrFragmentTypes.Skeleton, stable: true),
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:string-modules",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"modules":["leave-application","approval"]}""",
                },
                Fragment(IrFragmentTypes.DDL, stable: true, withDdl: true),
                Fragment(IrFragmentTypes.FormPageIR, stable: true),
            },
        };

        var ctx = Build(snapshot);
        if (!string.Equals(ctx.NameSpace, "LeaveApplication", StringComparison.Ordinal))
            throw new InvalidOperationException($"expected LeaveApplication, got {ctx.NameSpace}");
    }

    private static void TestNestedFormPageFields_BuildsColumns()
    {
        var snapshot = new IrSnapshot
        {
            Fragments = new[]
            {
                Fragment(IrFragmentTypes.Skeleton, stable: true),
                new IrSnapshotFragment
                {
                    FragmentId = "architecture:string-modules",
                    FragmentType = IrFragmentTypes.Architecture,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"modules":["leave-application"]}""",
                },
                new IrSnapshotFragment
                {
                    FragmentId = "ddl:markdown",
                    FragmentType = IrFragmentTypes.DDL,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"tableNames":["LeaveRequest"],"ddl":"```sql\nCREATE TABLE LeaveRequest (RequestID INT PRIMARY KEY, Reason NVARCHAR(200) NULL, Days INT NOT NULL);\n```"}""",
                },
                new IrSnapshotFragment
                {
                    FragmentId = "form:pages",
                    FragmentType = IrFragmentTypes.FormPageIR,
                    StabilityState = IrStabilityStates.Stable,
                    Payload = """{"pages":[{"fields":[{"id":"reason","label":"事由"},{"id":"days","label":"天数"}]}]}""",
                },
            },
        };

        var ctx = Build(snapshot);
        // P9-S4：列定义来自 DDL（确定性投影），form page 字段不再作为列源
        if (ctx.TableField.Count < 3)
            throw new InvalidOperationException($"expected >=3 columns from DDL projection, got {ctx.TableField.Count}");
    }

    private static Ir2CodegenContext Build(IrSnapshot snapshot)
    {
        // P9-S4：Build 需显式传入 Projection（消费端契约主权）
        var projection = EntityDesignProjector.Project(snapshot, new EntityDesignProjectionOptions
        {
            ProjectId = "neg-test",
            TenantId = "000000",
            PipelineId = "0",
        });

        var builder = new TemplateContextBuilder();
        return builder.Build(snapshot, new Ir2CodegenBuildOptions
        {
            ProjectId = "neg-test",
            TenantId = "000000",
            SampleId = "neg-test",
            StrictMode = true,
            Projection = projection,
        });
    }

    private static IrSnapshotFragment Fragment(string type, bool stable, bool withDdl = false)
    {
        var payload = type switch
        {
            IrFragmentTypes.Skeleton => """{"entityDrafts":[{"entityName":"LeaveRequest","tableName":"OA_LEAVE_REQUEST"}]}""",
            IrFragmentTypes.Architecture => """{"modules":[{"name":"OaLeave","layer":"application"}]}""",
            IrFragmentTypes.FormPageIR => """{"fields":[{"fieldId":"reason","label":"事由","component":"Input"}]}""",
            IrFragmentTypes.DDL when withDdl =>
                """{"tableNames":["OA_LEAVE_REQUEST"],"ddl":"CREATE TABLE [dbo].[OA_LEAVE_REQUEST] ([F_Id] NVARCHAR(50) NOT NULL PRIMARY KEY, [F_Reason] NVARCHAR(200) NULL);"}""",
            _ => "{}",
        };

        return new IrSnapshotFragment
        {
            FragmentId = $"{type}:test",
            FragmentType = type,
            StabilityState = stable ? IrStabilityStates.Stable : IrStabilityStates.Draft,
            Payload = payload,
        };
    }

    private static void AssertBah(Action action, string expectedSubstring)
    {
        Exception? caught = null;
        try
        {
            action();
        }
        catch (TemplateContextBuildException ex)
        {
            caught = ex;
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        if (caught == null)
            throw new InvalidOperationException($"预期 Oops.Bah 但未抛出（期望包含: {expectedSubstring}）");

        var message = caught.Message;
        if (caught.InnerException != null)
            message += " " + caught.InnerException.Message;

        if (!message.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Bah 消息不匹配。期望包含 '{expectedSubstring}'，实际: {message}", caught);
    }
}
