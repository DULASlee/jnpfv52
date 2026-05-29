using JNPF.Common.Models;
using JNPF.Common.Models.VisualDev;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using Mapster;

namespace JNPF.VisualDev.Engine.Mapper;

internal class VisualDev : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<FieldsModel, ListSearchParametersModel>()
           .Map(dest => dest.jnpfKey, src => src.__config__.jnpfKey)
           .Map(dest => dest.format, src => src.format)
           .Map(dest => dest.multiple, src => src.multiple)
           .Map(dest => dest.searchType, src => src.searchType)
           .Map(dest => dest.vModel, src => src.__vModel__);
        config.ForType<CodeGenFieldsModel, FieldsModel>()
           .Map(dest => dest.__config__, src => src.__config__.ToObject<ConfigModel>())
           .Map(dest => dest.props, src => string.IsNullOrEmpty(src.props) ? null : src.props.ToObject<CodeGenPropsBeanModel>())
           .Map(dest => dest.options, src => string.IsNullOrEmpty(src.options) ? null : src.options.ToObject<List<object>>())
           .Map(dest => dest.ableIds, src => string.IsNullOrEmpty(src.ableIds) ? null : src.ableIds.ToObject<List<string>>());
    }
}