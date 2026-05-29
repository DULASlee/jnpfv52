using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace JNPF.Xunit;

/// <summary>
/// 单元测试框架执行器
/// </summary>
public class XunitTestFrameworkExecutorWithAssemblyFixture : XunitTestFrameworkExecutor
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <param name="sourceInformationProvider"></param>
    /// <param name="diagnosticMessageSink"></param>
    public XunitTestFrameworkExecutorWithAssemblyFixture(AssemblyName assemblyName
        , ISourceInformationProvider sourceInformationProvider
        , IMessageSink diagnosticMessageSink)
        : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
    }

    /// <summary>
    /// 执行测试案例
    /// </summary>
    /// <param name="testCases"></param>
    /// <param name="executionMessageSink"></param>
    /// <param name="executionOptions"></param>
    protected async override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
    {
        using var assemblyRunner = new XunitTestAssemblyRunnerWithAssemblyFixture(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
        await assemblyRunner.RunAsync();
    }
}