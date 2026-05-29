using JNPF.Common.Security;
using JNPF.TaskScheduler.Entitys.Dto.TaskScheduler;
using JNPF.TaskScheduler.Entitys.Model;
using Mapster;

namespace JNPF.TaskScheduler.Entitys.Mapper;

public class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<TimeTaskEntity, TimeTaskListOutput>()
            .Map(dest => dest.startTime, src => src.ExecuteContent.ToObject<ContentModel>().startTime)
            .Map(dest => dest.endTime, src => src.ExecuteContent.ToObject<ContentModel>().endTime);
    }
}
