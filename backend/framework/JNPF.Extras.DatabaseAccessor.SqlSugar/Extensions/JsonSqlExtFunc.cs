using SqlSugar;

namespace JNPF.Extras.DatabaseAccessor.SqlSugar.Extensions;

/// <summary>
/// Newtonsoft.Json 序列化拓展.
/// </summary>
public static class JsonSqlExtFunc
{
    /// <summary>
    /// 
    /// </summary>
    public static List<SqlFuncExternal> ExpMethods = new List<SqlFuncExternal>
    {
        new SqlFuncExternal(){
            UniqueMethodName = "ToObject",
            MethodValue = (expInfo, dbType, expContext) =>
                {
                    //var value = string.Empty;
                    return null;
                }
        }
    };

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static T ToObject<T>(string value)
    {
        //这里不能写任何实现代码，需要在上面的配置中实现
        throw new NotSupportedException("Can only be used in expressions");
    }
}
