using JNPF.Common.Dtos.Datainterface;
using JNPF.Common.Models;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 数据接口
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IDataInterfaceService
{
    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    Task<DataInterfaceEntity> GetInfo(string id);

    /// <summary>
    /// 根据不同类型请求接口.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type">0 ： 分页 1 ：详情 ，其他 原始.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<object> GetResponseByType(string id, int type, DataInterfacePreviewInput input);

    /// <summary>
    /// 处理远端数据.
    /// </summary>
    /// <param name="key">缓存标识.</param>
    /// <param name="propsUrl">远端数据ID.</param>
    /// <param name="value">指定选项标签为选项对象的某个属性值.</param>
    /// <param name="label">指定选项的值为选项对象的某个属性值.</param>
    /// <param name="children">指定选项的子选项为选项对象的某个属性值.</param>
    /// <param name="linkageParameters">联动参数.</param>
    /// <returns></returns>
    Task<List<StaticDataModel>> GetDynamicList(string key, string propsUrl, string value, string label, string children, List<DataInterfaceParameter> linkageParameters = null);

    /// <summary>
    /// 获取数据接口数据.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <param name="type"></param>
    /// <param name="isEcho"></param>
    /// <returns></returns>
    Task<object> GetDataInterfaceData(string id, DataInterfacePreviewInput input, int type, bool isEcho = false);

    Task<object> ExcuteInterfaceData(string interfaceId, string dataId, string formdata);

    /// <summary>
    /// 处理接口参数.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Dictionary<string, string> GetDatainterfaceParameter(DataInterfacePreviewInput input);
}