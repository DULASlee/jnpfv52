using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.JsonSerialization;
using JNPF.Systems.Entitys.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SqlSugar;

namespace JNPF.Systems.Common;

/// <summary>
/// 测试接口.
/// </summary>
[ApiDescriptionSettings(Name = "Test", Order = 306)]
[Route("api")]
public class TestService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<UserEntity> _sqlSugarRepository;
    private readonly IDataBaseManager _databaseService;
    private readonly ITenant _db;
    private readonly IFileManager _fileManager;

    public TestService(ISqlSugarRepository<UserEntity> sqlSugarRepository, ISqlSugarClient context, IDataBaseManager databaseService, IFileManager fileManager)
    {
        _sqlSugarRepository = sqlSugarRepository;
        _databaseService = databaseService;
        _fileManager = fileManager;
        _db = context.AsTenant();
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<dynamic> test()
    {
        try
        {
            //PutObjectArgs a = new PutObjectArgs().WithObjectSize(0);
            //var aaaaa= JsEngineUtil.AggreFunction("COUNT('1','1','1')").ToString();
            //var xx = App.HttpContext.Request.Host.ToString();
            //var sql = "SELECT  TOP 1 [F_PARENTID],[F_PROCESSID],[F_ENCODE],[F_FULLNAME],[F_FLOWURGENT],[F_FLOWID],[F_FLOWCODE],[F_FLOWNAME],[F_FLOWTYPE],[F_FLOWCATEGORY],[F_FLOWFORM],[F_FLOWFORMCONTENTJSON],[F_FLOWTEMPLATEJSON],[F_FLOWVERSION],[F_STARTTIME],[F_ENDTIME],[F_THISSTEP],[F_THISSTEPID],[F_GRADE],[F_STATUS],[F_COMPLETION],[F_DESCRIPTION],[F_SORTCODE],[F_ISASYNC],[F_ISBATCH],[F_TASKNODEID],[F_TEMPLATEID],[F_REJECTDATAID],[F_DELEGATEUSER],[F_CREATORTIME],[F_CREATORUSERID],[F_ENABLEDMARK],[F_LastModifyTime],[F_LastModifyUserId],[F_DeleteMark],[F_DeleteTime],[F_DeleteUserId],[F_Id] FROM [FLOW_TASK]  WHERE (( [F_DeleteMark] IS NULL ) AND ( [F_Id] = N'367536153122855173' ))";
            //var darta = _sqlSugarRepository.AsSugarClient().Ado.SqlQuery<dynamic>(sql);
            //FileHelper.MakeThumbnail("E:\\VSImages\\2a1367108c711a74011aee52c7830b33.jpeg", "E:\\VSImages\\缩略图113.jpeg", 120, 120, "DB");
            //var stream = FileHelper.FileToStream("E:\\VSImages\\2a1367108c711a74011aee52c7830b33.jpeg");
            //var newssss = FileHelper.MakeThumbnail(stream, 120, 120, "DB");
            //await _fileManager.UploadFileByType(newssss, "E:\\VSImages", "缩略图2323.jpeg");
            //var user = _sqlSugarRepository.GetFirst(x => x.Account == "101002").ToObject<TestModel>();
            ////return user;

            //var list = new List<Content>();
            //list.Add(new Content { EnCode = "tips.loadMore", Name = "加载更多" });
            //list.Add(new Content { EnCode = "tips.fieldDuplicate", Name = "测试钉钉连接成功" });
            //list.Add(new Content { EnCode = "profile.education", Name = "文化程度" });
            //list.Add(new Content { EnCode = "alert.workflowTip03", Name = "转办成功" });
            //list.Add(new Content { EnCode = "notice.system", Name = "系统" });
            //list.Add(new Content { EnCode = "tips.loadMore.ddsds", Name = "加载更多132121" });
            //var output = new Dictionary<string, object>();

            //foreach (var item in list)
            //{
            //    var thisDic = cs1(item);
            //    var xx = thisDic.ToJsonString();
            //    output = cs2(output, thisDic);
            //}
            //var str= output.ToJsonString();
            var dic = new Dictionary<string, object>();
            dic.Add("3213", DateTime.Now.ToString());

            return new { sj = dic };
        }
        catch (Exception e)
        {
            throw;
        }
    }

    [HttpPost("update1/{id}")]
    public async Task<dynamic> update1([FromBody] dynamic data1)
    {
        //业务方法

        return data1;

    }
    public void xx(UserEntity user)
    {
        user.Account = "2312321";

    }

    public void xx1(UserEntity user)
    {
        user.Account = "2312321";

    }

    public Dictionary<string, object> cs1(Content content)
    {
        var list = content.EnCode.Split(".").ToList();
        list.Reverse();
        var resultDic = new Dictionary<string, object>();
        int index = 0;
        foreach (var item in list)
        {
            var key = item;
            resultDic = index == 0 ? new Dictionary<string, object> { { key, content.Name } } : new Dictionary<string, object> { { key, resultDic } };
            ++index;
        }
        return resultDic;
    }

    public Dictionary<string, object> cs2(Dictionary<string, object> zhDic, Dictionary<string, object> dqDic)
    {
        if (zhDic.Any())
        {
            if (zhDic.ContainsKey(dqDic.Keys.FirstOrDefault()))
            {
                // 最后结果是字符串替换
                if (zhDic[dqDic.Keys.FirstOrDefault()] is string || dqDic[dqDic.Keys.FirstOrDefault()] is string)
                {
                    zhDic[dqDic.Keys.FirstOrDefault()] = dqDic[dqDic.Keys.FirstOrDefault()];
                }
                else
                {
                    var dic1 = zhDic[dqDic.Keys.FirstOrDefault()].ToObject<Dictionary<string, object>>();
                    var dic2 = dqDic[dqDic.Keys.FirstOrDefault()].ToObject<Dictionary<string, object>>();
                    zhDic[dqDic.Keys.FirstOrDefault()] = cs2(dic1, dic2);
                }
            }
            else
            {
                zhDic[dqDic.Keys.FirstOrDefault()] = dqDic[dqDic.Keys.FirstOrDefault()];
            }
        }
        else
        {
            zhDic = dqDic;
        }
        return zhDic;
    }

}


public class TestModel
{
    [JsonConverter(typeof(NewtonsoftJsonDateTimeJsonConverter))]
    public DateTime? firstLogTime { get; set; }
}


public class Content
{
    public string EnCode { get; set; }

    public string Name { get; set; }
}