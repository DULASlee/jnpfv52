using JNPF.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.XWPF.UserModel;
using OfficeOpenXml;
using System.Text;
using UglyToad.PdfPig;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 附件预处理服务 — 分批解析，不截断全文。
/// 大文件由调用方（PipelineAttachmentService）增量写入分块存档后再合并取出。
/// </summary>
public class AttachmentProcessor : ITransient
{
    private readonly ILogger<AttachmentProcessor> _logger;

    /// <summary>单批目标字符数（按段落/页/行自然边界切分，可略超）。</summary>
    public const int DefaultTargetChunkChars = 8_000;

    private const int MaxExcelColumns = 20;

    static AttachmentProcessor()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 兼容旧调用：同步解析并内存合并（不做长度截断）。
    /// 大文件场景请改用 <see cref="ExtractChunks"/> + 分块存档。
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
                var sb = new StringBuilder();
                sb.AppendLine().AppendLine($"===== 附件：{file.FileName} =====");
                foreach (var chunk in ExtractChunks(file, DefaultTargetChunkChars))
                {
                    if (sb.Length > 0 && !string.IsNullOrWhiteSpace(chunk.Text))
                        sb.AppendLine();
                    sb.Append(chunk.Text);
                }
                var text = sb.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    parts.Add(text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "附件处理失败: {FileName}", file.FileName);
                parts.Add($"\n\n[附件 {file.FileName} 处理失败：{ex.Message}]");
            }
        }

        return Task.FromResult(string.Join("\n\n", parts));
    }

    /// <summary>
    /// 按自然边界分批产出文本块（不截断、不丢中间内容）。
    /// </summary>
    public IEnumerable<AttachmentTextChunk> ExtractChunks(AttachmentFile file, int targetChunkChars = DefaultTargetChunkChars)
    {
        if (file == null) yield break;
        if (targetChunkChars < 1_000) targetChunkChars = DefaultTargetChunkChars;

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        IEnumerable<AttachmentTextChunk> chunks;
        try
        {
            chunks = ext switch
            {
                ".xlsx" or ".xls" => ExtractExcelChunks(file.Content, targetChunkChars),
                ".docx" => ExtractWordChunks(file.Content, targetChunkChars),
                ".pdf" => ExtractPdfChunks(file.Content, targetChunkChars),
                _ when GateConstants.IsImageFile(file.FileName)
                    => SingleChunk($"[附件：图片 {file.FileName}，需通过多模态模型提取]", "image"),
                ".txt" or ".csv" or ".md" => ExtractPlainTextChunks(file.Content, targetChunkChars),
                _ => SingleChunk($"[附件：{file.FileName}，格式{ext}暂不支持自动解析]", "unsupported"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "分批解析失败: {FileName}", file.FileName);
            chunks = SingleChunk($"[附件 {file.FileName} 处理失败：{ex.Message}]", "error");
        }

        var index = 0;
        foreach (var c in chunks)
        {
            var text = SanitizeExtractedText(c.Text);
            if (string.IsNullOrWhiteSpace(text)) continue;
            yield return new AttachmentTextChunk(index++, text, c.SourceHint);
        }
    }

    public static string SanitizeExtractedText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new StringBuilder(text.Length);
        char prev = '\0';
        foreach (var ch in text)
        {
            if (ch == '\0' || ch == '\r') continue;
            if (ch == '\t')
            {
                sb.Append(' ');
                prev = ' ';
                continue;
            }
            if (ch == '\n')
            {
                if (prev == '\n' && sb.Length >= 2 && sb[^1] == '\n' && sb[^2] == '\n')
                    continue;
                sb.Append('\n');
                prev = '\n';
                continue;
            }
            if (char.IsControl(ch)) continue;
            sb.Append(ch);
            prev = ch;
        }
        return sb.ToString().Trim();
    }

    public bool HasImageAttachments(List<AttachmentFile> attachments)
        => attachments?.Any(a => GateConstants.IsImageFile(a.FileName)) ?? false;

    private static IEnumerable<AttachmentTextChunk> SingleChunk(string text, string hint)
    {
        yield return new AttachmentTextChunk(0, text, hint);
    }

    private IEnumerable<AttachmentTextChunk> ExtractWordChunks(byte[] content, int targetChars)
    {
        using var stream = new MemoryStream(content);
        using var doc = new XWPFDocument(stream);

        var buf = new StringBuilder();
        var startItem = 1;
        var item = 0;

        void Flush(string hint, List<AttachmentTextChunk> sink)
        {
            if (buf.Length == 0) return;
            sink.Add(new AttachmentTextChunk(0, buf.ToString(), hint));
            buf.Clear();
        }

        var pending = new List<AttachmentTextChunk>();
        try
        {
            foreach (var paragraph in doc.Paragraphs)
            {
                item++;
                var text = paragraph.ParagraphText?.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                var style = paragraph.Style;
                var line = (!string.IsNullOrWhiteSpace(style) && style.StartsWith("Heading"))
                    ? $"## {text}"
                    : text;

                if (buf.Length > 0 && buf.Length + line.Length + 1 > targetChars)
                {
                    Flush($"word-paras {startItem}-{item - 1}", pending);
                    startItem = item;
                }
                if (buf.Length > 0) buf.AppendLine();
                buf.Append(line);
            }
            Flush($"word-paras {startItem}-{item}", pending);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Word 段落分批提取部分失败");
            if (buf.Length == 0)
                pending.Add(new AttachmentTextChunk(0, $"[Word 段落提取异常：{ex.Message}]", "word-error"));
            else
                Flush($"word-paras {startItem}-{item}-partial", pending);
        }

        try
        {
            var tableIdx = 0;
            foreach (var table in doc.Tables)
            {
                tableIdx++;
                var tb = new StringBuilder();
                tb.AppendLine($"[表格 {tableIdx}]");
                foreach (var row in table.Rows)
                {
                    var cells = row.GetTableCells().Select(c => c.GetText()?.Trim() ?? "");
                    tb.AppendLine(string.Join(" | ", cells));
                    if (tb.Length >= targetChars)
                    {
                        pending.Add(new AttachmentTextChunk(0, tb.ToString(), $"word-table {tableIdx}"));
                        tb.Clear();
                        tb.AppendLine($"[表格 {tableIdx} 续]");
                    }
                }
                if (tb.Length > 0)
                    pending.Add(new AttachmentTextChunk(0, tb.ToString(), $"word-table {tableIdx}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Word 表格分批提取失败（段落已保留）");
            pending.Add(new AttachmentTextChunk(0, $"[表格提取失败：{ex.Message}]", "word-table-error"));
        }

        return pending;
    }

    private IEnumerable<AttachmentTextChunk> ExtractPdfChunks(byte[] content, int targetChars)
    {
        using var document = PdfDocument.Open(content);
        var buf = new StringBuilder();
        var startPage = 1;
        var pageNo = 0;
        var pending = new List<AttachmentTextChunk>();

        foreach (var page in document.GetPages())
        {
            pageNo++;
            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (buf.Length > 0 && buf.Length + text.Length + 1 > targetChars)
            {
                pending.Add(new AttachmentTextChunk(0, buf.ToString(), $"pdf-pages {startPage}-{pageNo - 1}"));
                buf.Clear();
                startPage = pageNo;
            }
            if (buf.Length > 0) buf.AppendLine();
            buf.AppendLine(text);
        }

        if (buf.Length > 0)
            pending.Add(new AttachmentTextChunk(0, buf.ToString(), $"pdf-pages {startPage}-{pageNo}"));

        return pending;
    }

    private IEnumerable<AttachmentTextChunk> ExtractExcelChunks(byte[] content, int targetChars)
    {
        using var stream = new MemoryStream(content);
        using var package = new ExcelPackage(stream);
        var workbook = package.Workbook;
        if (workbook?.Worksheets == null || workbook.Worksheets.Count == 0)
            yield break;

        foreach (var worksheet in workbook.Worksheets)
        {
            if (worksheet.Dimension == null)
            {
                yield return new AttachmentTextChunk(0, $"【Sheet: {worksheet.Name}】\n（空表）", $"excel-{worksheet.Name}");
                continue;
            }

            var colCount = Math.Min(worksheet.Dimension.End.Column, MaxExcelColumns);
            var headerRow = DetectHeaderRow(worksheet, colCount);
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var val = worksheet.Cells[headerRow, col]?.Text?.Trim();
                headers.Add(string.IsNullOrWhiteSpace(val) ? $"列{col}" : val);
            }

            var buf = new StringBuilder();
            buf.AppendLine($"【Sheet: {worksheet.Name}】");
            buf.AppendLine(string.Join(" | ", headers));
            buf.AppendLine(new string('-', Math.Min(120, headers.Sum(h => h.Length) + headers.Count * 3)));

            var startRow = headerRow + 1;
            for (int row = headerRow + 1; row <= worksheet.Dimension.End.Row; row++)
            {
                var cells = new List<string>();
                for (int col = 1; col <= colCount; col++)
                    cells.Add(worksheet.Cells[row, col]?.Text?.Trim() ?? "");
                var line = string.Join(" | ", cells);

                if (buf.Length > 0 && buf.Length + line.Length + 1 > targetChars)
                {
                    yield return new AttachmentTextChunk(0, buf.ToString(),
                        $"excel-{worksheet.Name} rows {startRow}-{row - 1}");
                    buf.Clear();
                    buf.AppendLine($"【Sheet: {worksheet.Name} 续】");
                    buf.AppendLine(string.Join(" | ", headers));
                    startRow = row;
                }
                buf.AppendLine(line);
            }

            if (buf.Length > 0)
            {
                yield return new AttachmentTextChunk(0, buf.ToString(),
                    $"excel-{worksheet.Name} rows {startRow}-{worksheet.Dimension.End.Row}");
            }
        }
    }

    private static IEnumerable<AttachmentTextChunk> ExtractPlainTextChunks(byte[] content, int targetChars)
    {
        var text = Encoding.UTF8.GetString(content);
        if (string.IsNullOrWhiteSpace(text)) yield break;

        using var reader = new StringReader(text);
        var buf = new StringBuilder();
        var startLine = 1;
        var lineNo = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (buf.Length > 0 && buf.Length + line.Length + 1 > targetChars)
            {
                yield return new AttachmentTextChunk(0, buf.ToString(), $"text-lines {startLine}-{lineNo - 1}");
                buf.Clear();
                startLine = lineNo;
            }
            if (buf.Length > 0) buf.AppendLine();
            buf.Append(line);
        }
        if (buf.Length > 0)
            yield return new AttachmentTextChunk(0, buf.ToString(), $"text-lines {startLine}-{lineNo}");
    }

    private static int DetectHeaderRow(ExcelWorksheet worksheet, int colCount)
    {
        if (worksheet.Dimension.End.Row < 2) return 1;
        int row1NonEmpty = CountNonEmptyCells(worksheet, 1, colCount);
        int row2NonEmpty = CountNonEmptyCells(worksheet, 2, colCount);
        if (row1NonEmpty <= 1 && row2NonEmpty > 1) return 2;
        return 1;
    }

    private static int CountNonEmptyCells(ExcelWorksheet worksheet, int row, int colCount)
    {
        int count = 0;
        for (int col = 1; col <= colCount; col++)
        {
            if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col]?.Text))
                count++;
        }
        return count;
    }
}

/// <summary>附件解析产出的一批文本。</summary>
public sealed record AttachmentTextChunk(int Index, string Text, string SourceHint);

public class AttachmentFile
{
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = Array.Empty<byte>();
    /// <summary>PrepareForGate 已解析文本；有值时 GatePipeline 不再重复解析</summary>
    public string? PreExtractedText { get; set; }
    public string? AttachmentId { get; set; }
}
