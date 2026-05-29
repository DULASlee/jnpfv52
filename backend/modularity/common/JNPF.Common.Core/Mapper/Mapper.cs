using JNPF.Common.Models.User;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.TaskScheduler.Entitys;
using Mapster;

namespace JNPF.Common.Core.Mapper;

/// <summary>
/// 对象映射.
/// </summary>
public class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<UserEntity, UserInfoModel>()
           .Map(dest => dest.userId, src => src.Id)
           .Map(dest => dest.userAccount, src => src.Account)
           .Map(dest => dest.userName, src => src.RealName)
           .Map(dest => dest.headIcon, src => "/api/File/Image/userAvatar/" + src.HeadIcon)
           .Map(dest => dest.prevLoginTime, src => src.PrevLogTime)
           .Map(dest => dest.prevLoginIPAddress, src => src.PrevLogIP);
        config.ForType<TriggerTimeline, TimeTaskEntity>()
            .Map(dest => dest.LastRunTime, src => src.LastRunTime)
            .Map(dest => dest.NextRunTime, src => src.NextRunTime)
            .Map(dest => dest.RunCount, src => src.NumberOfRuns);
    }
}