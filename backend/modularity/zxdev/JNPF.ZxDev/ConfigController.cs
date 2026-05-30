using JNPF.Common.Core.Manager;
using JNPF.Extensions;
using JNPF.Common.CodeGen.DataParsing;
using JNPF.Common.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;


using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Reflection;
using System.ComponentModel;
using Newtonsoft.Json;
using JNPF.ZxDev.Entitys;
using JNPF.Common.Helper;
using System.Data;
using JNPF.Common.Configuration;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.Text;
using JNPF.ZxDev.Entitys.Dto.Config;
using JNPF.Message.Interfaces;
using JNPF.Common.Dtos.Message;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Aop.Api.Domain;
using Attribute = System.Attribute;
using Microsoft.AspNetCore.Authorization;

namespace JNPF.ZxDev;

/// <summary>
/// 业务实现：系统配置信息.
/// </summary>
[ApiDescriptionSettings("ZxDev", Tag = "Config", Name = "Config", Order = 200)]
[Route("api/ZxDev/[controller]")]
public class ConfigController : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SysConfig> _repository;

    /// <summary>
    /// 数据库管理.
    /// </summary>
    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 数据接口服务.
    /// </summary>
    private readonly IDataInterfaceService _dataInterfaceService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;


    /// <summary>
    /// 通用数据解析.
    /// </summary>
    private readonly ControlParsing _controlParsing;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;


    /// <summary>
    /// 客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 初始化一个<see cref="ConfigService"/>类型的新实例.
    /// </summary>
    public ConfigController(
        ISqlSugarRepository<SysConfig> repository,
        IDataInterfaceService dataInterfaceService,
        IDataBaseManager dataBaseManager,
        ISqlSugarClient context,
        ICacheManager cacheManager,
        ControlParsing controlParsing,
        IMessageManager messageManager,
        IUserManager userManager)
    {
        _repository = repository;
        _dataBaseManager = dataBaseManager;
        _sqlSugarClient = (SqlSugarScope)context;
        _dataInterfaceService = dataInterfaceService;
        _cacheManager = cacheManager;
        _controlParsing = controlParsing;
        _userManager = userManager;
        _messageManager = messageManager; 
    }

    /// <summary>
    /// 新建系统配置信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ConfigCrInput input)
    {
        input = CodeGenHelper.SetEmptyStringNull(input);


        var entity = input.Adapt<SysConfig>();
        entity.KeyName = SnowflakeIdHelper.NextId();
        var isOk = await _sqlSugarClient.Insertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 更新配置类的视图结构
    /// </summary>
    /// <returns></returns>
    [HttpPost("UpdateConfigView")]
    public async Task UpdateConfigView()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies();
        Assembly assembly = types.FirstOrDefault(a => a.FullName.StartsWith("JNPF.Water.Entitys"));

        var typeList = assembly.GetTypes();
        foreach (var configType in typeList)
        {
            if (!configType.BaseType.Name.EndsWith("ConfigView")) continue;


            var nameAttribute = (DisplayNameAttribute)Attribute.GetCustomAttribute(configType, typeof(DisplayNameAttribute));
            string configName = nameAttribute != null ? nameAttribute.DisplayName : string.Empty;
            PropertyInfo[] properties = configType.GetProperties();

            string tableName = "Sys_Config_" + configType.Name;
            string createTableSQL = $"CREATE TABLE {tableName} ( ID NVARCHAR(50) , ";
            string addExtendedPropertySQL = "";
            foreach (var property in properties)
            {
                string propertyName = property.Name;
                string propertyType = "NVARCHAR(50)";
                string propertyDescription = GetPropertyDescription(property); // 获取属性的说明信息
                createTableSQL += $"{propertyName} {propertyType} , ";

                // 如果有说明信息，为字段添加扩展属性
                if (!string.IsNullOrEmpty(propertyDescription))
                {
                    addExtendedPropertySQL += $"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{propertyDescription}', " +
                                                    $"@level0type=N'SCHEMA', @level0name=N'dbo', " +
                                                    $"@level1type=N'TABLE', @level1name=N'{tableName}', " +
                                                    $"@level2type=N'COLUMN', @level2name=N'{propertyName}';";
                }
            }

            createTableSQL = createTableSQL.Remove(createTableSQL.Length - 2);
            createTableSQL += ")";

            string checkIfExistsSQL = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL " +
                                           $"DROP TABLE {tableName};";

            try
            {
                _sqlSugarClient.Ado.ExecuteCommand(checkIfExistsSQL);
                _sqlSugarClient.Ado.ExecuteCommand(createTableSQL);

                if (addExtendedPropertySQL.IsNotEmptyOrNull())
                    _sqlSugarClient.Ado.ExecuteCommand(addExtendedPropertySQL);
            }
            catch (Exception e)
            {
                throw Oops.Oh(e.Message);
            }
        }
        //成功
    }

    [HttpPost("GetConfigData/{configName}")]
    public async Task<dynamic> GetConfigData(string configName)
    {
        var config = _sqlSugarClient.Queryable<SysConfig>().First(it => it.KeyName == configName && it.DeleteMark == null);

        string jsonData = config.KeyValue ?? "";
        var formDataObject = JsonConvert.DeserializeObject(jsonData);
        formDataObject = formDataObject ?? new object();
        return new { formData = formDataObject };
    }

    [HttpPost("SubmitConfigData/{id}")]
    public async Task SubmitConfigData(string id, [FromBody] dynamic configData)
    {

        var config = _sqlSugarClient.Queryable<SysConfig>().First(it => it.Id.ToString() == id);

        if (config != null)
        {
            //using (var tran = _sqlSugarClient.Ado.Transaction)
            //{
            config.DeleteMark = 1;
            _sqlSugarClient.Updateable<SysConfig>(config).ExecuteCommand();

            SysConfig newConfig = config.Copy();
            newConfig.Id = SnowflakeIdHelper.NextId();
            newConfig.VersionNum++;
            newConfig.DeleteMark = null;
            newConfig.UpdateBy = _userManager.UserId;
            newConfig.UpdateDate = DateTime.Now;
            var formDataObject = JsonConvert.SerializeObject(configData);
            newConfig.KeyValue = formDataObject;

            _sqlSugarClient.Insertable<SysConfig>(newConfig).ExecuteCommand();
            //    tran.Commit();
            //}
        }
    }

    // 获取属性的说明信息
    static string GetPropertyDescription(PropertyInfo property)
    {
        var descriptionAttribute = (DisplayNameAttribute)Attribute.GetCustomAttribute(property, typeof(DisplayNameAttribute));
        return descriptionAttribute != null ? descriptionAttribute.DisplayName : string.Empty;
    }

    [HttpPost("CreateDatabale")]
    public async Task CreateDatabale(string Id)
    {
        var entity = _sqlSugarClient.Queryable<SystemDbEntity>().First(aa => aa.Id == Id);
        dynamic jsonEntity = JsonConvert.DeserializeObject<JArray>(entity.filename);
        string filename = (string)jsonEntity[0]["fileId"];

        var filePath = Path.Combine(KeyVariable.SystemPath, "SystemFile", filename);

        DataSet ds = new DataSet();
        using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            IWorkbook workbook = null;

            // 2003
            if (filePath.IndexOf(".xlsx") == -1)
                workbook = new HSSFWorkbook(file);
            else
                workbook = new XSSFWorkbook(file);

            int index = 0;
            while (true)
            {
                try
                {
                    string SheetName = workbook.GetSheetName(index++);
                    DataTable table = ExcelImportHelper.ToDataTable(workbook, SheetName, 0);
                    table.TableName = SheetName;
                    ds.Tables.Add(table);
                }
                catch (Exception e)
                {
                    if (index == 0)
                        throw e;
                    break;
                    //代表已正常读了一个表
                }
            }

            var dbLink = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(it => it.Id.Equals("536119374235303877"));
            _sqlSugarClient = _dataBaseManager.ChangeDataBase(dbLink);

            foreach (DataTable table in ds.Tables)
            {
                if (!table.TableName.Contains("-")) continue;

                string description = table.TableName.Split('-')[0].Trim();
                string tableName = table.TableName.Split('-')[1].Trim();

                try
                {
                    var isTableExists = _sqlSugarClient.DbMaintenance.IsAnyTable(tableName, false);
                    if (isTableExists)
                    {
                        int rowCount = _sqlSugarClient.Ado.GetInt($"SELECT COUNT(*) FROM {tableName}");
                        if (rowCount > 0) { continue; }
                        else
                        {
                            _sqlSugarClient.Ado.ExecuteCommand($"DROP TABLE dbo.{tableName}");
                        }
                    }

                    string sql = GenerateCreateTableSql(table, tableName, description);
                    _sqlSugarClient.Ado.ExecuteCommand(sql);
                }
                catch (Exception e)
                {

                    throw;
                }
            }
        }


    }

    public string GenerateCreateTableSql(DataTable schemaTable, string tableName, string tableDes)
    {
        StringBuilder sqlBuilder = new StringBuilder();

        // 开始创建表的SQL语句
        sqlBuilder.AppendLine($"CREATE TABLE dbo.{tableName}");
        sqlBuilder.AppendLine("(");

        // 追踪是否已经添加了主键约束
        bool primaryKeyAdded = false;
        bool firstRow = true;
        foreach (DataRow row in schemaTable.Rows)
        {
            //跳过首行
            if (firstRow) { firstRow = false; continue; }
            string columnName = row[0].ToString().Trim(); //"字段名"  
            string columnType = row[2].ToString().Trim(); //"数据类型"
            string strAllowNull = row[4].ToString().Trim(); //"允许为空"
            bool allowNull = strAllowNull == "Yes" || strAllowNull == "1" || strAllowNull == "是";
            string strPrimaryKey = row[3].ToString().Trim(); //"主键"
            bool isPrimaryKey = strPrimaryKey == "Yes" || strPrimaryKey == "1" || strPrimaryKey == "是";

            sqlBuilder.Append($"[{columnName}] {columnType} {(allowNull ? "NULL" : "NOT NULL")}");

            // 只有在尚未添加主键约束且当前字段被标记为主键时才添加主键约束
            if (isPrimaryKey && !primaryKeyAdded)
            {
                sqlBuilder.Append(" PRIMARY KEY");
                primaryKeyAdded = true;
            }

            sqlBuilder.AppendLine(",");
        }

        // 移除最后一个逗号
        sqlBuilder.Remove(sqlBuilder.Length - 3, 1);

        // 结束创建表
        sqlBuilder.AppendLine(");");

        // 为表添加扩展属性描述
        sqlBuilder.AppendLine($"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{tableDes}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{tableName}';");

        firstRow = true;
        // 为每个字段添加扩展属性描述
        foreach (DataRow row in schemaTable.Rows)
        {
            if (firstRow) { firstRow = false; continue; }
            string columnName = row[0].ToString().Trim(); //"字段名"
            string columnDescription = row[1].ToString().Trim(); //"描述"

            sqlBuilder.AppendLine($"EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'{columnDescription.Replace("'", "''")}' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'{tableName}', @level2type=N'COLUMN',@level2name=N'{columnName}';");
        }

        return sqlBuilder.ToString();
    }

    private readonly IMessageManager _messageManager;


    /// <summary>
    /// 话务系统对接设置，临时接口，需要移入水务系统
    /// </summary>
    /// <param name="callRequest"></param>
    /// <returns></returns>
    [HttpPost("sendMobileData")]
    [AllowAnonymous]
    public dynamic SendMobileData([FromBody] dynamic callRequest)
    {
        string mobile = callRequest.request.caller;

        Dictionary<string, string> linkParams = new Dictionary<string, string>();
        linkParams.Add("MobilePhone", mobile);

        MessageSendParam sendParam = new MessageSendParam()
        {
            field = "@Title",
            fieldName = "标题",
            id = "558793885111812037",
            templateCode = "",
            templateId = "558632320148438981",
            templateName = "话务系统来电信息2",
            templateType = "0",
            value = mobile,
            relationField = "",
            isSubTable = false,
        };
        
        MessageSendModel item = new MessageSendModel
        {
            id = "558636364279578565",
            toUser = new List<string> { "554082398552195013", "561056916965425093", "561057720271110085", "561057815452450757" }, //用户 ryzls
            msgTemplateName = "话务系统来电信息2",
            sendConfigId = "558636364002754501",
            messageType = "信息弹窗",
            templateId = "558632320148438981",
            accountConfigId = "",
            gotoLink = "/model/call.info.query",
            linkParams = linkParams,
        };
        item.paramJson= new List<MessageSendParam> { sendParam };

        _messageManager.SendDefinedMsg(item, new Dictionary<string, object>());

        Dictionary<string, object> res = new Dictionary<string, object>();
        Dictionary<string, object> map = new Dictionary<string, object>();
        map.Add("retcode", 0);
        map.Add("reason", "成功");
        res.Add("response", map);

        return new { response = map };
    }


}
