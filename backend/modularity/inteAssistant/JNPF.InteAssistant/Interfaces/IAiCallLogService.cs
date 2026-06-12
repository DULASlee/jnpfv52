using JNPF.InteAssistant.Entitys.Dto.InteAssistant;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// AI 调用日志服务接口
/// </summary>
public interface IAiCallLogService
{
    /// <summary>
    /// 获取调用日志列表
    /// </summary>
    Task<dynamic> GetList(AiCallLogListQueryInput input);

    /// <summary>
    /// 获取调用日志详情
    /// </summary>
    Task<dynamic> GetInfo(string id);
}
