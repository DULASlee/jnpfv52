using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.Email;
using JNPF.Extras.Thirdparty.Email;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Web;

namespace JNPF.Extend;

/// <summary>
/// 邮件收发
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "Email", Order = 600)]
[Route("api/extend/[controller]")]
public class EmailService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<EmailReceiveEntity> _repository;
    private readonly ITenant _db;
    private readonly IUserManager _userManager;
    private readonly IFileManager _fileManager;

    public EmailService(ISqlSugarRepository<EmailReceiveEntity> repository, IUserManager userManager, ISqlSugarClient context, IFileManager fileManager)
    {
        _repository = repository;
        _userManager = userManager;
        _db = context.AsTenant();
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// (带分页)获取邮件列表(收件箱、标星件、草稿箱、已发送).
    /// </summary>
    /// <param name="input">请求参数</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] EmailListQuery input)
    {
        switch (input.type)
        {
            case "inBox"://收件箱
                return await GetReceiveList(input);
            case "star"://标星件
                return await GetStarredList(input);
            case "draft"://草稿箱
                return await GetDraftList(input);
            case "sent"://已发送
                return await GetSentList(input);
            default:
                return PageResult<EmailListOutput>.SqlSugarPageResult(new SqlSugarPagedList<EmailListOutput>());
        }
    }

    /// <summary>
    /// 信息（收件/发件）.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var output = new EmailInfoOutput();
        var data = await GetInfo(id);
        var jobj = data.ToObject<JObject>();
        if (jobj.ContainsKey("Read"))
        {
            var entity = data.Adapt<EmailReceiveEntity>();
            output = entity.Adapt<EmailInfoOutput>();
            output.bodyText = HttpUtility.HtmlDecode(entity.BodyText);
        }
        else
        {
            var entity = data.Adapt<EmailSendEntity>();
            output = entity.Adapt<EmailInfoOutput>();
            output.bodyText = HttpUtility.HtmlDecode(entity.BodyText);
        }
        return output;
    }

    /// <summary>
    /// 信息（配置）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Config")]
    public async Task<dynamic> GetConfigInfo_Api()
    {
        return (await GetConfigInfo()).Adapt<EmailConfigInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        try
        {
            _db.BeginTran();

            var entity = await GetInfo(id);
            if (entity is EmailReceiveEntity)
            {
                //删除邮件
                var mailConfig = await GetConfigInfo();
                var mailReceiveEntity = entity as EmailReceiveEntity;
                MailUtil.Delete(new MailParameterInfo { Account = mailConfig.Account, Password = mailConfig.Password, POP3Host = mailConfig.POP3Host, POP3Port = mailConfig.POP3Port.ParseToInt() }, mailReceiveEntity.MID);
            }
            //删除数据
            var isOk = false;
            if (entity is EmailReceiveEntity)
                isOk = await _repository.AsSugarClient().Updateable((EmailReceiveEntity)entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
            else
                isOk = await _repository.AsSugarClient().Updateable((EmailSendEntity)entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();
            if (!isOk)
                throw Oops.Oh(ErrorCode.COM1002);

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1002);
        }
    }

    /// <summary>
    /// 设置已读邮件.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Read")]
    public async Task ReceiveRead(string id)
    {
        var isOk = await _repository.AsSugarClient().Updateable<EmailReceiveEntity>().SetColumns(it => new EmailReceiveEntity()
        {
            Read = 1,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 设置未读邮件.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Unread")]
    public async Task ReceiveUnread(string id)
    {
        var isOk = await _repository.AsSugarClient().Updateable<EmailReceiveEntity>().SetColumns(it => new EmailReceiveEntity()
        {
            Read = 0,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 设置星标邮件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Star")]
    public async Task ReceiveYesStarred(string id)
    {
        var isOk = await _repository.AsSugarClient().Updateable<EmailReceiveEntity>().SetColumns(it => new EmailReceiveEntity()
        {
            Starred = 1,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 设置取消星标.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Unstar ")]
    public async Task ReceiveNoStarred(string id)
    {
        var isOk = await _repository.AsSugarClient().Updateable<EmailReceiveEntity>().SetColumns(it => new EmailReceiveEntity()
        {
            Starred = 0,
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id.Equals(id)).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 收邮件.
    /// </summary>
    /// <returns></returns>
    [HttpPost("Receive")]
    public async Task<dynamic> Receive()
    {
        var mailConfig = await GetConfigInfo();
        if (mailConfig != null)
        {
            var mailAccount = mailConfig.Adapt<MailParameterInfo>();
            if (MailUtil.CheckConnected(mailAccount))
            {
                new List<EmailReceiveEntity>();
                var startTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " 00:00");
                var endTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " 23:59");
                var receiveCount = await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().CountAsync(x => x.MAccount == mailConfig.Account && SqlFunc.Between(x.CreatorTime, startTime, endTime));
                List<EmailReceiveEntity> entitys = MailUtil.Get(mailAccount, receiveCount).Select(item => new EmailReceiveEntity
                {
                    MAccount = mailConfig.Account,
                    MID = item.UID,
                    Sender = item.To,
                    SenderName = item.ToName,
                    Subject = item.Subject,
                    BodyText = HttpUtility.HtmlEncode(item.BodyText),
                    Attachment = item.Attachment.ToJsonString(),
                    Date = item.Date,
                    Read = 0
                }).ToList();

                if (entitys.Count > 0)
                {
                    await _repository.AsSugarClient().Insertable(entitys).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }

                return entitys.Count;
            }
            else
            {
                throw Oops.Oh(ErrorCode.Ex0003);
            }
        }
        else
        {
            throw Oops.Oh(ErrorCode.Ex0004);
        }
    }

    /// <summary>
    /// 存草稿.
    /// </summary>
    /// <param name="input">对象实体.</param>
    /// <returns></returns>
    [HttpPost("Actions/SaveDraft")]
    public async Task SaveDraft([FromBody] EmailActionsSaveDraftInput input)
    {
        var entity = input.Adapt<EmailSendEntity>();
        entity.BodyText = HttpUtility.HtmlEncode(entity.BodyText);
        entity.To = input.recipient;
        entity.Sender = App.GetConfig<AppOptions>("JNPF_App", true).ErrorReportTo;
        var isOk = 0;
        entity.State = -1;
        if (entity.Id.IsEmpty())
        {
            isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        else
        {
            isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }

        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 发邮件.
    /// </summary>
    /// <param name="input">对象实体.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task SaveSent([FromBody] EmailSendInput input)
    {
        var entity = input.Adapt<EmailSendEntity>();
        var mailConfig = await GetConfigInfo();
        foreach (var item in input.recipient.Split(","))
        {
            if (!item.IsEmail())
                throw Oops.Oh(ErrorCode.Ex0003);
            if (mailConfig != null)
            {
                entity.BodyText = HttpUtility.HtmlEncode(entity.BodyText);
                entity.To = item;
                entity.Sender = App.GetConfig<AppOptions>("JNPF_App", true).ErrorReportTo;
                entity.State = 1;
                var isOk = 0;
                if (entity.Id.IsEmpty())
                {
                    entity.Sender = mailConfig.Account;
                    isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
                else
                {
                    isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
                }
                //拷贝文件,注意：从临时文件夹拷贝到邮件文件夹
                var attachmentList = entity.Attachment.ToList<MailFileParameterInfo>();
                var temporaryFile = FileVariable.TemporaryFilePath;
                var mailFilePath = FileVariable.EmailFilePath;
                foreach (MailFileParameterInfo mailFile in attachmentList)
                {
                    FileHelper.MoveFile(Path.Combine(temporaryFile, mailFile.fileId), Path.Combine(mailFilePath, mailFile.fileId));
                    mailFile.fileName = mailFile.name;
                }
                //发送邮件
                var mailModel = new MailInfo();
                mailModel.To = entity.To;
                mailModel.CC = entity.CC;
                mailModel.Bcc = entity.BCC;
                mailModel.Subject = entity.Subject;
                mailModel.BodyText = HttpUtility.HtmlDecode(entity.BodyText);
                mailModel.Attachment = attachmentList;
                MailUtil.Send(new MailParameterInfo { AccountName = mailConfig.SenderName, Account = mailConfig.Account, Password = mailConfig.Password, SMTPHost = mailConfig.SMTPHost, SMTPPort = mailConfig.SMTPPort.ParseToInt(), Ssl = mailConfig.Ssl == 1 }, mailModel);
                if (isOk < 1)
                    throw Oops.Oh(ErrorCode.COM1008);
            }
            else
            {
                throw Oops.Oh(ErrorCode.Ex0004);
            }
        }
    }

    /// <summary>
    /// 保存邮箱配置
    /// </summary>
    /// <param name="input">对象实体</param>
    /// <returns></returns>
    [HttpPut("Config")]
    public async Task SaveConfig([FromBody] EmailConfigUpInput input)
    {
        var entity = input.Adapt<EmailConfigEntity>();
        var data = await GetConfigInfo();
        var isOk = 0;
        if (data == null)
        {
            isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
        else
        {
            entity.Id = data.Id;
            isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        }
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1008);
    }

    /// <summary>
    /// 邮箱账户密码验证.
    /// </summary>
    /// <param name="input">对象实体</param>
    [HttpPost("Config/Actions/CheckMail")]
    public void CheckLogin([FromBody] EmailConfigActionsCheckMailInput input)
    {
        var entity = input.Adapt<EmailConfigEntity>();
        if (!MailUtil.CheckConnected(entity.Adapt<MailParameterInfo>()))
            throw Oops.Oh(ErrorCode.Ex0003);
    }

    /// <summary>
    /// 下载附件.
    /// </summary>
    /// <param name="fileModel">文件对象</param>
    [HttpPost("Download")]
    public async Task Download(AnnexModel fileModel)
    {
        var filePath = Path.Combine(FileVariable.EmailFilePath, fileModel.FileId);
        if (await _fileManager.ExistsFile(filePath))
        {
            _fileManager.DownloadFileByType(filePath, fileModel.FileName);
        }
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 信息（配置）.
    /// </summary>
    /// <returns></returns>
    private async Task<EmailConfigEntity> GetConfigInfo()
    {
        return await _repository.AsSugarClient().Queryable<EmailConfigEntity>().FirstAsync(x => x.CreatorUserId == _userManager.UserId);
    }

    /// <summary>
    /// 列表（收件箱）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<dynamic> GetReceiveList(EmailListQuery input)
    {
        var whereLambda = LinqExpression.And<EmailReceiveEntity>();
        whereLambda = whereLambda.And(x => x.CreatorUserId == _userManager.UserId && x.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.Date, input.startTime, input.endTime));
        }
        if (!string.IsNullOrEmpty(input.keyword))
        {
            whereLambda = whereLambda.And(m => m.Sender.Contains(input.keyword) || m.Subject.Contains(input.keyword));
        }
        var list = await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().Where(whereLambda)
            .OrderBy(x => x.Date, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<EmailListOutput>()
        {
            list = list.list.Adapt<List<EmailListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<EmailListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 列表（未读邮件）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<dynamic> GetUnreadList(EmailListQuery input)
    {
        var whereLambda = LinqExpression.And<EmailReceiveEntity>();
        whereLambda = whereLambda.And(x => x.CreatorUserId == _userManager.UserId && x.Read == 0 && x.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.CreatorTime, input.startTime, input.endTime));
        }
        if (!string.IsNullOrEmpty(input.keyword))
        {
            whereLambda = whereLambda.And(m => m.Sender.Contains(input.keyword) || m.Subject.Contains(input.keyword));
        }
        var list = await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().Where(whereLambda).OrderBy(x => x.Date, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<EmailListOutput>()
        {
            list = list.list.Adapt<List<EmailListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<EmailListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 列表（星标件）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<dynamic> GetStarredList(EmailListQuery input)
    {
        var whereLambda = LinqExpression.And<EmailReceiveEntity>();
        whereLambda = whereLambda.And(x => x.CreatorUserId == _userManager.UserId && x.Starred == 1 && x.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.CreatorTime, input.startTime, input.endTime));
        }
        //关键字（用户、IP地址、功能名称）
        if (!string.IsNullOrEmpty(input.keyword))
        {
            whereLambda = whereLambda.And(m => m.Sender.Contains(input.keyword) || m.Subject.Contains(input.keyword));
        }
        var list = await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().Where(whereLambda).OrderBy(x => x.Date, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<EmailListOutput>()
        {
            list = list.list.Adapt<List<EmailListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<EmailListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 列表（草稿箱）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<dynamic> GetDraftList(EmailListQuery input)
    {
        var whereLambda = LinqExpression.And<EmailSendEntity>();
        whereLambda = whereLambda.And(x => x.CreatorUserId == _userManager.UserId && x.State == -1 && x.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.CreatorTime, input.startTime, input.endTime));
        }
        if (!string.IsNullOrEmpty(input.keyword))
        {
            whereLambda = whereLambda.And(m => m.Sender.Contains(input.keyword) || m.Subject.Contains(input.keyword));
        }
        var list = await _repository.AsSugarClient().Queryable<EmailSendEntity>().Where(whereLambda).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<EmailListOutput>()
        {
            list = list.list.Adapt<List<EmailListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<EmailListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 列表（已发送）.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private async Task<dynamic> GetSentList(EmailListQuery input)
    {
        var whereLambda = LinqExpression.And<EmailSendEntity>();
        whereLambda = whereLambda.And(x => x.CreatorUserId == _userManager.UserId && x.State != -1 && x.DeleteMark == null);
        if (input.endTime != null && input.startTime != null)
        {
            whereLambda = whereLambda.And(x => SqlFunc.Between(x.CreatorTime, input.startTime, input.endTime));
        }
        if (!string.IsNullOrEmpty(input.keyword))
        {
            whereLambda = whereLambda.And(m => m.Sender.Contains(input.keyword) || m.Subject.Contains(input.keyword));
        }
        var list = await _repository.AsSugarClient().Queryable<EmailSendEntity>().Where(whereLambda).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<EmailListOutput>()
        {
            list = list.list.Adapt<List<EmailListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<EmailListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<object> GetInfo(string id)
    {
        var entity = new object();
        if (await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().AnyAsync(x => x.Id == id && x.DeleteMark == null))
        {
            var receiveInfo = await _repository.AsSugarClient().Queryable<EmailReceiveEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
            receiveInfo.Read = 1;
            await _repository.AsSugarClient().Updateable(receiveInfo).CallEntityMethod(m => m.LastModify()).UpdateColumns(x => new { x.LastModifyTime, x.LastModifyUserId, x.Read }).ExecuteCommandHasChangeAsync();
            entity = receiveInfo;
        }
        else
        {
            entity = await _repository.AsSugarClient().Queryable<EmailSendEntity>().FirstAsync(x => x.Id == id && x.DeleteMark == null);
        }
        return entity;
    }
    #endregion
}
