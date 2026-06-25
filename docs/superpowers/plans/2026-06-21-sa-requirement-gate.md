# SA 需求门控 + 附件处理 — 实施计划

> **For agentic workers:** Use `superpowers:executing-plans` to implement this plan step-by-step.

**Goal:** 实现 RequirementGateService + AttachmentProcessor，修复清言报告 9 项问题，集成到 AIDevelopmentPipelineService

**Architecture:** 门控服务纯逻辑返回 DTO，Pipeline 服务编排 SSE 通信。附件处理 Extract → Validate → Evaluate → Prompt 四步流水线

**Tech Stack:** .NET 8 + EPPlus 5.8.4 + NPOI 2.7.2 + PdfPig 0.1.9 + SqlSugar

---

### Task 1: 安装 NuGet 包

**Files:** Modify `JNPF.InteAssistant.csproj`

- [ ] **Step 1: 添加三个包引用**

```bash
cd D:\JNPF-v52\backend\modularity\inteAssistant\JNPF.InteAssistant
dotnet add package EPPlus --version 5.8.4
dotnet add package NPOI --version 2.7.2
dotnet add package PdfPig --version 0.1.9
```

- [ ] **Step 2: 验证包安装成功**

```bash
dotnet restore
dotnet build --no-restore
```
Expected: 0 errors

---

### Task 2: 创建 AttachmentProcessor

**Files:** Create `Gates/AttachmentProcessor.cs`

- [ ] **Write the complete file**

完整代码见设计报告。关键修复：
- `ExcelPackage.LicenseContext` 设为 static 构造函数（#7）
- `XWPFDocument` 加 `using`（#2）
- `ProcessAttachmentsAsync` 去掉无意义 `async`（#5）
- `MemoryStream` 全部 `using` 保护

```csharp
using Microsoft.Extensions.Logging;
using NPOI.XWPF.UserModel;
using OfficeOpenXml;
using System.Text;
using UglyToad.PdfPig;

namespace JNPF.InteAssistant.Gates;

public class AttachmentProcessor : ITransient
{
    private readonly ILogger<AttachmentProcessor> _logger;
    private const int MaxExtractedLength = 30000;

    static AttachmentProcessor()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public AttachmentProcessor(ILogger<AttachmentProcessor> logger)
    {
        _logger = logger;
    }

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
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp"
                        => $"[附件：图片 {file.FileName}，需通过多模态模型提取]",
                    ".txt" or ".csv" => ExtractText(file.Content),
                    _ => $"[附件：{file.FileName}，格式{ext}暂不支持自动解析]"
                };
                if (!string.IsNullOrWhiteSpace(extracted))
                    parts.Add($"\n\n===== 附件：{file.FileName} =====\n{extracted}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "附件处理失败: {FileName}", file.FileName);
                parts.Add($"\n\n[附件 {file.FileName} 处理失败：{ex.Message}]");
            }
        }

        var result = string.Join("", parts);
        if (result.Length > MaxExtractedLength)
            result = result[..MaxExtractedLength] + "\n\n[... 内容过长，已截断 ...]";

        return Task.FromResult(result);
    }

    public bool HasImageAttachments(List<AttachmentFile> attachments)
    {
        return attachments?.Any(a =>
        {
            var ext = Path.GetExtension(a.FileName)?.ToLower();
            return ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
        }) ?? false;
    }

    private string ExtractExcel(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var package = new ExcelPackage(stream);
        var workbook = package.Workbook;
        if (workbook?.Worksheets == null || workbook.Worksheets.Count == 0) return "";

        var result = new StringBuilder();
        foreach (var worksheet in workbook.Worksheets)
        {
            result.AppendLine($"【Sheet: {worksheet.Name}】");
            if (worksheet.Dimension == null) { result.AppendLine("（空表）"); continue; }
            var rowCount = Math.Min(worksheet.Dimension.End.Row, 51);
            var colCount = Math.Min(worksheet.Dimension.End.Column, 20);
            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
            {
                var val = worksheet.Cells[1, col]?.Text?.Trim();
                headers.Add(string.IsNullOrWhiteSpace(val) ? $"列{col}" : val);
            }
            result.AppendLine(string.Join(" | ", headers));
            result.AppendLine(new string('-', headers.Sum(h => h.Length) + headers.Count * 3));
            for (int row = 2; row <= rowCount; row++)
            {
                var cells = new List<string>();
                for (int col = 1; col <= colCount; col++)
                    cells.Add(worksheet.Cells[row, col]?.Text?.Trim() ?? "");
                result.AppendLine(string.Join(" | ", cells));
            }
            if (worksheet.Dimension.End.Row > 51)
                result.AppendLine($"... 共{worksheet.Dimension.End.Row}行，仅显示前50行");
            result.AppendLine();
        }
        return result.ToString();
    }

    private string ExtractWord(byte[] content)
    {
        using var stream = new MemoryStream(content);
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
                var cells = row.Cells.Select(c => c.GetText()?.Trim() ?? "");
                result.AppendLine(string.Join(" | ", cells));
            }
        }
        return result.ToString();
    }

    private string ExtractPdf(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var result = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text)) result.AppendLine(text);
        }
        return result.ToString();
    }

    private string ExtractText(byte[] content)
    {
        return Encoding.UTF8.GetString(content);
    }
}

public class AttachmentFile
{
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}
```

