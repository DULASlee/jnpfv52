namespace JNPF.VisualDev.Runtime;

using Newtonsoft.Json;

/// <summary>
/// 平台条件模型 — SqlSugar.WhereType 平台等价（裁决 C，值逐项对齐保证序列化等价）.
/// </summary>
public enum CompileWhereType
{
    And = 0,
    Or = 1,
    Null = -1,
}

/// <summary>
/// 平台条件模型 — SqlSugar.ConditionalType 平台等价（裁决 C，值逐项对齐保证序列化等价）.
/// </summary>
public enum CompileConditionalType
{
    Equal = 0,
    Like = 1,
    GreaterThan = 2,
    GreaterThanOrEqual = 3,
    LessThan = 4,
    LessThanOrEqual = 5,
    In = 6,
    NotIn = 7,
    LikeLeft = 8,
    LikeRight = 9,
    NoEqual = 10,
    IsNullOrEmpty = 11,
    IsNot = 12,
    NoLike = 13,
    EqualNull = 14,
    InLike = 15,
}

/// <summary>
/// 平台条件模型标记接口 — SqlSugar.IConditionalModel 平台等价（空标记）.
/// </summary>
public interface ICompileConditionalModel
{
}

/// <summary>
/// 平台条件模型 — SqlSugar.ConditionalModel 平台等价.
/// 属性名/声明顺序与 SqlSugar 对齐（Newtonsoft 默认序列化=声明序，往返等价单测守护）.
/// CustomConditionalFunc/CustomParameterValue 以 object 占位对齐序列化形态
/// （全仓零消费，事实见裁决 C 记录；类型不引 SqlSugar）.
/// </summary>
public class CompileConditionalModel : ICompileConditionalModel
{
    /// <summary>
    /// 字段名.
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// 字段值.
    /// </summary>
    public string FieldValue { get; set; }

    /// <summary>
    /// C# 类型名（datetime/decimal 等渲染提示）.
    /// </summary>
    public string CSharpTypeName { get; set; }

    /// <summary>
    /// 自定义条件函数占位（全仓零消费；仅对齐序列化形态）.
    /// </summary>
    public object CustomConditionalFunc { get; set; }

    /// <summary>
    /// 自定义条件参数占位（全仓零消费；仅对齐序列化形态）.
    /// </summary>
    public object CustomParameterValue { get; set; }

    /// <summary>
    /// 条件类型.
    /// </summary>
    public CompileConditionalType ConditionalType { get; set; }

    /// <summary>
    /// 字段值转换函数（签名与 SqlSugar 实测对齐：Func&lt;string, object&gt;）.
    /// [JsonIgnore] 与 SqlSugar 实测对齐：置位与否均不入 JSON（委托不可序列化）.
    /// </summary>
    [JsonIgnore]
    public Func<string, object> FieldValueConvertFunc { get; set; }
}

/// <summary>
/// 平台条件模型 — SqlSugar.ConditionalCollections 平台等价（同层条件组）.
/// </summary>
public class CompileConditionalCollections : ICompileConditionalModel
{
    /// <summary>
    /// 条件列表（Key=连接类型，Value=条件）.
    /// </summary>
    public List<KeyValuePair<CompileWhereType, CompileConditionalModel>> ConditionalList { get; set; }
}

/// <summary>
/// 平台条件模型 — SqlSugar.ConditionalTree 平台等价（嵌套条件树）.
/// </summary>
public class CompileConditionalTree : ICompileConditionalModel
{
    /// <summary>
    /// 条件列表（Key=连接类型，Value=条件/子树）.
    /// </summary>
    public List<KeyValuePair<CompileWhereType, ICompileConditionalModel>> ConditionalList { get; set; }
}
