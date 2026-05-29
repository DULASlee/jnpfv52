using JNPF.Common.Models.User;
using JNPF.Message.Entitys.Dto.IM;
using JNPF.Message.Entitys.Dto.MessageAccount;
using JNPF.Message.Entitys.Dto.Notice;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Entitys.Model.IM;
using Mapster;

namespace JNPF.Message.Entitys.Mapper;

public class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<IMContentEntity, IMUnreadNumModel>()
            .Map(dest => dest.unreadNum, src => src.EnabledMark);
        config.ForType<UserOnlineModel, OnlineUserListOutput>()
          .Map(dest => dest.userId, src => src.userId)
          .Map(dest => dest.userAccount, src => src.account)
          .Map(dest => dest.userName, src => src.userName)
          .Map(dest => dest.loginTime, src => src.lastTime)
          .Map(dest => dest.loginIPAddress, src => src.lastLoginIp)
          .Map(dest => dest.loginPlatForm, src => src.lastLoginPlatForm);
        config.ForType<NoticeInput, NoticeEntity>()
          .Map(dest => dest.Type, src => src.remindCategory)
          .Map(dest => dest.Description, src => src.excerpt);
        config.ForType<MessageAccountListOutput, MessageAccountEntity>()
       .Map(dest => dest.Category, src => src.type);
    }
}