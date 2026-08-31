using System.Reflection;

namespace JNPF.Runtime.Capability;

/// <summary>
/// 程序集标记：Section 9 Mode Governance Core 第一阶段产出物。
/// 当前实现为 Section 9 Capability Contract 子集，无 Runtime / Layer 0 依赖。
/// </summary>
internal static class RuntimeCapabilityAssembly
{
    public static Assembly Current => typeof(RuntimeCapabilityAssembly).Assembly;
}
