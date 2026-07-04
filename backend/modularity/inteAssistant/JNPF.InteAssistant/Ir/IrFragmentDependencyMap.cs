using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Ir;

/// <summary>
/// 阶段五 P5-B01 — IR 片段变更向下游传播表（对齐文档 13 §6 D3：字段级 Bug 不重算 arch/ui）。
/// </summary>
public static class IrFragmentDependencyMap
{
    public static IReadOnlyList<string> GetDownstreamFragmentTypes(string fragmentType) =>
        fragmentType switch
        {
            IrFragmentTypes.EventSpec =>
            [
                IrFragmentTypes.DDL,
                IrFragmentTypes.GeneratedCode,
                IrFragmentTypes.TestSuite,
            ],
            IrFragmentTypes.DDL =>
            [
                IrFragmentTypes.GeneratedCode,
                IrFragmentTypes.TestSuite,
            ],
            IrFragmentTypes.Architecture =>
            [
                IrFragmentTypes.DDL,
                IrFragmentTypes.GeneratedCode,
                IrFragmentTypes.TestSuite,
            ],
            IrFragmentTypes.GeneratedCode => [IrFragmentTypes.TestSuite],
            IrFragmentTypes.FormPageIR => [],
            IrFragmentTypes.SystemDesign => [],
            _ => Array.Empty<string>(),
        };
}
