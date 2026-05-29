using JNPF.WorkFlow.Entitys.Dto.FlowTemplate;

namespace JNPF.WorkFlow.Interfaces.Service;

/// <summary>
/// 流程设计.
/// </summary>
public interface IFlowTemplateService
{
    /// <summary>
    /// 发起列表.
    /// </summary>
    /// <returns></returns>
    Task<List<FlowTemplateTreeOutput>> GetFlowFormList(int flowType, string userId = null);
}