---

### Task 3: 创建 RequirementGateService

**Files:** Create `Gates/RequirementGateService.cs`

- [ ] **Write the complete file**

完整代码见设计报告。关键修复：
- `JsonDocument.Parse` 加 `using`（#3）
- `StringContent` 加 `using`（#4）
- `HttpClient` 改 `HttpRequestMessage` per-request 认证（#8）
- `ExtractFromImages` 包含 `model` 参数（#1）

代码过长不在此重复，参考清言报告文件2的完整修复版。

---

### Task 4: 创建多模态 Vision 配置

**Files:** Create `Configurations/MultimodalVision.json`

```json
{
  "MultimodalVision": {
    "ApiUrl": "https://open.bigmodel.cn/api/paas/v4/chat/completions",
    "ApiKey": "your_actual_api_key_here",
    "Model": "glm-4v"
  }
}
```

---

### Task 5: 集成到 AIDevelopmentPipelineService

**Files:** Modify `AIDevelopmentPipelineService.cs`

- [ ] **在 StreamLlmResponseAsync 中插入门控逻辑**

在 LLM 调用之前（约第 357 行 ChatCompletionRequest 构造之前）插入门控代码。门控服务通过 DI scope 获取，只返回结果对象，Pipeline 自己发 SSE。

关键集成点：
1. 获取 AttachmentProcessor 和 RequirementGateService
2. 调用 ProcessAttachmentsAsync → 附件文本
3. 有图片时调用 ExtractFromImages → 图片分析文本
4. 调用 ValidateHardRules → 不通过则发 SSE 拒绝 + return
5. 调用 EvaluateMaturity → 发成熟度 SSE
6. 调用 GetSystemPrompt → 替换原 SystemPrompt
7. GetPipelineAttachmentsAsync 占位方法（三方案注释）

---

### Task 6: 编译验证

```bash
cd D:\JNPF-v52\backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj
```
Expected: 0 errors

---

### 自检清单

| # | 检查项 | 状态 |
|---|--------|------|
| 1 | NuGet 包安装 | pending |
| 2 | AttachmentProcessor 完整 + 修复 #2 #5 #7 | pending |
| 3 | RequirementGateService 完整 + 修复 #1 #3 #4 #6 #8 | pending |
| 4 | Configurations/MultimodalVision.json | pending |
| 5 | AIDevelopmentPipelineService 集成 | pending |
| 6 | dotnet build 0 errors | pending |
