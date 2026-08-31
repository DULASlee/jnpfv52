using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace JNPF.Tests.Agent;

public sealed class GateBAndCTests
{
    private const string FlowCommentServicePath = @"D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow\Service\FlowCommentService.cs";
    private const string WorkFlowAssemblyPath = @"D:\JNPF-v52\backend\modularity\workflow\JNPF.WorkFlow\bin\Debug\net8.0\JNPF.WorkFlow.dll";

    [Fact]
    public void GateB_BuildListQuery_IsInternal_L2()
    {
        var assembly = Assembly.LoadFrom(WorkFlowAssemblyPath);
        var serviceType = assembly.GetType("JNPF.WorkFlow.Service.FlowCommentService")!;
        var method = serviceType.GetMethod("BuildListQuery", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        Assert.True(method!.IsAssembly, "BuildListQuery must be internal (IsAssembly=true)");
    }

    [Fact]
    public void GateB_GetList_BodyContainsNoQueryConstruction_L1_Roslyn()
    {
        var source = File.ReadAllText(FlowCommentServicePath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var getList = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "GetList");

        var invocations = getList.Body!.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => i.Expression.ToString())
            .ToList();

        // GetList must NOT contain SQL chain construction
        Assert.DoesNotContain(invocations, m => m.Contains("Queryable"));
        Assert.DoesNotContain(invocations, m => m.Contains("JoinQueryInfos"));
        Assert.DoesNotContain(invocations, m => m.Contains("OrderBy"));
        Assert.DoesNotContain(invocations, m => m.Contains("Select"));

        // GetList MUST call BuildListQuery
        Assert.Contains(invocations, m => m.Contains("BuildListQuery"));
    }

    [Fact]
    public void GateC_PublicApi_AllFiveMethods_L2()
    {
        var assembly = Assembly.LoadFrom(WorkFlowAssemblyPath);
        var serviceType = assembly.GetType("JNPF.WorkFlow.Service.FlowCommentService")!;
        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName).Select(m => m.Name).ToHashSet();
        Assert.Contains("GetList", methods);
        Assert.Contains("GetInfo", methods);
        Assert.Contains("Create", methods);
        Assert.Contains("Update", methods);
        Assert.Contains("Delete", methods);
    }

    [Fact]
    public void GateC_DI_TwoConstructorParameters_L2()
    {
        var assembly = Assembly.LoadFrom(WorkFlowAssemblyPath);
        var serviceType = assembly.GetType("JNPF.WorkFlow.Service.FlowCommentService")!;
        var ctor = serviceType.GetConstructors().Single();
        var parameters = ctor.GetParameters();
        Assert.Equal(2, parameters.Length);
        // ParameterType.Name for generic types includes "`1" arity suffix
        Assert.StartsWith("ISqlSugarRepository", parameters[0].ParameterType.Name);
        Assert.Equal("IUserManager", parameters[1].ParameterType.Name);
    }

    [Fact]
    public void GateC_HttpRouting_L2()
    {
        var assembly = Assembly.LoadFrom(WorkFlowAssemblyPath);
        var serviceType = assembly.GetType("JNPF.WorkFlow.Service.FlowCommentService")!;
        var getList = serviceType.GetMethod("GetList")!;
        var attrs = getList.GetCustomAttributes().Select(a => a.GetType().Name).ToList();
        Assert.Contains("HttpGetAttribute", attrs);
    }

    [Fact]
    public void GateC_SoftDelete_ThreeFiltersViaRoslyn_L1()
    {
        var source = File.ReadAllText(FlowCommentServicePath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();
        var deleteMarkRefs = root.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(b => b.Right.ToString().Contains("null")
                && b.Left.ToString().Contains("DeleteMark"))
            .Count();
        Assert.Equal(3, deleteMarkRefs);
    }

    [Fact]
    public void GateC_LifecycleCalls_L1_Roslyn()
    {
        var source = File.ReadAllText(FlowCommentServicePath);
        var tree = CSharpSyntaxTree.ParseText(source);
        Assert.Contains("CallEntityMethod(m => m.Creator())", tree.GetRoot().ToString());
        Assert.Contains("CallEntityMethod(m => m.LastModify())", tree.GetRoot().ToString());
        Assert.Contains("CallEntityMethod(m => m.Delete())", tree.GetRoot().ToString());
    }

    [Fact]
    public void GateC_Exception_OopsOhCalled_L1()
    {
        var source = File.ReadAllText(FlowCommentServicePath);
        var tree = CSharpSyntaxTree.ParseText(source);
        var count = System.Text.RegularExpressions.Regex.Matches(tree.GetRoot().ToString(), @"Oops\.Oh\(ErrorCode\.COM1000\)").Count;
        Assert.Equal(3, count);
    }
}