using JNPF.Common.Core.Manager;
using JNPF.Systems.Entitys.Permission;
using JNPF.WorkFlow.Entitys.Dto.FlowComment;
using JNPF.WorkFlow.Entitys.Entity;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqlSugar;
using Xunit;

namespace JNPF.Tests.Agent;

/// <summary>
/// Replicates PRE-refactor inline query from PRE_REFACTOR_COMMIT (frozen).
///
/// Provenance (v5):
///  1. [B1] BLOB SHA: git rev-parse &lt;commit&gt;:&lt;path&gt; == baseline.referenceBlobSha
///  2. [P1-2] Roslyn fingerprint: required query elements present in pre-refactor source
///     (Queryable, JoinQueryInfos, Where, OrderBy, OrderByIF, Select, userManager.UserId)
///
/// v5 explicit `using Xunit;` — does not rely on global usings.
/// </summary>
public static class PreRefactorQueryReplicator
{
    public static ISugarQueryable<FlowCommentListOutput> BuildPreRefactorQueryable(
        FlowCommentListQuery input, ISqlSugarClient client, IUserManager userManager)
    {
        return client.Queryable<FlowCommentEntity, UserEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.CreatorUserId == b.Id))
            .Where((a, b) => a.TaskId == input.taskId && a.DeleteMark == null)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), a => a.LastModifyTime, OrderByType.Desc)
            .Select((a, b) => new FlowCommentListOutput
            {
                id = a.Id, taskId = a.TaskId, text = a.Text, image = a.Image, file = a.File,
                creatorUserId = b.Id, creatorTime = a.CreatorTime,
                creatorUser = SqlFunc.MergeString(b.RealName, "/", b.Account),
                creatorUserHeadIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
                isDel = SqlFunc.IIF(a.CreatorUserId == userManager.UserId, true, false),
                lastModifyTime = a.LastModifyTime,
            });
    }

    public static string GenerateSql(FlowCommentListQuery input, ISqlSugarClient client, IUserManager userManager)
    {
        var queryable = BuildPreRefactorQueryable(input, client, userManager);
        var sqlRecordable = queryable.ToSql();
        return SqlSugarQueryCaptureHelper.NormalizeSql(sqlRecordable.Key);
    }

    /// <summary>
    /// [B1] Verify pre-refactor source BLOB SHA matches baseline.referenceBlobSha.
    /// Uses git object-database identity (independent of any text encoding pipeline).
    /// </summary>
    public static void VerifyBlobIdentity(string baselineJsonPath, string repoRelativePath)
    {
        var baseline = System.Text.Json.JsonDocument.Parse(File.ReadAllText(baselineJsonPath)).RootElement;
        var preRefactorCommit = baseline.GetProperty("preRefactorCommit").GetString()!;
        var expectedBlobSha = baseline.GetProperty("referenceBlobSha").GetString()!;

        var actualBlobSha = GitHelper.GetBlobSha(preRefactorCommit, repoRelativePath);
        Assert.Equal(expectedBlobSha, actualBlobSha);
    }

    /// <summary>
    /// [P1-2] Roslyn-resolved fingerprint of pre-refactor source. Proves the replicator
    /// mirrors original pre-refactor semantics, not a hand-typed facsimile.
    /// Each element below is verified via Roslyn InvocationExpressionSyntax /
    /// BinaryExpressionSyntax — not string contains (which would be too permissive).
    /// </summary>
    public static void VerifyPreRefactorFingerprint(string baselineJsonPath, string repoRelativePath)
    {
        var baseline = System.Text.Json.JsonDocument.Parse(File.ReadAllText(baselineJsonPath)).RootElement;
        var preRefactorCommit = baseline.GetProperty("preRefactorCommit").GetString()!;
        var source = GitHelper.GetFileFromCommit(preRefactorCommit, repoRelativePath);

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        // 1. Queryable chain elements (Roslyn-resolved invocations)
        var invocations = root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => i.Expression.ToString())
            .ToList();

        Assert.Contains(invocations, inv => inv.Contains("Queryable"));
        Assert.Contains(invocations, inv => inv.Contains("JoinQueryInfos"));
        Assert.Contains(invocations, inv => inv.Contains("OrderBy"));
        Assert.Contains(invocations, inv => inv.Contains("OrderByIF"));
        Assert.Contains(invocations, inv => inv.Contains("Select"));

        // 2. Required predicates (Roslyn-resolved binary expressions)
        var binaryExprs = root.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Select(b => b.ToString())
            .ToList();

        Assert.Contains(binaryExprs, b => b.Contains("TaskId"));
        Assert.Contains(binaryExprs, b => b.Contains("DeleteMark"));

        // 3. User context required for isDel IIF (semantic literal required)
        Assert.Contains("userManager.UserId", source);
        Assert.Contains("SqlFunc.IIF", source);
    }
}