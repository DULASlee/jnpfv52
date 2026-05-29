using JNPF.WorkFlow.Entitys.Model;
using JNPF.WorkFlow.Entitys.Model.Properties;
using Mapster;

namespace JNPF.WorkFlow.Entitys.Mapper;

internal class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<FlowTemplateJsonModel, TaskNodeModel>()
            .Map(dest => dest.upNodeId, src => src.prevId);
        config.ForType<ChildTaskProperties, ApproversProperties>()
            .Map(dest => dest.assigneeType, src => src.initiateType)
            .Map(dest => dest.approvers, src => src.initiator)
            .Map(dest => dest.extraRule, src => "1");
        config.ForType<StartProperties, ApproversProperties>()
            .Map(dest => dest.approvers, src => src.initiator);
    }
}
