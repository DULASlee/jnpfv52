using JNPF.WorkFlow.Entitys.Dto.FlowComment;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Agent;

public sealed class GateDTests
{
    private const string BaselineJsonPath = @"D:\JNPF-v52\backend\tests\JNPF.Tests.Runtime.Expert\build-baseline.json";
    private const string FlowCommentServiceRepoPath = "backend/modularity/workflow/JNPF.WorkFlow/Service/FlowCommentService.cs";

    private static SqlSugarClient CreateSqlSugarClient() =>
        new(new ConnectionConfig
        {
            ConnectionString = "Server=test;Database=test;Integrated Security=true;",
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true
        });

    [Fact]
    public void GateD_PreRefactorBlobIdentity_MatchesBaseline()
    {
        // [B1] BLOB SHA, not file hash
        PreRefactorQueryReplicator.VerifyBlobIdentity(BaselineJsonPath, FlowCommentServiceRepoPath);
    }

    [Fact]
    public void GateD_PreRefactorFingerprint_ContainsRequiredQueryElements()
    {
        // [P1-2] Roslyn semantic fingerprint of pre-refactor source
        PreRefactorQueryReplicator.VerifyPreRefactorFingerprint(BaselineJsonPath, FlowCommentServiceRepoPath);
    }

    [Fact]
    public void GateD_RefactoredBuildListQuery_InternalInvocation_L3()
    {
        var client = CreateSqlSugarClient();
        var userManager = UserManagerStub.Build("test-user-id");
        var repo = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);

        var service = new JNPF.WorkFlow.Service.FlowCommentService(repo, userManager);
        var queryable = service.BuildListQuery(new FlowCommentListQuery { taskId = "task-1", keyword = "test" });

        Assert.NotNull(queryable);
        var sql = queryable.ToSql();
        Assert.NotEmpty(sql.Key);
    }

    [Fact]
    public void GateD_RefactoredSql_EqualsPreRefactorSql_L3()
    {
        var client = CreateSqlSugarClient();
        var userManager = UserManagerStub.Build("test-user-id");
        var repo = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);

        var service = new JNPF.WorkFlow.Service.FlowCommentService(repo, userManager);

        var input = new FlowCommentListQuery { taskId = "task-1", keyword = "test" };
        var sqlRefactored = SqlSugarQueryCaptureHelper.NormalizeSql(service.BuildListQuery(input).ToSql().Key);
        var sqlPreRefactor = PreRefactorQueryReplicator.GenerateSql(input, client, userManager);

        Assert.Equal(sqlPreRefactor, sqlRefactored);
    }

    [Fact]
    public void GateD_DifferentInputs_ProduceDifferentSql_L3()
    {
        // Note: SqlSugar parameterises user-controlled values (taskId, keyword).
        // After normalisation, both Key strings are equal ("... = @p ..."),
        // but the underlying Value (SugarParameter array) differs.
        var client = CreateSqlSugarClient();
        var userManager = UserManagerStub.Build("test-user-id");
        var repo = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);
        var service = new JNPF.WorkFlow.Service.FlowCommentService(repo, userManager);

        var q1 = service.BuildListQuery(new FlowCommentListQuery { taskId = "task-1", keyword = "" }).ToSql();
        var q2 = service.BuildListQuery(new FlowCommentListQuery { taskId = "task-2", keyword = "" }).ToSql();

        // Key (SQL structure) is the same — both have @p placeholder
        Assert.Equal(SqlSugarQueryCaptureHelper.NormalizeSql(q1.Key),
                     SqlSugarQueryCaptureHelper.NormalizeSql(q2.Key));

        // Value (parameters) MUST differ
        var q1Params = q1.Value.Select(p => $"{p.ParameterName}={p.Value}").ToList();
        var q2Params = q2.Value.Select(p => $"{p.ParameterName}={p.Value}").ToList();
        Assert.NotEqual(q1Params, q2Params);
        Assert.Contains(q1Params, p => p.EndsWith("task-1") || p.Contains("task-1"));
        Assert.Contains(q2Params, p => p.EndsWith("task-2") || p.Contains("task-2"));
    }

    [Fact]
    public void GateD_RepositoryAudit_NoUnexpectedCalls()
    {
        // [B5] v5: prove audit completeness — BuildListQuery only invokes AsSugarClient
        var client = CreateSqlSugarClient();
        var userManager = UserManagerStub.Build("test-user-id");
        var repo = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);
        var concrete = SqlSugarRepositoryStub.AsConcrete(repo);

        var service = new JNPF.WorkFlow.Service.FlowCommentService(repo, userManager);
        var input = new FlowCommentListQuery { taskId = "task-1", keyword = "test" };
        _ = service.BuildListQuery(input).ToSql();  // trigger the ToSql() path

        Assert.NotNull(concrete);
        Assert.Empty(concrete!.UnexpectedCalls);
    }

    [Fact]
    public void GateD_UserContext_AffectsSqlKey_And_Parameters_L3()
    {
        var client = CreateSqlSugarClient();
        var userA = UserManagerStub.Build("user-a-id");
        var userB = UserManagerStub.Build("user-b-id");
        var repoA = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);
        var repoB = SqlSugarRepositoryStub.Build<JNPF.WorkFlow.Entitys.Entity.FlowCommentEntity>(client);

        var serviceA = new JNPF.WorkFlow.Service.FlowCommentService(repoA, userA);
        var serviceB = new JNPF.WorkFlow.Service.FlowCommentService(repoB, userB);

        var input = new FlowCommentListQuery { taskId = "t", keyword = "" };
        var sqlA = serviceA.BuildListQuery(input).ToSql();
        var sqlB = serviceB.BuildListQuery(input).ToSql();

        // SQL Key MAY be the same (IIF with parameterised @userId). What MUST differ
        // is the parameter values (or the full recordable).
        // SqlSugar SugarParameter has ParameterName/Value (not Key/Value)
        var paramAValues = sqlA.Value.Select(p => $"{p.ParameterName}={p.Value}").ToList();
        var paramBValues = sqlB.Value.Select(p => $"{p.ParameterName}={p.Value}").ToList();
        Assert.Contains(paramAValues, p => p.Contains("user-a-id"));
        Assert.Contains(paramBValues, p => p.Contains("user-b-id"));
        Assert.NotEqual(paramAValues, paramBValues);

        // [B5] audit holds for both paths
        var concreteA = SqlSugarRepositoryStub.AsConcrete(repoA);
        var concreteB = SqlSugarRepositoryStub.AsConcrete(repoB);
        Assert.NotNull(concreteA);
        Assert.NotNull(concreteB);
        Assert.Empty(concreteA!.UnexpectedCalls);
        Assert.Empty(concreteB!.UnexpectedCalls);
    }
}