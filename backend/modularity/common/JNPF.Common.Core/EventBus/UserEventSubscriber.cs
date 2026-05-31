using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.Systems.Entitys.Model.Permission.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using SqlSugar;

namespace JNPF.EventHandler;

/// <summary>
/// 用户事件订阅.
/// </summary>
public class UserEventSubscriber : IEventSubscriber, ISingleton
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private readonly ISqlSugarClient _sqlSugarClient;

    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public UserEventSubscriber(
        ISqlSugarClient sqlSugarClient,
        ITenantManager tenantManager)
    {
        _sqlSugarClient = sqlSugarClient;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 修改用户登录信息.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("User:UpdateUserLogin")]
    public async Task UpdateUserLoginInfo(EventHandlerExecutingContext context)
    {
        var log = (UserEventSource)context.Source;
        var db = _sqlSugarClient.CopyNew();
        if (KeyVariable.MultiTenancy) await _tenantManager.ChangTenant(db, log.TenantId);

        await db.Updateable(log.Entity).UpdateColumns(m => new { m.FirstLogIP, m.FirstLogTime, m.PrevLogTime, m.PrevLogIP, m.LastLogTime, m.LastLogIP, m.LogSuccessCount }).ExecuteCommandAsync();
    }

    /// <summary>
    /// 单点登录同步用户信息.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("User:Maxkey_Identity")]
    public async Task ReceiveUserInfo(EventHandlerExecutingContext context)
    {
        var log = context.Source.Payload;
        await Receive(log.ToString());
    }

    /// <summary>
    /// 根据单点服务端消息 同步用户信息到数据库.
    /// </summary>
    /// <param name="message"></param>
    private async Task<bool> Receive(string message)
    {
        bool isSuccess;
        var map = new Dictionary<string, object>();
        try
        {
            var mqMessage = message.ToObject<MqMessage>();

            // 转成用户实体类
            var userInfo = mqMessage.content.ToObject<UserInfo>();
            var userEntity = new UserEntity();
            userEntity.Id = userInfo.id;
            userEntity.Account = userInfo.username;
            userEntity.MobilePhone = userInfo.mobile;
            userEntity.Email = userInfo.email;
            userEntity.Gender = userInfo.gender;
            userEntity.CreatorTime = userInfo.createdDate.IsNullOrWhiteSpace() ? null : userInfo.createdDate?.ParseToLong().TimeStampToDateTime();
            userEntity.CreatorUserId = userInfo.createdBy;
            userEntity.LastModifyUserId = userInfo.modifiedBy;
            userEntity.LastModifyTime = userInfo.modifiedDate.IsNullOrWhiteSpace() ? null : userInfo.modifiedDate?.ParseToLong().TimeStampToDateTime();
            userEntity.RealName = userInfo.displayName;
            userEntity.LogSuccessCount = userInfo.loginCount;
            userEntity.LogErrorCount = userInfo.badPasswordCount;
            userEntity.LastLogIP = userInfo.lastLoginIp;
            userEntity.LastLogTime = userInfo.lastLoginTime.IsNullOrWhiteSpace() ? null : userInfo.lastLoginTime?.ParseToLong().TimeStampToDateTime();
            userEntity.EnabledMark = userInfo.status == 1 ? 1 : 0;
            userEntity.HeadIcon = "001.png";

            var db = _sqlSugarClient.CopyNew();
            if (KeyVariable.MultiTenancy) await _tenantManager.ChangTenant(db, userInfo.instId);

            isSuccess = await process(db, userEntity, mqMessage.actionType, userInfo.instId);
        }
        catch (Exception)
        {
            // _logger.error("同步用户失败", e);
            isSuccess = false;
        }

        if (!isSuccess)
        {
            // _logger.info("消息消费失败：" + message);
        }
        else
        {
            // _logger.debug("同步用户信息, {}", JSONObject.toJSONString(map));
        }

        return isSuccess;
    }

    /// <summary>
    /// 保存到数据库处理逻辑.
    /// </summary>
    /// <param name="actionType"></param>
    /// <param name="instId"></param>
    /// <returns></returns>
    private async Task<bool> process(ISqlSugarClient db, UserEntity entity, string actionType, string instId)
    {
        if (actionType.Equals("CREATE_ACTION"))
        {
            if (db.Queryable<UserEntity>().Any(x => x.Account.Equals(entity.Account) && x.DeleteMark == null)) return true;
            entity.Secretkey = Guid.NewGuid().ToString();

            var defaultPassWord = await db.Queryable<SysConfigEntity>()
                .Where(it => it.Key.Equals("newUserDefaultPassword"))
                .Select(it => it.Value)
                .FirstAsync();
            entity.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt(defaultPassWord) + entity.Secretkey);

            UserRelationEntity? entityRelation = new UserRelationEntity();
            entityRelation.Id = SnowflakeIdHelper.NextId();
            entityRelation.ObjectType = "Organize";
            entityRelation.ObjectId = db.Queryable<OrganizeEntity>().First(x => x.ParentId.Equals("-1")).Id;
            entityRelation.SortCode = 0;
            entityRelation.UserId = entity.Id;
            entityRelation.CreatorTime = DateTime.Now;
            entityRelation.CreatorUserId = entity.CreatorUserId;
            db.Insertable(entityRelation).ExecuteCommand(); // 批量新增用户关系

            // 新增用户记录
            return await db.Insertable(entity).CallEntityMethod(m => m.Create()).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync() > 0;
        }
        else if (actionType.Equals("UPDATE_ACTION"))
        {
            var oldEntity = await db.Queryable<UserEntity>().FirstAsync(x => x.Account.Equals(entity.Account) && x.DeleteMark == null);
            entity.Id = oldEntity.Id;
            return await db.Updateable(entity).CallEntityMethod(m => m.LastModify()).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync() > 0;
        }
        else if (actionType.Equals("DELETE_ACTION"))
        {
            var oldEntity = await db.Queryable<UserEntity>().FirstAsync(x => x.Account.Equals(entity.Account) && x.DeleteMark == null);
            oldEntity.EnabledMark = 0;

            // 同步删除用户 只能 该状态为 ： 禁用
            return await db.Updateable(oldEntity).CallEntityMethod(m => m.LastModify()).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync() > 0;

        }
        else if (actionType.Equals("PASSWORD_ACTION"))
        {
            return await db.Updateable<UserEntity>().SetColumns(it => new UserEntity()
            {
                Password = entity.Password,
                ChangePasswordDate = SqlFunc.GetDate(),
                LastModifyUserId = entity.LastModifyUserId,
                LastModifyTime = SqlFunc.GetDate()
            }).Where(it => it.Id == entity.Id).ExecuteCommandAsync() > 0;
        }
        else
        {
            //_logger.info("Other Action , will sikp it ...");
        }

        return true;
    }
}