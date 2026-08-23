using System.Linq;
using System.Reflection;
using JNPF.VisualDev.Interfaces;
using Xunit;

namespace JNPF.Tests.VisualDev;

/// <summary>
/// M1 安全网 — IRunService 接口面契约测试（规格 4.2，契约 C-RS-IRunService@v0）.
/// 纪律（4.2.1 BR-2）：反射+字符串匹配，零 MVC 类型依赖.
/// 存量事实（2026-08-24 实测）：接口现有 18 成员（规格原载 17，偏差已上报待裁）；
/// WorkFlow 模块实测消费 5 成员（规格原载 7，同上）.
/// </summary>
public class RunServiceContractTests
{
    /// <summary>
    /// C-RS-IRunService@v0 存量 18 成员签名冻结——重构期接口面只增不改不删（S5 瘦身除外，升 @v1）.
    /// </summary>
    private static readonly string[] FrozenMembers =
    {
        nameof(IRunService.Create),
        nameof(IRunService.CreateHaveTableSql),
        nameof(IRunService.Update),
        nameof(IRunService.BatchUpdate),
        nameof(IRunService.UpdateHaveTableSql),
        nameof(IRunService.DelHaveTableInfo),
        nameof(IRunService.DelInteAssistant),
        nameof(IRunService.BatchDelHaveTableData),
        nameof(IRunService.GetListResult),
        nameof(IRunService.GetRelationFormList),
        nameof(IRunService.GetHaveTableInfo),
        nameof(IRunService.GetHaveTableInfoDetails),
        nameof(IRunService.GenerateFeilds),
        nameof(IRunService.GetDbLink),
        nameof(IRunService.SaveFlowFormData),
        nameof(IRunService.GetFlowFormDataDetails),
        nameof(IRunService.SaveDataToDataByFId),
        nameof(IRunService.GetVisualDevModelDataConfig),
    };

    /// <summary>
    /// WorkFlow 模块消费面（全仓调用点实测：FlowTaskManager/FlowTaskOtherUtil/FlowFormService）.
    /// 瘦身后必须保留在接口面，nameof 守护.
    /// </summary>
    private static readonly string[] WorkFlowConsumedMembers =
    {
        nameof(IRunService.SaveFlowFormData),
        nameof(IRunService.GetFlowFormDataDetails),
        nameof(IRunService.SaveDataToDataByFId),
        nameof(IRunService.GetVisualDevModelDataConfig),
        nameof(IRunService.GetDbLink),
    };

    [Fact]
    public void InterfaceSurface_MatchesFrozenBaseline()
    {
        var actual = typeof(IRunService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToArray();

        var expected = FrozenMembers.OrderBy(n => n).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InterfaceSurface_MemberCount_IsFrozenAt18()
    {
        var count = typeof(IRunService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Length;

        Assert.Equal(18, count);
    }

    [Fact]
    public void WorkFlowConsumedMembers_AllPresentOnInterface()
    {
        var actual = typeof(IRunService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();

        foreach (var member in WorkFlowConsumedMembers)
        {
            Assert.True(actual.Contains(member), $"WorkFlow 消费成员缺失：{member}");
        }
    }
}
