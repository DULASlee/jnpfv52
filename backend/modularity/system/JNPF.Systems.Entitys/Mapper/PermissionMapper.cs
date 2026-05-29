using JNPF.Common.Models.User;
using JNPF.Systems.Entitys.Dto.Department;
using JNPF.Systems.Entitys.Dto.Group;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.OrganizeAdministrator;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using Mapster;

namespace JNPF.Systems.Entitys.Mapper;

/// <summary>
/// 权限模块对象映射 .
/// </summary>
public class PermissionMapper : IRegister
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
        config.ForType<UserEntity, UserInfoOutput>()
             .Map(dest => dest.headIcon, src => "/api/File/Image/userAvatar/" + src.HeadIcon);
        config.ForType<UserEntity, UserSelectorOutput>()
            .Map(dest => dest.fullName, src => src.RealName + "/" + src.Account)
            .Map(dest => dest.type, src => "user")
            .Map(dest => dest.parentId, src => src.OrganizeId);
        config.ForType<OrganizeEntity, UserSelectorOutput>()
            .Map(dest => dest.type, src => src.Category)
            .Map(dest => dest.icon, src => "icon-ym icon-ym-tree-organization3");
        config.ForType<OrganizeEntity, DepartmentSelectorOutput>()
             .Map(dest => dest.type, src => src.Category);
        config.ForType<GroupCrInput, GroupEntity>()
             .Map(dest => dest.Category, src => src.type);
        config.ForType<GroupEntity, GroupUpInput>()
             .Map(dest => dest.type, src => src.Category);
        config.ForType<OrganizeAdminIsTratorCrInput, OrganizeAdministratorEntity>()
            .Ignore(dest => dest.UserId);
        config.ForType<OrganizeEntity, DepartmentInfoOutput>()
            .Ignore(dest => dest.organizeIdTree);
        config.ForType<OrganizeEntity, OrganizeInfoOutput>()
            .Ignore(dest => dest.organizeIdTree);
    }
}