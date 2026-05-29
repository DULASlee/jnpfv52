using JNPF.VisualDev.Entitys.Dto.VisualDev;
using Mapster;

namespace JNPF.VisualDev.Entitys.Mapper;

public class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<VisualDevEntity, VisualDevSelectorOutput>()
            .Map(dest => dest.parentId, src => src.Category);
        config.ForType<VisualDevReleaseEntity, VisualDevSelectorOutput>()
            .Map(dest => dest.parentId, src => src.Category);
    }
}
