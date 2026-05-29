using JNPF.DependencyInjection;
using SqlSugar;

namespace JNPF.Common.Security;

/// <summary>
/// 代码生成数据权限帮助类.
/// </summary>
[SuppressSniffer]
public static class CodeGenAuthorizeHelper
{
    /// <summary>
    /// 逆转数据转换别名.
    /// </summary>
    public static string ReverseDataConversion(List<IConditionalModel> conditionalModels, string tableName, string tableNumber)
    {
        var pvalue = conditionalModels.ToJsonString();
        return pvalue.Replace(tableName, tableNumber);
    }
}