using JNPF.Systems.Entitys.Model.DataInterFace;

namespace JNPF.Systems.Entitys.Model.System.DataInterFace;

public class DataInterfaceProperJson
{
    /// <summary>
    /// 静态数据.
    /// </summary>
    public string staticData { get; set; }

    /// <summary>
    /// sql数据.
    /// </summary>
    public SqlDataModel sqlData { get; set; }

    /// <summary>
    /// api数据.
    /// </summary>
    public ApiDataModel apiData { get; set; }

    /// <summary>
    /// 变量id.
    /// </summary>
    public List<string> variateIds { get; set; } = new List<string>();
}

public class SqlDataModel
{
    /// <summary>
    /// 链接id.
    /// </summary>
    public string dbLinkId { get; set; }

    /// <summary>
    /// SQL语句.
    /// </summary>
    public string sql { get; set; }
}

public class ApiDataModel
{
    /// <summary>
    /// 链接id.
    /// </summary>
    public string method { get; set; }

    /// <summary>
    /// 请求地址.
    /// </summary>
    public string url { get; set; }

    /// <summary>
    /// 头部参数.
    /// </summary>
    public List<DataInterfaceReqParameter> header { get; set; } = new List<DataInterfaceReqParameter>();

    /// <summary>
    /// 请求参数.
    /// </summary>
    public List<DataInterfaceReqParameter> query { get; set; } = new List<DataInterfaceReqParameter>();

    /// <summary>
    /// body参数.
    /// </summary>
    public string body { get; set; }

    /// <summary>
    /// body类型(0-none 1-form-data 2-x-www-form-urlencoded 3-json 4-xml ).
    /// </summary>
    public string bodyType { get; set; }

    /// <summary>
    /// 扩展参数.
    /// </summary>
    public List<DataInterfaceReqParameter> extraParameters { get; set; } = new List<DataInterfaceReqParameter>();
}
