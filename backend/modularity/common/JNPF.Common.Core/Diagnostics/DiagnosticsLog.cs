using System.Text.Json;

namespace JNPF.Common.Core.Diagnostics;

/// <summary>
/// 诊断探针 — 从 X-Diagnostics header 反序列化.
/// </summary>
public class DiagnosticsProbe
{
    public string Category { get; set; } = "probe";
    public string Level { get; set; } = "trace";
    public bool TraceSql { get; set; }
    public string? Ts { get; set; }
}

/// <summary>
/// 统一诊断日志 — 替代散落的 File.AppendAllText / Console.WriteLine。
/// 写入 backend/.claude/diagnostics/ 目录，按 session 分文件，JSONL 格式。
/// Agent 可以通过 Read + jq 直接分析。
/// </summary>
public static class DiagnosticsLog
{
    // 从进程所在目录探测 backend/.claude/diagnostics
    private static string FindDiagnosticsDir()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // baseDir 是 bin/Debug/net8.0/，向上 5 级到 backend/
            var backendDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
            var diagDir = Path.Combine(backendDir, ".claude", "diagnostics");
            Directory.CreateDirectory(diagDir);
            return diagDir;
        }
        catch (Exception ex)
        {
            // Last resort: write to temp
            var fallback = Path.Combine(Path.GetTempPath(), "jnpf-diagnostics");
            Directory.CreateDirectory(fallback);
            File.WriteAllText(Path.Combine(fallback, "init-error.log"), ex.ToString());
            return fallback;
        }
    }

    private static readonly string BaseDir = FindDiagnosticsDir();

    private static readonly string SessionFile = Path.Combine(
        BaseDir, $"session-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");

    private static readonly object _lock = new();
    private static bool _initialized;

    static DiagnosticsLog()
    {
        try
        {
            Directory.CreateDirectory(BaseDir);
            // 清理 7 天前的旧日志
            foreach (var f in Directory.GetFiles(BaseDir, "session-*.jsonl"))
            {
                if (File.GetLastWriteTime(f) < DateTime.Now.AddDays(-7))
                    File.Delete(f);
            }
            _initialized = true;
        }
        catch { }
    }

    /// <summary>
    /// 记录诊断事件.
    /// </summary>
    /// <param name="category">分类（如 "IM", "WebSocket", "DB", "API"）</param>
    /// <param name="eventName">事件名（如 "SendMessage", "OnConnection"）</param>
    /// <param name="data">诊断数据（任意可 JSON 序列化的对象）</param>
    /// <param name="level">级别: trace / info / warn / error</param>
    public static void Log(string category, string eventName, object? data = null, string level = "info")
    {
        if (!_initialized) return;
        try
        {
            var entry = new
            {
                ts = DateTime.Now.ToString("O"),
                category,
                evt = eventName,
                level,
                data
            };
            var line = JsonSerializer.Serialize(entry) + "\n";
            lock (_lock)
            {
                File.AppendAllText(SessionFile, line);
            }
        }
        catch { }
    }

    /// <summary>
    /// 记录异常.
    /// </summary>
    public static void Error(string category, string eventName, Exception ex, object? context = null)
    {
        Log(category, eventName, new
        {
            error = ex.GetType().Name,
            message = ex.Message,
            stackTrace = ex.StackTrace?.Split('\n').Take(5).Select(s => s.Trim()),
            context
        }, "error");
    }

    /// <summary>
    /// 记录 SQL 追踪.
    /// </summary>
    public static void Sql(string operation, string? sql, object? parameters = null)
    {
        Log("SQL", operation, new { sql, parameters }, "trace");
    }

    /// <summary>
    /// 当前 session 的日志文件路径（供 Agent 读取）.
    /// </summary>
    public static string CurrentSessionFile => SessionFile;
}
