using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.XWPF.UserModel;
using OfficeOpenXml;
using System.Text;
using UglyToad.PdfPig;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 附件预处理服务
/// 支持：Excel / Word(docx) / PDF / 图片 / 纯文本
///
/// 修复项（相对清言版本）：
///   ✅ EPPlus LicenseContext 改为静态构造函数（只执行一次） — #7
///   ✅ XWPFDocument 加 using（修复内存泄漏） — #2
///   ✅ ProcessAttachmentsAsync 去掉 async 状态机（修复性能浪费） — #5
///   ✅ 添加 MemoryStream using 确保所有流资源释放
/// </summary>
public class AttachmentProcessor : ITransient
{
    private readonly ILogger<AttachmentProcessor> _logger;
    private const int MaxExtractedLength = 30000;

    // 修复 #7：静态构造函数，确保只执行一次
    static AttachmentProcessor()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理所有附件，返回合并后的纯文本
    /// 修复 #5：去掉 async，直接返回 Task（无真正异步操作时避免状态机开销）
    /// </summary>
    public Task<string> ProcessAttachmentsAsync(List<AttachmentFile> attachments)
    {
        if (attachments == null || attachments.Count == 0)
            return Task.FromResult("");

        var parts = new List<string>();

        foreach (var file in attachments)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName)?.ToLower();
                var extracted = ext switch
                {
                    ".xlsx" or ".xls" => ExtractExcel(file.Content),
                    ".docx" => ExtractWord(file.Content),
                    ".pdf" => ExtractPdf(file.Content),
                    // 图片走多模态LLM，这里只标记占位
                    _ when GateConstants.IsImageFile(file.FileName)
                        => $"[附件：图片 {file.FileName}，需通过多模态模型提取]",
                    ".txt" or ".csv" => ExtractText(file.Content),
                    _ => $"[附件：{file.FileName}，格式{ext}暂不支持自动解析]"
                };

                if (!string.IsNullOrWhiteSpace(extracted))
                {
                    parts.Add($"\n\n===== 附件：{file.FileName} =====\n{extracted}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "附件处理失败: {FileName}", file.FileName);
                parts.Add($"\n\n[附件 {file.FileName} 处理失败：{ex.Message}]");
            }
        }

        var result = string.Join("", parts);
        if (result.Length > MaxExtractedLength)
        {
            result = result[..MaxExtractedLength] + "\n\n[... 内容过长，已截断 ...]";
        }

        return Task.FromResult(result);
    }

    /// <summary>判断附件中是否包含图片（需走多模态LLM）</summary>
    public bool HasImageAttachments(List<AttachmentFile> attachments)
    {
        return attachments?.Any(a => GateConstants.IsImageFile(a.FileName)) ?? false;
    }

    // ═══════════════════════════════════════════════════
    // Excel 提取
    // ═══════════════════════════════════════════════════

    private string ExtractExcel(byte[] content)
    {
        // using 确保 MemoryStream 和 ExcelPackage 都被释放
        using var stream = new MemoryStream(content);
        using var package = new ExcelPackage(stream);
        var workbook = package.Workbook;

        if (workbook?.Worksheets == null || workbook.Worksheets.Count == 0)
            return "";

        var result = new StringBuilder();

        foreach (var worksheet in workbook.Worksheets)
        {
            result.AppendLine($"【Sheet: {worksheet.Name}】");

            if (worksheet.Dimension == null)
            {
                result.AppendLine("（空表）");
                continue;
            }

            var rowCount = Math.Min(worksheet.Dimension.End.Row, 51);  // 表头 + 50行
            var colCount = Math.Min(worksheet.Dimension.End.Column, 20); // 最多20列

            // 表头
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var val = worksheet.Cells[1, col]?.Text?.Trim();
                headers.Add(string.IsNullOrWhiteSpace(val) ? $"列{col}" : val);
            }
            result.AppendLine(string.Join(" | ", headers));
            result.AppendLine(new string('-', headers.Sum(h => h.Length) + headers.Count * 3));

            // 数据行
            for (int row = 2; row <= rowCount; row++)
            {
                var cells = new List<string>();
                for (int col = 1; col <= colCount; col++)
                {
                    cells.Add(worksheet.Cells[row, col]?.Text?.Trim() ?? "");
                }
                result.AppendLine(string.Join(" | ", cells));
            }

            if (worksheet.Dimension.End.Row > 51)
            {
                result.AppendLine($"... 共{worksheet.Dimension.End.Row}行，仅显示前50行");
            }
            result.AppendLine();
        }

        return result.ToString();
    }

    // ═══════════════════════════════════════════════════
    // Word 提取
    // 修复 #2：XWPFDocument 加 using（修复内存泄漏）
    // ═══════════════════════════════════════════════════

    private string ExtractWord(byte[] content)
    {
        using var stream = new MemoryStream(content);
        // 修复 #2：XWPFDocument 实现了 IDisposable，必须 using
        using var doc = new XWPFDocument(stream);

        var result = new StringBuilder();

        foreach (var paragraph in doc.Paragraphs)
        {
            var text = paragraph.ParagraphText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var style = paragraph.Style;
                if (!string.IsNullOrWhiteSpace(style) && style.StartsWith("Heading"))
                    result.AppendLine($"## {text}");
                else
                    result.AppendLine(text);
            }
        }

        foreach (var table in doc.Tables)
        {
            result.AppendLine("\n[表格]");
            foreach (var row in table.Rows)
            {
                var cells = row.GetTableCells().Select(c => c.GetText()?.Trim() ?? "");
                result.AppendLine(string.Join(" | ", cells));
            }
        }

        return result.ToString();
    }

    // ═══════════════════════════════════════════════════
    // PDF 提取
    // ═══════════════════════════════════════════════════

    private string ExtractPdf(byte[] content)
    {
        // PdfPig 的 PdfDocument.Open(byte[]) 内部会复制数据
        // 返回的 PdfDocument 实现了 IDisposable
        using var document = PdfDocument.Open(content);

        var result = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.AppendLine(text);
            }
        }

        return result.ToString();
    }

    // ═══════════════════════════════════════════════════
    // 纯文本提取
    // ═══════════════════════════════════════════════════

    private string ExtractText(byte[] content)
    {
        return Encoding.UTF8.GetString(content);
    }
}

// ═══════════════════════════════════════════════════
// DTO
// ═══════════════════════════════════════════════════

public class AttachmentFile
{
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
