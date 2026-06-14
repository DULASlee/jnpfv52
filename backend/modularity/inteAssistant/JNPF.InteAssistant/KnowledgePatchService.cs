using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace JNPF.InteAssistant;

/// <summary>
/// KnowledgePatch 签名验证服务 (Phase 6 Enhanced).
/// Foundry → Studio 的知识传递安全通道.
/// 新增: Zip 上传 + UPSERT 合并 + 版本管理.
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "KnowledgePatch", Order = 179)]
[Route("api/InteAssistant/[controller]")]
public class KnowledgePatchService : IDynamicApiController, ITransient
{
    private readonly IKnowledgeGraphStore _knowledgeGraphStore;
    private readonly IConfiguration _configuration;

    private const string SignatureKeyConfig = "KnowledgePatch:SignatureKey";
    private static int _patchVersion;

    public KnowledgePatchService(
        IKnowledgeGraphStore knowledgeGraphStore,
        IConfiguration configuration)
    {
        _knowledgeGraphStore = knowledgeGraphStore;
        _configuration = configuration;
    }

    /// <summary>
    /// 验证 KnowledgePatch 包签名完整性（JSON Body 模式）.
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
        var signingKey = GetSigningKey();
        var computedSignature = ComputeHmacSha256(package.PackageHash, signingKey);
        if (!string.Equals(computedSignature, package.Signature, StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("Signature 校验失败：签名不匹配");

        // 3. 合并到知识图谱
        var result = await MergeContentAsync(package.Content, isUpsert: false);
        return result;
    }

    /// <summary>
    /// Phase 6 新增：接收签名 Zip 包并合并.
    /// 请求格式: multipart/form-data，字段 zip(file) + signature(string).
    /// </summary>
    [HttpPost("Receive")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    public async Task<dynamic> Receive()
    {
        var httpContext = App.HttpContext;
        var form = httpContext.Request.Form;

        // 1. 获取 zip 文件
        var zipFile = form.Files.GetFile("zip");
        if (zipFile == null || zipFile.Length == 0)
            throw Oops.Bah("请上传 zip 文件（字段名: zip）");

        // 2. 获取签名
        var signature = form["signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
            throw Oops.Bah("请提供签名（字段名: signature）");

        // 3. 读取 zip 内容并计算哈希
        byte[] zipBytes;
        using (var ms = new MemoryStream())
        {
            await zipFile.CopyToAsync(ms);
            zipBytes = ms.ToArray();
        }

        var computedHash = ComputeSha256(zipBytes);

        // 4. 验证签名
        var signingKey = GetSigningKey();
        var computedSignature = ComputeHmacSha256(computedHash, signingKey);
        if (!string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("Signature 校验失败：签名不匹配");

        // 5. 解析 zip 内容
        string contentJson;
        using (var zipStream = new MemoryStream(zipBytes))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".json"))
                ?? archive.Entries.FirstOrDefault();
            if (entry == null)
                throw Oops.Bah("Zip 包中未找到 JSON 文件");

            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            contentJson = await reader.ReadToEndAsync();
        }

        // 6. 合并到知识图谱（UPSERT 模式）
        var result = await MergeContentAsync(contentJson, isUpsert: true);
        return result;
    }

    // ─── 查询端点 ───

    /// <summary>
    /// 查询知识节点（分页 + 域过滤）.
    /// </summary>
    [HttpGet("nodes")]
    public async Task<dynamic> ListNodes(
        [FromQuery] string? label = null,
        [FromQuery] string? domain = null,
        [FromQuery] int currentPage = 1,
        [FromQuery] int pageSize = 20)
    {
        var (list, total) = await _knowledgeGraphStore.ListNodesAsync(
            label, domain, currentPage, pageSize);
        return new { list, pagination = new { total, currentPage, pageSize } };
    }

    /// <summary>
    /// 获取单个节点详情.
    /// </summary>
    [HttpGet("nodes/{id}")]
    public async Task<dynamic> GetNode(string id)
    {
        var node = await _knowledgeGraphStore.GetNodeAsync(id);
        if (node == null)
            throw Oops.Bah($"节点 {id} 不存在");

        return node;
    }

    /// <summary>
    /// 查询关系边（分页）.
    /// </summary>
    [HttpGet("edges")]
    public async Task<dynamic> ListEdges(
        [FromQuery] string? relationType = null,
        [FromQuery] int currentPage = 1,
        [FromQuery] int pageSize = 20)
    {
        var (list, total) = await _knowledgeGraphStore.ListEdgesAsync(
            relationType, currentPage, pageSize);
        return new { list, pagination = new { total, currentPage, pageSize } };
    }

    /// <summary>
    /// 获取知识图谱统计.
    /// </summary>
    [HttpGet("stats")]
    public async Task<dynamic> GetStats()
    {
        var stats = await _knowledgeGraphStore.GetStatsAsync();
        stats.PatchVersion = _patchVersion;
        return stats;
    }

    // ─── Private helpers ───

    private async Task<dynamic> MergeContentAsync(string contentJson, bool isUpsert)
    {
        try
        {
            var content = JsonDocument.Parse(contentJson);
            var root = content.RootElement;

            var insertedNodes = new List<string>();
            var updatedNodes = new List<string>();

            // 写入节点
            if (root.TryGetProperty("nodes", out var nodes))
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    var label = node.GetProperty("label").GetString() ?? "entity";
                    var name = node.GetProperty("name").GetString() ?? "";
                    var props = node.TryGetProperty("properties", out var p) ? p.GetRawText() : null;

                    if (isUpsert)
                    {
                        var result = await _knowledgeGraphStore.UpsertNodeAsync(label, name, props);
                        if (result.CreatorTime == result.LastModifyTime)
                            insertedNodes.Add(result.Id);
                        else
                            updatedNodes.Add(result.Id);
                    }
                    else
                    {
                        var result = await _knowledgeGraphStore.AddNodeAsync(label, name, props);
                        insertedNodes.Add(result.Id);
                    }
                }
            }

            // 写入边
            var edgesInserted = 0;
            if (root.TryGetProperty("edges", out var edges))
            {
                foreach (var edge in edges.EnumerateArray())
                {
                    var sourceId = edge.GetProperty("sourceNodeId").GetString() ?? "";
                    var targetId = edge.GetProperty("targetNodeId").GetString() ?? "";
                    var relationType = edge.GetProperty("relationType").GetString() ?? "references";
                    var props = edge.TryGetProperty("properties", out var ep) ? ep.GetRawText() : null;

                    await _knowledgeGraphStore.AddEdgeAsync(sourceId, targetId, relationType, props);
                    edgesInserted++;
                }
            }

            var version = Interlocked.Increment(ref _patchVersion);

            return new
            {
                success = true,
                nodesInserted = insertedNodes.Count,
                nodesUpdated = updatedNodes.Count,
                edgesInserted,
                patchVersion = version
            };
        }
        catch (Exception ex) when (ex is not AppFriendlyException)
        {
            throw Oops.Oh("知识图谱合并失败：" + ex.Message);
        }
    }

    private string GetSigningKey()
    {
        return _configuration.GetValue<string>(SignatureKeyConfig, "jnpf-default-signing-key");
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeSha256(byte[] input)
    {
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var hashBytes = HMACSHA256.HashData(keyBytes, messageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
