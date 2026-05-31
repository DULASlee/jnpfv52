using System.Globalization;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.API.Entry.Services;

/// <summary>
/// 技术日志查询服务 — 读取 Serilog JSON 文件日志，提供错误查询、链路追踪、慢查询三个端点.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "TechnicalLog", Order = 220)]
[Route("api/system/[controller]")]
public class TechnicalLogService : IDynamicApiController, ITransient
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<TechnicalLogService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public TechnicalLogService(IConfiguration cfg, ILogger<TechnicalLogService> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    #region GET

    /// <summary>
    /// 获取错误日志列表（带分页）.
    /// </summary>
    /// <param name="date">日期，默认今天.</param>
    /// <param name="page">页码，从1开始.</param>
    /// <param name="pageSize">每页条数.</param>
    /// <param name="level">日志级别过滤，默认 Error.</param>
    /// <returns>分页结果.</returns>
    [HttpGet("errors")]
    public async Task<PagedResult<TechLogEntry>> GetErrorsAsync(
        [FromQuery] DateTime? date = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string level = "Error")
    {
        var targetDate = date ?? DateTime.Today;
        var entries = await ReadLogEntriesAsync(targetDate, level);

        // 按时间倒序
        entries = entries.OrderByDescending(e => e.Timestamp).ToList();

        var total = entries.Count;
        var items = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<TechLogEntry> { Items = items, Total = total };
    }

    /// <summary>
    /// 根据 TraceId 聚合链路日志.
    /// </summary>
    /// <param name="traceId">链路追踪ID.</param>
    /// <returns>聚合结果.</returns>
    [HttpGet("trace")]
    public async Task<TraceAggregateResult> GetTraceAsync([FromQuery] string traceId)
    {
        var result = new TraceAggregateResult { TraceId = traceId ?? string.Empty };

        if (string.IsNullOrWhiteSpace(traceId))
            return result;

        // 搜索今天和昨天的日志（跨天链路）
        var dates = new[] { DateTime.Today, DateTime.Today.AddDays(-1) };

        foreach (var date in dates)
        {
            var entries = await ReadLogEntriesAsync(date);
            var matched = entries.Where(e =>
                string.Equals(e.TraceId, traceId, StringComparison.OrdinalIgnoreCase)).ToList();
            result.FileLogs.AddRange(matched);
        }

        // 按时间排序
        result.FileLogs = result.FileLogs.OrderBy(e => e.Timestamp).ToList();

        return result;
    }

    /// <summary>
    /// 获取慢请求日志（Slow SQL）.
    /// </summary>
    /// <param name="date">日期，默认今天.</param>
    /// <param name="thresholdMs">耗时阈值（毫秒），默认 1000ms.</param>
    /// <returns>慢请求列表.</returns>
    [HttpGet("slow-requests")]
    public async Task<List<TechLogEntry>> GetSlowRequestsAsync(
        [FromQuery] DateTime? date = null,
        [FromQuery] int thresholdMs = 1000)
    {
        var targetDate = date ?? DateTime.Today;

        // 慢 SQL 在 warning 文件中
        var entries = await ReadLogEntriesAsync(targetDate, "Warning");

        var slow = entries
            .Where(e => e.Message != null && e.Message.Contains("Slow SQL", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        return slow;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 读取指定日期和级别的 Serilog JSON 日志文件.
    /// </summary>
    private async Task<List<TechLogEntry>> ReadLogEntriesAsync(DateTime date, string? levelFilter = null)
    {
        var logDir = _cfg["Logging:File:LogDir"] ?? "logs";
        var dateStr = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        // 确定要读取的文件列表
        var files = new List<string>();

        if (string.IsNullOrEmpty(levelFilter) ||
            levelFilter.Equals("Error", StringComparison.OrdinalIgnoreCase))
        {
            var errorFile = Path.Combine(logDir, $"error-{dateStr}.json");
            if (File.Exists(errorFile))
                files.Add(errorFile);
        }

        if (string.IsNullOrEmpty(levelFilter) ||
            levelFilter.Equals("Warning", StringComparison.OrdinalIgnoreCase))
        {
            var warningFile = Path.Combine(logDir, $"warning-{dateStr}.json");
            if (File.Exists(warningFile))
                files.Add(warningFile);
        }

        // Error 级别日志也可能写入 warning 文件（fallback）
        if (levelFilter?.Equals("Error", StringComparison.OrdinalIgnoreCase) == true && files.Count == 0)
        {
            var warningFile = Path.Combine(logDir, $"warning-{dateStr}.json");
            if (File.Exists(warningFile))
                files.Add(warningFile);
        }

        var allEntries = new List<TechLogEntry>();

        foreach (var file in files.Distinct())
        {
            var entries = await ReadSerilogJsonFileAsync(file);
            if (!string.IsNullOrEmpty(levelFilter))
            {
                entries = entries.Where(e =>
                    string.Equals(e.Level, levelFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            allEntries.AddRange(entries);
        }

        return allEntries;
    }

    /// <summary>
    /// 读取单个 Serilog JSON 文件，每行一个 JSON 对象.
    /// 使用 FileShare.ReadWrite 确保可以读取正在被 Serilog 写入的文件.
    /// </summary>
    private async Task<List<TechLogEntry>> ReadSerilogJsonFileAsync(string filePath)
    {
        var entries = new List<TechLogEntry>();

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    // Serilog JsonFormatter 输出格式：
                    // {"Timestamp":"...","Level":"...","MessageTemplate":"...","Message":"...","Properties":{...},"Exception":"..."}
                    var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    var entry = new TechLogEntry
                    {
                        Timestamp = GetDateTime(root, "Timestamp"),
                        Level = GetString(root, "Level"),
                        MessageTemplate = GetString(root, "MessageTemplate"),
                        Message = GetString(root, "Message"),
                        Exception = GetString(root, "Exception"),
                    };

                    // Properties 是嵌套对象，TraceId/UserId/TenantId 在其中
                    if (root.TryGetProperty("Properties", out var props))
                    {
                        entry.TraceId = GetString(props, "TraceId");
                        entry.UserId = GetString(props, "UserId");
                        entry.TenantId = GetString(props, "TenantId");
                    }

                    entries.Add(entry);
                }
                catch (JsonException)
                {
                    // 最后一行可能不完整（Serilog 正在写入），静默跳过
                    _logger.LogDebug("跳过无法解析的日志行: {FilePath}", filePath);
                }
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "读取日志文件失败: {FilePath}", filePath);
        }

        return entries;
    }

    private static string GetString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? string.Empty
                : prop.GetRawText();
        }
        return string.Empty;
    }

    private static DateTime GetDateTime(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(prop.GetString(), out var dt))
            {
                return dt;
            }
        }
        return DateTime.MinValue;
    }

    #endregion
}

#region Models

/// <summary>
/// Serilog 技术日志条目.
/// </summary>
public class TechLogEntry
{
    /// <summary>
    /// 日志时间戳.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 日志级别.
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// 消息模板.
    /// </summary>
    public string MessageTemplate { get; set; } = string.Empty;

    /// <summary>
    /// 渲染后的消息.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息.
    /// </summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>
    /// 链路追踪ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// 分页结果.
/// </summary>
/// <typeparam name="T">条目类型.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 数据列表.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 总条数.
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// 链路追踪聚合结果.
/// </summary>
public class TraceAggregateResult
{
    /// <summary>
    /// 链路追踪ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 文件日志条目.
    /// </summary>
    public List<TechLogEntry> FileLogs { get; set; } = new();
}

#endregion
