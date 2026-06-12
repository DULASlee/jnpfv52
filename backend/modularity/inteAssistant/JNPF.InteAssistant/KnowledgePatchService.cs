using System.Security.Cryptography;
using System.Text;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace JNPF.InteAssistant;

/// <summary>
/// KnowledgePatch 签名验证服务
/// Foundry → Studio 的知识传递安全通道
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "KnowledgePatch", Order = 179)]
[Route("api/InteAssistant/[controller]")]
public class KnowledgePatchService : IDynamicApiController, ITransient
{
    private readonly IKnowledgeGraphStore _knowledgeGraphStore;
    private readonly IConfiguration _configuration;

    private const string SignatureKeyConfig = "KnowledgePatch:SignatureKey";

    /// <summary>
    /// 初始化一个<see cref="KnowledgePatchService"/>类型的新实例
    /// </summary>
    public KnowledgePatchService(
        IKnowledgeGraphStore knowledgeGraphStore,
        IConfiguration configuration)
    {
        _knowledgeGraphStore = knowledgeGraphStore;
        _configuration = configuration;
    }

    /// <summary>
    /// 验证 KnowledgePatch 包签名完整性
    /// </summary>
    [HttpPost("Verify")]
    public async Task<dynamic> Verify([FromBody] KnowledgeIncrementPackage package)
    {
        if (string.IsNullOrEmpty(package.Content))
            throw Oops.Bah("Content 不能为空");

        if (string.IsNullOrEmpty(package.PackageHash))
            throw Oops.Bah("PackageHash 不能为空");

        if (string.IsNullOrEmpty(package.Signature))
            throw Oops.Bah("Signature 不能为空");

        // 1. 验证内容哈希
        var computedHash = ComputeSha256(package.Content);
        if (!string.Equals(computedHash, package.PackageHash, StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("PackageHash 校验失败：内容已被篡改");

        // 2. 验证签名
        var signingKey = _configuration.GetValue<string>(SignatureKeyConfig, "jnpf-default-signing-key");
        var computedSignature = ComputeHmacSha256(package.PackageHash, signingKey);
        if (!string.Equals(computedSignature, package.Signature, StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("Signature 校验失败：签名不匹配");

        // 3. 验证通过，写入知识图谱
        try
        {
            // Content JSON 解析为节点/边列表
            var contentJson = System.Text.Json.JsonDocument.Parse(package.Content);
            var root = contentJson.RootElement;

            var insertedNodes = new List<string>();

            // 写入节点
            if (root.TryGetProperty("nodes", out var nodes))
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    var label = node.GetProperty("label").GetString() ?? "entity";
                    var name = node.GetProperty("name").GetString() ?? "";
                    var props = node.TryGetProperty("properties", out var p) ? p.GetRawText() : null;

                    var result = await _knowledgeGraphStore.AddNodeAsync(label, name, props);
                    insertedNodes.Add(result.Id);
                }
            }

            // 写入边
            if (root.TryGetProperty("edges", out var edges))
            {
                foreach (var edge in edges.EnumerateArray())
                {
                    var sourceId = edge.GetProperty("sourceNodeId").GetString() ?? "";
                    var targetId = edge.GetProperty("targetNodeId").GetString() ?? "";
                    var relationType = edge.GetProperty("relationType").GetString() ?? "references";
                    var props = edge.TryGetProperty("properties", out var ep) ? ep.GetRawText() : null;

                    await _knowledgeGraphStore.AddEdgeAsync(sourceId, targetId, relationType, props);
                }
            }

            return new { success = true, nodesInserted = insertedNodes.Count, edgesInserted = edges.GetArrayLength() };
        }
        catch (Exception ex) when (ex is not AppFriendlyException)
        {
            throw Oops.Oh("知识图谱写入失败：" + ex.Message);
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var hashBytes = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
