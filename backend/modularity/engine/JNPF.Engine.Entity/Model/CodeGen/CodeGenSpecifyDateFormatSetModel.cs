namespace JNPF.Engine.Entity.Model.CodeGen;

/// <summary>
/// 代码生成指定日期格式集合模型.
/// </summary>
public class CodeGenSpecifyDateFormatSetModel
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// 格式.
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 子级.
    /// </summary>
    public List<CodeGenSpecifyDateFormatSetModel> Children { get; set; }
}