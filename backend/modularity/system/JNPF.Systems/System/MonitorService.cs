using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.Monitor;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace JNPF.Systems;

/// <summary>
/// 系统监控
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Monitor", Order = 215)]
[Route("api/system/[controller]")]
public class MonitorService : IDynamicApiController, ITransient
{
    #region Get

    /// <summary>
    /// 系统监控.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public dynamic GetInfo()
    {
        var flag_Linux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        var flag_Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        MonitorOutput output = new MonitorOutput();
        if (flag_Linux)
        {
            output.system = MachineHelper.GetSystemInfo_Linux();
            output.cpu = MachineHelper.GetCpuInfo_Linux();
            output.memory = MachineHelper.GetMemoryInfo_Linux();
            output.disk = MachineHelper.GetDiskInfo_Linux();
        }
        else if (flag_Windows)
        {
            output.system = MachineHelper.GetSystemInfo_Windows();
            output.cpu = MachineHelper.GetCpuInfo_Windows();
            output.memory = MachineHelper.GetMemoryInfo_Windows();
            output.disk = MachineHelper.GetDiskInfo_Windows();
        }

        output.time = DateTime.Now;
        return output;
    }

    #endregion
}