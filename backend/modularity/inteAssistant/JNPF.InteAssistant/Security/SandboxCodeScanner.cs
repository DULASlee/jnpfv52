using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Security;

/// <summary>
/// 沙箱代码安全扫描器（正则版 · 阶段五）。
///
/// 对用户提交到沙箱的代码进行黑名单关键字扫描，
/// 检测潜在的恶意或危险操作。阶段五使用正则匹配；
/// 阶段六升级为 Roslyn 完整 AST 分析。
///
/// 检测类别：
///   1. 文件系统破坏 — File.Delete / Directory.Delete
///   2. 进程启动 — Process.Start
///   3. 硬编码网络地址 — IP / URL
///   4. 跨租户数据访问 — Tenant_ 前缀越权
///   5. 反射/动态代码执行 — Assembly.Load / eval
/// </summary>
public class SandboxCodeScanner : ISandboxCodeScanner, ITransient
{
    private static readonly List<BlacklistRule> Rules = BuildRules();

    /// <summary>
    /// 扫描代码内容，返回所有违规项。
    /// </summary>
    /// <param name="code">待扫描的代码文本</param>
    /// <param name="filePath">文件路径（用于结果定位）</param>
    /// <returns>违规列表（空 = 安全）</returns>
    public ScanResult Scan(string code, string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ScanResult.Clean;

        var violations = new List<CodeViolation>();

        foreach (var rule in Rules)
        {
            var matches = rule.Pattern.Matches(code);
            foreach (Match match in matches)
            {
                var lineNumber = GetLineNumber(code, match.Index);
                violations.Add(new CodeViolation
                {
                    RuleId = rule.Id,
                    Category = rule.Category,
                    Severity = rule.Severity,
                    Description = rule.Description,
                    MatchedText = match.Value,
                    FilePath = filePath ?? "(inline)",
                    LineNumber = lineNumber,
                    Column = match.Index - GetLineStartIndex(code, match.Index),
                });
            }
        }

        return new ScanResult
        {
            IsClean = violations.Count == 0,
            Violations = violations,
            TotalFilesScanned = 1,
            TotalViolations = violations.Count,
        };
    }

    /// <summary>
    /// 批量扫描多个文件。
    /// </summary>
    public ScanResult ScanMultiple(IEnumerable<(string FilePath, string Content)> files)
    {
        var allViolations = new List<CodeViolation>();
        var fileCount = 0;

        foreach (var (filePath, content) in files)
        {
            fileCount++;
            var result = Scan(content, filePath);
            allViolations.AddRange(result.Violations);
        }

        return new ScanResult
        {
            IsClean = allViolations.Count == 0,
            Violations = allViolations,
            TotalFilesScanned = fileCount,
            TotalViolations = allViolations.Count,
        };
    }

    #region Rule Definitions

