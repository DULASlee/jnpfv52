using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems;

/// <summary>
/// 常用菜单数据（App端 stub，返回空集合）
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "MenuData", Order = 299)]
[Route("api/system/[controller]")]
public class MenuDataService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 获取常用菜单列表
    /// </summary>
    [HttpGet]
    public dynamic GetList()
    {
        return new { list = new List<object>(), pagination = new { total = 0 } };
    }

    /// <summary>
    /// 获取应用数据列表
    /// </summary>
    [HttpPost("getAppDataList")]
    public dynamic GetAppDataList([FromBody] dynamic body)
    {
        return new { list = new List<object>(), pagination = new { total = 0 } };
    }

    /// <summary>
    /// 获取数据列表
    /// </summary>
    [HttpPost("getDataList")]
    public dynamic GetDataList([FromBody] dynamic body)
    {
        return new { list = new List<object>(), pagination = new { total = 0 } };
    }

    /// <summary>
    /// 添加常用菜单
    /// </summary>
    [HttpPost("{id}")]
    public IActionResult AddUsual(string id)
    {
        return new OkResult();
    }

    /// <summary>
    /// 删除常用菜单
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult DelUsual(string id)
    {
        return new OkResult();
    }
}
