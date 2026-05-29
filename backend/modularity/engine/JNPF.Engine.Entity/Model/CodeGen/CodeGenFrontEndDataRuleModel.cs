using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成前端必填验证.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndDataRequiredModel : CodeGenFrontEndDataRuleBasics
{
    /// <summary>
    /// 必填.
    /// </summary>
    public bool required { get; set; }
}

/// <summary>
/// 代码生成前端正则校验.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndDataRuleModel : CodeGenFrontEndDataRuleBasics
{
    /// <summary>
    /// 正则表达式.
    /// </summary>
    public object pattern { get; set; }
}

/// <summary>
/// 代码生成前端数据校验基础.
/// </summary>
[SuppressSniffer]
public class CodeGenFrontEndDataRuleBasics
{
    /// <summary>
    /// 消息.
    /// </summary>
    public string message { get; set; }

    /// <summary>
    /// 触发.
    /// </summary>
    public object trigger { get; set; }
}