    private static List<BlacklistRule> BuildRules()
    {
        return new List<BlacklistRule>
        {
            // === 类别 1：文件系统破坏 ===
            new()
            {
                Id = "SEC-FS-001",
                Category = "FileSystem",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"File\.Delete\s*\(|Directory\.Delete\s*\(|File\.Move\s*\(",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "文件/目录删除操作：禁止在沙箱中执行文件系统破坏操作",
            },
            new()
            {
                Id = "SEC-FS-002",
                Category = "FileSystem",
                Severity = ViolationSeverity.High,
                Pattern = new Regex(@"System\.IO\.File\.WriteAll|System\.IO\.Directory\.Create",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "文件写入操作：沙箱代码不应直接写入服务器文件系统",
            },

            // === 类别 2：进程启动与系统命令 ===
            new()
            {
                Id = "SEC-PROC-001",
                Category = "ProcessExecution",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"Process\.Start\s*\(|System\.Diagnostics\.Process",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "进程启动操作：禁止在沙箱中启动外部进程",
            },
            new()
            {
                Id = "SEC-PROC-002",
                Category = "ProcessExecution",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"new\s+Process\s*\{|new\s+ProcessStartInfo",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "Process 实例化：禁止创建进程对象",
            },

            // === 类别 3：硬编码网络地址 ===
            new()
            {
                Id = "SEC-NET-001",
                Category = "NetworkAccess",
                Severity = ViolationSeverity.High,
                Pattern = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
                    RegexOptions.Compiled),
                Description = "硬编码 IP 地址：禁止在代码中硬编码 IP（应使用配置或环境变量）",
            },
            new()
            {
                Id = "SEC-NET-002",
                Category = "NetworkAccess",
                Severity = ViolationSeverity.Medium,
                Pattern = new Regex(@"https?://(?!localhost|127\.0\.0\.1)[^\s""'\)]+",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "外部 URL：禁止硬编码外部 URL 发起网络请求",
            },

            // === 类别 4：跨租户数据访问 ===
            new()
            {
                Id = "SEC-TENANT-001",
                Category = "TenantIsolation",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"\bTenant_\w+|F_TENANT_ID\s*=\s*['""](?!@)[^'""]+['""]",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "跨租户数据访问：禁止硬编码或引用其他租户的前缀和 TenantId",
            },

            // === 类别 5：反射与动态代码执行 ===
            new()
            {
                Id = "SEC-REFL-001",
                Category = "Reflection",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"Assembly\.Load\s*\(|Activator\.CreateInstance\s*\(|Type\.GetType\s*\(",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "反射加载：禁止动态加载程序集或类型",
            },
            new()
            {
                Id = "SEC-REFL-002",
                Category = "Reflection",
                Severity = ViolationSeverity.Critical,
                Pattern = new Regex(@"\beval\s*\(|CSharpCodeProvider|CodeDomProvider",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "动态代码执行：禁止 eval 或动态编译代码",
            },

            // === 类别 6：SQL 注入风险 ===
            new()
            {
                Id = "SEC-SQL-001",
                Category = "SqlInjection",
                Severity = ViolationSeverity.High,
                Pattern = new Regex(@"EXEC\s*\(|sp_executesql|DROP\s+TABLE|DROP\s+DATABASE|TRUNCATE\s+TABLE",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "SQL 注入/DDL 操作：禁止拼接 SQL 或执行 DDL 语句",
            },
            new()
            {
                Id = "SEC-SQL-002",
                Category = "SqlInjection",
                Severity = ViolationSeverity.Medium,
                Pattern = new Regex(@"SqlCommand|OleDbCommand|OracleCommand",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Description = "原始 SQL 命令：建议使用 ORM 而非原始 ADO.NET 命令",
            },
        };
    }

    #endregion

    #region Helpers

    private static int GetLineNumber(string code, int index)
    {
        var lineCount = 1;
        for (var i = 0; i < index && i < code.Length; i++)
        {
            if (code[i] == '\n') lineCount++;
        }
        return lineCount;
    }

    private static int GetLineStartIndex(string code, int index)
    {
        var lineStart = code.LastIndexOf('\n', Math.Max(0, index - 1));
        return lineStart >= 0 ? lineStart + 1 : 0;
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// 沙箱代码扫描器接口。
/// </summary>
public interface ISandboxCodeScanner
{
    ScanResult Scan(string code, string? filePath = null);
    ScanResult ScanMultiple(IEnumerable<(string FilePath, string Content)> files);
}

/// <summary>
/// 扫描结果。
/// </summary>
public class ScanResult
{
    public static readonly ScanResult Clean = new()
    {
        IsClean = true,
        Violations = new List<CodeViolation>(),
        TotalFilesScanned = 0,
        TotalViolations = 0,
    };

    public bool IsClean { get; set; }
    public List<CodeViolation> Violations { get; set; } = new();
    public int TotalFilesScanned { get; set; }
    public int TotalViolations { get; set; }

    /// <summary>
    /// 返回仅包含 Critical 和 High 严重级别的违规。
    /// </summary>
    public List<CodeViolation> BlockingViolations =>
        Violations.Where(v => v.Severity >= ViolationSeverity.High).ToList();
}

/// <summary>
/// 代码违规记录。
/// </summary>
public class CodeViolation
{
    public string RuleId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MatchedText { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int Column { get; set; }
}

/// <summary>
/// 违规严重级别。
/// </summary>
public enum ViolationSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>
/// 黑名单规则定义。
/// </summary>
internal class BlacklistRule
{
    public string Id { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public ViolationSeverity Severity { get; init; }
    public Regex Pattern { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
}

#endregion
