using System.Runtime.CompilerServices;

// 允许测试程序集访问 internal 类型。
// 用途：RuntimeSession 构造函数仅供测试和 RuntimeLifecycleController 使用。
[assembly: InternalsVisibleTo("JNPF.Tests.Runtime.Core")]
