using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Ir;

namespace JNPF.InteAssistant.Skills.Cognitive.Mcp.Tools;

/// <summary>
/// ioi.validate——包装 IIoiValidatorService，对 EventSpec payload 做 IOI 不变量裁决。
/// 校验不通过属于"工具成功执行、裁决为否"，以 valid=false 返回而非工具失败。
/// </summary>
public sealed class IoiValidateTool : IMcpToolHandler, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IIoiValidatorService _ioiValidator;

    public IoiValidateTool(IIoiValidatorService ioiValidator) => _ioiValidator = ioiValidator;

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "ioi.validate",
        Description = "对 EventSpec payload 执行 IOI 输入输出不变量校验",
        ArgumentsSchema = """{"eventSpecPayload":"string 必填，EventSpec JSON"}""",
    };

    public Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        var payload = KgSearchSeedsTool.ReadStringArg(argumentsJson, "eventSpecPayload");
        if (string.IsNullOrWhiteSpace(payload))
            return Task.FromResult(McpToolResult.Fail("ioi.validate 缺少 eventSpecPayload 参数"));

        try
        {
            _ioiValidator.Validate(payload);
            return Task.FromResult(McpToolResult.Ok(
                JsonSerializer.Serialize(new { valid = true }, JsonOptions)));
        }
        catch (Exception ex)
        {
            // IoiValidatorService 以 Oops.Bah 表达校验不通过——转译为裁决结果
            return Task.FromResult(McpToolResult.Ok(
                JsonSerializer.Serialize(new { valid = false, reason = ex.Message }, JsonOptions)));
        }
    }
}
