using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Extensions;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Common;
using JNPF.Systems.Interfaces.System;
using JNPF.ViewEngine;
using JNPF.VisualDev.Engine;
using JNPF.VisualDev.Engine.CodeGen;
using JNPF.VisualDev.Engine.Security;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.CodeGen;
using JNPF.VisualDev.Entitys.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.IO.Compression;
using System.Text;

namespace JNPF.CodeGen;

/// <summary>
/// 业务实现：代码生成.
/// </summary>
[ApiDescriptionSettings(Tag = "CodeGenerater", Name = "Generater", Order = 175)]
[Route("api/visualdev/[controller]")]
public class CodeGenService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualDevEntity> _repository;

    /// <summary>
    /// 视图引擎.
    /// </summary>
    private readonly IViewEngine _viewEngine;

    /// <summary>
    /// 数据连接服务.
    /// </summary>
    private readonly IDbLinkService _dbLinkService;

    /// <summary>
    /// 字典数据服务.
    /// </summary>
    private readonly IDictionaryDataService _dictionaryDataService;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileService _fileService;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 数据库管理器.
    /// </summary>
    private readonly IDataBaseManager _databaseManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化一个<see cref="CodeGenService"/>类型的新实例.
    /// </summary>
    public CodeGenService(
        ISqlSugarRepository<VisualDevEntity> visualDevRepository,
        IViewEngine viewEngine,
        IDbLinkService dbLinkService,
        IDictionaryDataService dictionaryDataService,
        IFileService fileService,
        IUserManager userManager,
        IOptions<TenantOptions> tenantOptions,
        ICacheManager cacheManager,
        IDataBaseManager databaseManager)
    {
        _repository = visualDevRepository;
        _viewEngine = viewEngine;
        _dbLinkService = dbLinkService;
        _dictionaryDataService = dictionaryDataService;
        _fileService = fileService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _tenant = tenantOptions.Value;
        _databaseManager = databaseManager;
    }

    #region Post

    /// <summary>
    /// 下载代码.
    /// </summary>
    [HttpPost("{id}/Actions/DownloadCode")]
    public async Task<dynamic> DownloadCode(string id, [FromBody] DownloadCodeFormInput downloadCodeForm)
    {
        var templateEntity = await _repository.GetFirstAsync(v => v.Id == id && v.DeleteMark == null);
        _ = templateEntity ?? throw Oops.Oh(ErrorCode.COM1005);
        _ = templateEntity.Tables ?? throw Oops.Oh(ErrorCode.D2100);
        if (templateEntity.FormData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1026);
        if (templateEntity.WebType.Equals(2) && templateEntity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1027);
        var model = templateEntity.FormData.ToObjectOld<FormDataModel>();
        var dictionaryData = await _repository.AsSugarClient().Queryable<DictionaryDataEntity>().FirstAsync(it => it.Id.Equals(downloadCodeForm.module));
        downloadCodeForm.modulePackageName = dictionaryData.FullName;
        if (templateEntity.Type == 3)
            downloadCodeForm.modulePackageName = "WorkFlow";
        model.className = new List<string>() { downloadCodeForm.className.ParseToPascalCase() };
        model.areasName = downloadCodeForm.modulePackageName;
        string fileName = string.Format("{0}_{1:yyyyMMddHHmmss}", templateEntity.FullName, DateTime.Now);

        // 判断子表名称
        var childTb = new List<string>();
        if (!downloadCodeForm.subClassName.IsNullOrEmpty())
            childTb = new List<string>(downloadCodeForm.subClassName.Split(','));

        // 子表名称去重
        HashSet<string> set = new HashSet<string>(childTb);
        var tableList = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();
        tableList.FindAll(it => it.relationTable.Equals("")).ForEach(item =>
        {
            item.className = downloadCodeForm.className.ParseToPascalCase();
            item.tableName = downloadCodeForm.description;
        });
        bool result = childTb.Count == set.Count ? true : false;
        if (!result)
            throw Oops.Oh(ErrorCode.D2101);

        var numNo = 0;
        tableList.FindAll(it => !it.relationTable.Equals("")).ForEach(item =>
        {
            item.className = childTb[numNo].ParseToPascalCase();
            numNo++;
        });
        templateEntity.FormData = model.ToJsonString();
        templateEntity.Tables = tableList.ToJsonString();

        // 模板数据聚合
        await TemplatesDataAggregation(fileName, templateEntity);
        string randPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName);
        string downloadPath = randPath + ".zip";

        // 判断是否存在同名称文件
        if (File.Exists(downloadPath))
            File.Delete(downloadPath);

        ZipFile.CreateFromDirectory(randPath, downloadPath);
        if (!App.Configuration["OSS:Provider"].Equals("Invalid"))
            await _fileService.UploadFileByType(downloadPath, "CodeGenerate", string.Format("{0}.zip", fileName));
        var downloadFileName = string.Format("{0}|{1}.zip|codeGenerator", _userManager.UserId, fileName);
        _cacheManager.Set(fileName + ".zip", string.Empty);
        return new { name = fileName, url = "/api/File/Download?encryption=" + DESCEncryption.Encrypt(downloadFileName, "JNPF") };
    }

    /// <summary>
    /// 预览代码.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="downloadCodeForm"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/CodePreview")]
    public async Task<dynamic> CodePreview(string id, [FromBody] DownloadCodeFormInput downloadCodeForm)
    {
        var templateEntity = await _repository.GetFirstAsync(v => v.Id == id && v.DeleteMark == null);
        _ = templateEntity ?? throw Oops.Oh(ErrorCode.COM1005);
        _ = templateEntity.Tables ?? throw Oops.Oh(ErrorCode.D2100);
        if (templateEntity.FormData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1028);
        if (templateEntity.WebType.Equals(2) && templateEntity.ColumnData.IsNullOrEmpty()) throw Oops.Oh(ErrorCode.COM1029);
        var model = templateEntity.FormData.ToObjectOld<FormDataModel>();
        model.className = new List<string>() { downloadCodeForm.className.ParseToPascalCase() };
        model.areasName = downloadCodeForm.module;
        string fileName = SnowflakeIdHelper.NextId();

        // 判断子表名称
        var childTb = new List<string>();
        if (!downloadCodeForm.subClassName.IsNullOrEmpty())
            childTb = new List<string>(downloadCodeForm.subClassName.Split(','));

        // 子表名称去重
        HashSet<string> set = new HashSet<string>(childTb);
        var tableList = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();
        tableList.FindAll(it => it.relationTable.Equals("")).ForEach(item =>
        {
            item.className = downloadCodeForm.className.ParseToPascalCase();
            item.tableName = downloadCodeForm.description;
        });
        bool result = childTb.Count == set.Count ? true : false;
        if (!result)
            throw Oops.Oh(ErrorCode.D2101);

        var numNo = 0;
        tableList.FindAll(it => !it.relationTable.Equals("")).ForEach(item =>
        {
            item.className = childTb[numNo].ParseToPascalCase();
            numNo++;
        });
        templateEntity.FormData = model.ToJsonString();
        templateEntity.Tables = tableList.ToJsonString();

        await TemplatesDataAggregation(fileName, templateEntity);
        string randPath = Path.Combine(KeyVariable.SystemPath, "CodeGenerate", fileName);
        var dataList = this.PriviewCode(randPath);
        if (dataList == null && dataList.Count == 0)
            throw Oops.Oh(ErrorCode.D2102);
        return new { list = dataList };
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 模板数据聚合.
    /// </summary>
    /// <param name="fileName">生成ZIP文件名.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    private async Task TemplatesDataAggregation(string fileName, VisualDevEntity templateEntity)
    {
        // 类型名称
        var categoryName = (await _dictionaryDataService.GetInfo(templateEntity.Category)).EnCode;

        // 表关系
        List<DbTableRelationModel>? tableRelation = templateEntity.Tables.ToObject<List<DbTableRelationModel>>();

        // 表单数据
        var formDataModel = templateEntity.FormData.ToObjectOld<FormDataModel>();

        // 列表属性
        ColumnDesignModel? pcColumnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
        ColumnDesignModel? appColumnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
        pcColumnDesignModel ??= new ColumnDesignModel();
        appColumnDesignModel ??= new ColumnDesignModel();

        // 既是行内编辑又是纯表单 强制改成普通列表.
        if (templateEntity.WebType.Equals(1) && pcColumnDesignModel.type.Equals(4))
        {
            var columnData = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
            columnData["type"] = 1;
            templateEntity.ColumnData = columnData.ToJsonString();
        }

        // 分组和树形表格去掉复杂表头
        if (pcColumnDesignModel.type.Equals(3) || pcColumnDesignModel.type.Equals(5))
        {
            var columnData = templateEntity.ColumnData.ToObject<Dictionary<string, object>>();
            columnData["complexHeaderList"] = new List<object>();
            templateEntity.ColumnData = columnData.ToJsonString();
        }

        // 开启数据权限
        bool useDataPermission = false;

        if (pcColumnDesignModel.useDataPermission && appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else if (!pcColumnDesignModel.useDataPermission && appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else if (pcColumnDesignModel.useDataPermission && !appColumnDesignModel.useDataPermission)
        {
            useDataPermission = true;
        }
        else
        {
            useDataPermission = false;
        }

        switch (templateEntity.WebType)
        {
            case 1:
                useDataPermission = false;
                break;
        }

        // 剔除多余布局控件组
        var controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
        var fieldsCopy = formDataModel.fields.Copy();
        TemplateAnalysis.DataFormatReplace(controls);

        switch (templateEntity.Type)
        {
            case 4:
            case 5:
                switch (templateEntity.WebType)
                {
                    case 1:
                        break;
                    default:
                        // 统一处理下表单内控件
                        controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerFormDataModel(controls, pcColumnDesignModel, appColumnDesignModel);
                        controls = CodeGenUnifiedHandlerHelper.UnifiedHandlerControlRelationship(controls);
                        break;
                }

                break;
        }

        List<string> targetPathList = new List<string>();
        List<string> templatePathList = new List<string>();

        string tableName = string.Empty;
        CodeGenConfigModel codeGenConfigModel = new CodeGenConfigModel();

        // 主表代码生成配置模型
        CodeGenConfigModel codeGenMainTableConfigModel = new CodeGenConfigModel();

        // 子表表名和主键.
        var ctPrimaryKey = new Dictionary<string, string>();

        /*
         * 区分是纯主表、主带副、主带子、主带副与子
         * 1-纯主表、2-主带子、3-主带副、4-主带副与子
         * 生成模式
         * 因ORM原因 导航查询 一对多 列表查询
         * 不能使用ORM 自带函数 待作者开放.Select()
         * 导致一对多列表查询转换必须全使用子查询
         * 远端数据与静态数据无法列表转换所以全部ThenMapper内转换
         * 数据字典又分为两种值转换ID与EnCode
         */
        var modelType = JudgmentGenerationModel(tableRelation, controls);
        switch (modelType)
        {
            case GeneratePatterns.MainBelt:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    List<CodeGenTableRelationsModel> tableRelationsList = new List<CodeGenTableRelationsModel>();

                    var tableNo = 0;

                    // 生成子表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        tableNo++;
                        var controlId = string.Empty;
                        var children = controls.Find(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table));
                        if (children == null) continue;
                        controlId = children.__vModel__;
                        if (children != null) controls = children.__config__.children;

                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        ctPrimaryKey.Add(item.table, fieldList.Find(x => x.primaryKey).field);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 默认主表开启自增子表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.ChildTableBackEnd(item.table, item.className, fieldList, controls, templateEntity, controlId, item.tableField);
                        codeGenConfigModel.BusName = children.__config__.label;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendChildTableTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);
                        templatePathList = CodeGenTargetPathHelper.BackendChildTableTemplatePathList("SubTable", templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);

                        // 生成子表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = item.table,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = codeGenConfigModel.IsMapper,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = codeGenConfigModel.IsImportData,
                                EnableFlow = templateEntity.EnableFlow == 0 ? false : true,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        tableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = item.className,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
                            OriginalRelationField = item.relationField,
                            ControlTableComment = codeGenConfigModel.BusName,
                            TableComment = item.tableName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            ChilderColumnConfigListCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && !it.ForeignKeyField && it.jnpfKey != null).Count(),
                            TableNo = tableNo,
                            ControlModel = controlId,
                            IsQueryWhether = codeGenConfigModel.TableField.Any(it => it.QueryWhether),
                            IsShowField = codeGenConfigModel.TableField.Any(it => it.IsShow),
                            IsUnique = codeGenConfigModel.TableField.Any(it => it.IsUnique),
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                        });

                        // 还原全部控件
                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
                    }

                    // 生成主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (formDataModel.primaryKeyPolicy == 2 && templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.MainBeltBackEnd(item.table, fieldList, controls, templateEntity);
                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.TableRelations = tableRelationsList;
                        codeGenConfigModel.IsChildConversion = tableRelationsList.Any(it => it.IsConversion);
                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("2-MainBelt", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("2-MainBelt", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("2-MainBelt", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                }
                                break;
                        }

                        // 生成后端文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            string tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsSystemControl = codeGenConfigModel.IsSystemControl,
                                IsUpdate = codeGenConfigModel.IsUpdate,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                TableRelations = codeGenConfigModel.TableRelations,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUnique = codeGenConfigModel.IsUnique || codeGenConfigModel.TableRelations.Any(it => it.IsUnique),
                                IsChildConversion = codeGenConfigModel.IsChildConversion,
                                IsChildIndexShow = codeGenConfigModel.TableRelations.Any(it => it.IsShowField),
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);

                        codeGenMainTableConfigModel = codeGenConfigModel;
                    }
                }

                break;
            case GeneratePatterns.MainBeltVice:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    List<CodeGenTableRelationsModel> tableRelationsList = new List<CodeGenTableRelationsModel>();

                    // 副表表字段配置
                    List<TableColumnConfigModel> auxiliaryTableColumnList = new List<TableColumnConfigModel>();

                    var tableNo = 0;
                    tableName = tableRelation.Find(it => it.relationTable.Equals(string.Empty)).table;

                    // 生成副表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        tableNo++;
                        var auxiliaryControls = controls.FindAll(it => it.__config__.tableName == item.table);
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);

                        // 默认主表开启自增副表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                        {
                            throw Oops.Oh(ErrorCode.D2109);
                        }

                        codeGenConfigModel = CodeGenWay.AuxiliaryTableBackEnd(item.table, fieldList, auxiliaryControls, templateEntity, tableNo, 0, item.tableField);
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendAuxiliaryTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);
                        templatePathList = CodeGenTargetPathHelper.BackendAuxiliaryTemplatePathList("3-Auxiliary", templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);

                        codeGenConfigModel.TableField.ForEach(items =>
                        {
                            items.ClassName = item.className;
                        });

                        // 生成副表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                AuxiliaryTable = item.table.ParseToPascalCase(),
                                MainTable = tableName.ParseToPascalCase(),
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = true,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = codeGenConfigModel.IsImportData,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        var count = controls.Count(x => x.__vModel__.Contains(item.table));
                        tableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = codeGenConfigModel.ClassName,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
                            OriginalRelationField = item.relationField,
                            TableComment = item.tableName,
                            TableNo = tableNo,
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSystemControl = codeGenConfigModel.TableField.Any(it => it.IsSystemControl),
                            IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                            FieldCount = count,
                        });

                        auxiliaryTableColumnList.AddRange(codeGenConfigModel.TableField.FindAll(it => it.jnpfKey != null));
                    }

                    // 生成主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, tableName);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (formDataModel.primaryKeyPolicy == 2 && templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.MainBeltViceBackEnd(item.table, fieldList, auxiliaryTableColumnList, controls, templateEntity);

                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("4-MainBeltVice", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }
                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("4-MainBeltVice", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("4-MainBeltVice", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }

                        for (var i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                AuxiliayTableRelations = tableRelationsList,
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                IsMainConversion = codeGenConfigModel.TableField.Any(it => it.IsAuxiliary.Equals(false) && it.IsConversion.Equals(true)),
                                IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate.Equals(true) && it.IsAuxiliary.Equals(false) && it.jnpfKey != null),
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUnique = codeGenConfigModel.IsUnique,
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);

                            codeGenMainTableConfigModel = codeGenConfigModel;
                        }
                    }
                }

                break;
            case GeneratePatterns.PrimarySecondary:
                {
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);

                    // 解析子表
                    var subTable = new List<DbTableRelationModel>();
                    var secondaryTable = new List<DbTableRelationModel>();
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "0"))
                    {
                        // 解析子表比副表效率
                        // 先获取出子表 其他的默认为副表
                        switch (controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table)))
                        {
                            case true:
                                subTable.Add(item);
                                break;
                            default:
                                secondaryTable.Add(item);
                                break;
                        }
                    }

                    List<CodeGenTableRelationsModel> subTableRelationsList = new List<CodeGenTableRelationsModel>();
                    List<CodeGenTableRelationsModel> secondaryTableRelationsList = new List<CodeGenTableRelationsModel>();

                    int tableNo = 1;

                    // 已经区分子表与副表
                    // 生成子表
                    foreach (DbTableRelationModel? item in subTable)
                    {
                        var controlId = string.Empty;
                        var children = controls.Find(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE) && it.__config__.tableName.Equals(item.table));
                        if (children == null) continue;
                        controlId = children.__vModel__;
                        if (children != null) controls = children.__config__.children;

                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);
                        ctPrimaryKey.Add(item.table, fieldList.Find(x => x.primaryKey).field);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 默认主表开启自增子表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.ChildTableBackEnd(item.table, item.className, fieldList, controls, templateEntity, controlId, item.tableField);
                        codeGenConfigModel.BusName = children.__config__.label;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendChildTableTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);
                        templatePathList = CodeGenTargetPathHelper.BackendChildTableTemplatePathList("SubTable", templateEntity.WebType, templateEntity.Type, codeGenConfigModel.IsMapper, codeGenConfigModel.IsShowSubTableField);

                        // 生成子表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = item.table,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = codeGenConfigModel.IsMapper,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = codeGenConfigModel.IsImportData,
                                EnableFlow = templateEntity.EnableFlow == 0 ? false : true,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        subTableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = item.className,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
                            OriginalRelationField = item.relationField,
                            ControlTableComment = codeGenConfigModel.BusName,
                            TableComment = item.tableName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            ChilderColumnConfigListCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && !it.ForeignKeyField && it.jnpfKey != null).Count(),
                            TableNo = tableNo,
                            ControlModel = controlId,
                            IsQueryWhether = codeGenConfigModel.TableField.Any(it => it.QueryWhether),
                            IsShowField = codeGenConfigModel.TableField.Any(it => it.IsShow),
                            IsUnique = codeGenConfigModel.TableField.Any(it => it.IsUnique),
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                        });
                        tableNo++;

                        // 还原全部控件
                        controls = TemplateAnalysis.AnalysisTemplateData(formDataModel.fields);
                    }

                    // 副表表字段配置
                    List<TableColumnConfigModel> auxiliaryTableColumnList = new List<TableColumnConfigModel>();

                    // 生成副表
                    foreach (DbTableRelationModel? item in secondaryTable)
                    {
                        var auxiliaryControls = controls.FindAll(it => it.__config__.tableName == item.table);
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);

                        // 默认主表开启自增副表也需要开启自增
                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        codeGenConfigModel = CodeGenWay.AuxiliaryTableBackEnd(item.table, fieldList, auxiliaryControls, templateEntity, tableNo, 1, item.tableField);
                        codeGenConfigModel.BusName = item.tableName;
                        codeGenConfigModel.ClassName = item.className;

                        targetPathList = CodeGenTargetPathHelper.BackendAuxiliaryTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);
                        templatePathList = CodeGenTargetPathHelper.BackendAuxiliaryTemplatePathList("3-Auxiliary", templateEntity.WebType, templateEntity.Type, templateEntity.EnableFlow);

                        codeGenConfigModel.TableField.ForEach(items => items.ClassName = item.className);

                        // 生成副表相关文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            var tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                IsMainTable = false,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                NameSpace = formDataModel.areasName,
                                PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                                AuxiliaryTable = item.table.ParseToPascalCase(),
                                MainTable = tableName.ParseToPascalCase(),
                                MainClassName = codeGenConfigModel.ClassName,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsMapper = true,
                                WebType = templateEntity.WebType,
                                Type = templateEntity.Type,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                IsImportData = codeGenConfigModel.IsImportData,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                IsTenantColumn = false,
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }

                        var count = controls.Count(x => x.__vModel__.Contains(item.table));
                        secondaryTableRelationsList.Add(new CodeGenTableRelationsModel
                        {
                            ClassName = codeGenConfigModel.ClassName,
                            OriginalTableName = item.table,
                            RelationTable = item.relationTable,
                            TableName = item.table.ParseToPascalCase(),
                            PrimaryKey = codeGenConfigModel.TableField.Find(it => it.PrimaryKey).ColumnName,
                            TableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).ColumnName,
                            ChilderColumnConfigList = codeGenConfigModel.TableField,
                            OriginalTableField = codeGenConfigModel.TableField.Find(it => it.ForeignKeyField).OriginalColumnName,
                            RelationField = item.relationField.ReplaceRegex("^f_", string.Empty).ParseToPascalCase(),
                            OriginalRelationField = item.relationField,
                            TableComment = item.tableName,
                            TableNo = tableNo,
                            IsConversion = codeGenConfigModel.TableField.Any(it => it.IsConversion.Equals(true)),
                            IsDetailConversion = codeGenConfigModel.TableField.Any(it => it.IsDetailConversion.Equals(true)),
                            IsImportData = codeGenConfigModel.TableField.Any(it => it.IsImportField.Equals(true)),
                            IsSystemControl = codeGenConfigModel.TableField.Any(it => it.IsSystemControl),
                            IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsControlParsing = codeGenConfigModel.TableField.Any(it => it.IsControlParsing),
                            FieldCount = count,
                        });

                        auxiliaryTableColumnList.AddRange(codeGenConfigModel.TableField.FindAll(it => it.jnpfKey != null));
                    }

                    // 解析主表
                    foreach (DbTableRelationModel? item in tableRelation.FindAll(it => it.typeId == "1"))
                    {
                        var fieldList = _databaseManager.GetFieldList(targetLink, item.table);

                        if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                        // 开启乐观锁
                        if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                            throw Oops.Oh(ErrorCode.D2107);

                        if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                            throw Oops.Oh(ErrorCode.D2109);

                        if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                            throw Oops.Oh(ErrorCode.D2105);

                        // 列表带流程 或者 流程表单 自增ID
                        if (formDataModel.primaryKeyPolicy == 2 && templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                            throw Oops.Oh(ErrorCode.D2108);

                        if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                            throw Oops.Oh(ErrorCode.D2110);

                        // 后端生成
                        codeGenConfigModel = CodeGenWay.PrimarySecondaryBackEnd(item.table, fieldList, auxiliaryTableColumnList, controls, templateEntity);

                        codeGenConfigModel.IsMapper = true;
                        codeGenConfigModel.BusName = tableRelation.Find(it => it.relationTable.Equals("")).tableName;
                        codeGenConfigModel.TableRelations = subTableRelationsList;
                        codeGenConfigModel.IsChildConversion = subTableRelationsList.Any(it => it.IsConversion);

                        switch (templateEntity.WebType)
                        {
                            case 1:
                                switch (templateEntity.Type)
                                {
                                    case 3:
                                        targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("5-PrimarySecondary", codeGenConfigModel.IsMapper);
                                        break;
                                    default:
                                        targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                        templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                        break;
                                }
                                break;
                            case 2:
                                switch (codeGenConfigModel.TableType)
                                {
                                    case 4:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                    default:
                                        switch (templateEntity.Type)
                                        {
                                            case 3:
                                                targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(item.className, fileName, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("5-PrimarySecondary", codeGenConfigModel.IsMapper);
                                                break;
                                            default:
                                                targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(item.className, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                                templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("5-PrimarySecondary", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                                break;
                                        }

                                        break;
                                }
                                break;
                        }

                        // 生成后端文件
                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            string tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                                NameSpace = codeGenConfigModel.NameSpace,
                                BusName = codeGenConfigModel.BusName,
                                ClassName = codeGenConfigModel.ClassName,
                                PrimaryKey = codeGenConfigModel.PrimaryKey,
                                LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                                OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                                MainTable = codeGenConfigModel.MainTable,
                                LowerMainTable = codeGenConfigModel.LowerMainTable,
                                OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                                hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                                Function = codeGenConfigModel.Function,
                                TableField = codeGenConfigModel.TableField,
                                RelationsField = codeGenConfigModel.RelationsField,
                                TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                                DefaultSidx = codeGenConfigModel.DefaultSidx,
                                IsExport = codeGenConfigModel.IsExport,
                                IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                                IsUploading = codeGenConfigModel.IsUpload,
                                IsTableRelations = codeGenConfigModel.IsTableRelations,
                                IsMapper = codeGenConfigModel.IsMapper,
                                IsBillRule = codeGenConfigModel.IsBillRule,
                                DbLinkId = codeGenConfigModel.DbLinkId,
                                FormId = codeGenConfigModel.FormId,
                                WebType = codeGenConfigModel.WebType,
                                Type = codeGenConfigModel.Type,
                                EnableFlow = codeGenConfigModel.EnableFlow,
                                IsMainTable = codeGenConfigModel.IsMainTable,
                                EnCode = codeGenConfigModel.EnCode,
                                UseDataPermission = useDataPermission,
                                SearchControlNum = codeGenConfigModel.SearchControlNum,
                                IsAuxiliaryTable = codeGenConfigModel.IsAuxiliaryTable,
                                ExportField = codeGenConfigModel.ExportField,
                                TableRelations = codeGenConfigModel.TableRelations,
                                ConfigId = _userManager.TenantId,
                                DBName = _userManager.TenantDbName,
                                PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                                AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                                AuxiliayTableRelations = secondaryTableRelationsList,
                                FullName = codeGenConfigModel.FullName,
                                IsConversion = codeGenConfigModel.IsConversion,
                                IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                                HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                                PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                                ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                                IsUpdate = codeGenConfigModel.TableField.Any(it => it.IsUpdate.Equals(true) && it.IsAuxiliary.Equals(false) && it.jnpfKey != null),
                                IsUnique = codeGenConfigModel.IsUnique || codeGenConfigModel.TableRelations.Any(it => it.IsUnique),
                                IsChildConversion = codeGenConfigModel.IsChildConversion,
                                IsChildIndexShow = codeGenConfigModel.TableRelations.Any(it => it.IsShowField),
                                GroupField = codeGenConfigModel.GroupField,
                                GroupShowField = codeGenConfigModel.GroupShowField,
                                IsImportData = codeGenConfigModel.IsImportData,
                                ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel),
                                ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                                ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                                ImportDataType = codeGenConfigModel.ImportDataType,
                                DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                                IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                                IsTreeTable = codeGenConfigModel.IsTreeTable,
                                ParentField = codeGenConfigModel.ParentField,
                                TreeShowField = codeGenConfigModel.TreeShowField,
                                IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                                TableType = codeGenConfigModel.TableType,
                                IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                                PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                                AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                                PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                                AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);

                            codeGenMainTableConfigModel = codeGenConfigModel;
                        }
                    }
                }

                break;
            default:
                {
                    tableName = tableRelation.FirstOrDefault().table;
                    var link = await _repository.AsSugarClient().Queryable<DbLinkEntity>().FirstAsync(m => m.Id == templateEntity.DbLinkId && m.DeleteMark == null);
                    var targetLink = link ?? _databaseManager.GetTenantDbLink(_userManager.TenantId, _userManager.TenantDbName);
                    // 获取表结构
                    var fieldList = _databaseManager.GetFieldList(targetLink, tableName);

                    if (fieldList.Count == 0) throw Oops.Oh(ErrorCode.D2106);

                    // 开启乐观锁
                    if (formDataModel.concurrencyLock && !fieldList.Any(it => it.field.ToLower().Equals("f_version")))
                        throw Oops.Oh(ErrorCode.D2107);

                    if (formDataModel.primaryKeyPolicy == 2 && !fieldList.Any(it => it.primaryKey && it.identity))
                        throw Oops.Oh(ErrorCode.D2109);

                    if (templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_id")))
                        throw Oops.Oh(ErrorCode.D2105);

                    // 列表带流程 或者 流程表单 自增ID
                    if (formDataModel.primaryKeyPolicy == 2 && templateEntity.EnableFlow == 1 && !fieldList.Any(it => it.field.ToLower().Equals("f_flow_task_id")))
                        throw Oops.Oh(ErrorCode.D2108);

                    if (formDataModel.logicalDelete && (!fieldList.Any(it => it.field.ToLower().Equals("f_delete_mark")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_time")) || !fieldList.Any(it => it.field.ToLower().Equals("f_delete_user_id"))))
                        throw Oops.Oh(ErrorCode.D2110);

                    // 后端生成
                    codeGenConfigModel = CodeGenWay.SingleTableBackEnd(tableName, fieldList, controls, templateEntity);

                    switch (templateEntity.WebType)
                    {
                        case 1:
                            switch (templateEntity.Type)
                            {
                                case 3:
                                    targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                    templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("1-SingleTable", codeGenConfigModel.IsMapper);
                                    break;
                                default:
                                    targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                    templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                    break;
                            }

                            break;
                        case 2:
                            switch (codeGenConfigModel.TableType)
                            {
                                case 4:
                                    switch (templateEntity.Type)
                                    {
                                        // 流程表单没有行内编辑
                                        case 3:
                                            break;
                                        default:
                                            targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendInlineEditorTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                            break;
                                    }
                                    break;
                                default:
                                    switch (templateEntity.Type)
                                    {
                                        case 3:
                                            targetPathList = CodeGenTargetPathHelper.BackendFlowTargetPathList(codeGenConfigModel.ClassName, fileName, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendFlowTemplatePathList("1-SingleTable", codeGenConfigModel.IsMapper);
                                            break;
                                        default:
                                            targetPathList = CodeGenTargetPathHelper.BackendTargetPathList(codeGenConfigModel.ClassName, fileName, templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.TableType == 4, codeGenConfigModel.IsMapper);
                                            templatePathList = CodeGenTargetPathHelper.BackendTemplatePathList("1-SingleTable", templateEntity.WebType, templateEntity.EnableFlow, codeGenConfigModel.IsMapper);
                                            break;
                                    }
                                    break;
                            }
                            break;
                    }

                    // 生成后端文件
                    for (var i = 0; i < templatePathList.Count; i++)
                    {
                        var tContent = File.ReadAllText(templatePathList[i]);
                        var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                            IsInlineEditor = pcColumnDesignModel.type.Equals(4),
                            IsMainTable = true,
                            NameSpace = codeGenConfigModel.NameSpace,
                            BusName = codeGenConfigModel.BusName,
                            ClassName = codeGenConfigModel.ClassName,
                            PrimaryKey = codeGenConfigModel.PrimaryKey,
                            LowerPrimaryKey = codeGenConfigModel.LowerPrimaryKey,
                            OriginalPrimaryKey = codeGenConfigModel.OriginalPrimaryKey,
                            MainTable = codeGenConfigModel.MainTable,
                            LowerMainTable = codeGenConfigModel.LowerMainTable,
                            OriginalMainTableName = codeGenConfigModel.OriginalMainTableName,
                            hasPage = codeGenConfigModel.hasPage && !codeGenConfigModel.TableType.Equals(3),
                            Function = codeGenConfigModel.Function,
                            TableField = codeGenConfigModel.TableField,
                            RelationsField = codeGenConfigModel.RelationsField,
                            TableFieldCount = codeGenConfigModel.TableField.FindAll(it => !it.PrimaryKey && it.jnpfKey != null).Count(),
                            DefaultSidx = codeGenConfigModel.DefaultSidx,
                            IsExport = codeGenConfigModel.IsExport,
                            IsBatchRemove = codeGenConfigModel.IsBatchRemove,
                            IsUploading = codeGenConfigModel.IsUpload,
                            IsTableRelations = codeGenConfigModel.IsTableRelations,
                            IsMapper = codeGenConfigModel.IsMapper,
                            IsSystemControl = codeGenConfigModel.IsSystemControl,
                            IsUpdate = codeGenConfigModel.IsUpdate,
                            IsBillRule = codeGenConfigModel.IsBillRule,
                            DbLinkId = codeGenConfigModel.DbLinkId,
                            FormId = codeGenConfigModel.FormId,
                            WebType = codeGenConfigModel.WebType,
                            Type = codeGenConfigModel.Type,
                            EnableFlow = codeGenConfigModel.EnableFlow,
                            EnCode = codeGenConfigModel.EnCode,
                            UseDataPermission = useDataPermission,
                            SearchControlNum = codeGenConfigModel.SearchControlNum,
                            ExportField = codeGenConfigModel.ExportField,
                            ConfigId = _userManager.TenantId,
                            DBName = _userManager.TenantDbName,
                            PcUseDataPermission = pcColumnDesignModel.useDataPermission ? "true" : "false",
                            AppUseDataPermission = appColumnDesignModel.useDataPermission ? "true" : "false",
                            FullName = codeGenConfigModel.FullName,
                            IsConversion = codeGenConfigModel.IsConversion,
                            IsDetailConversion = codeGenConfigModel.IsDetailConversion,
                            HasSuperQuery = codeGenConfigModel.HasSuperQuery,
                            PrimaryKeyPolicy = codeGenConfigModel.PrimaryKeyPolicy,
                            ConcurrencyLock = codeGenConfigModel.ConcurrencyLock,
                            IsUnique = codeGenConfigModel.IsUnique,
                            GroupField = codeGenConfigModel.GroupField,
                            GroupShowField = codeGenConfigModel.GroupShowField,
                            IsImportData = codeGenConfigModel.IsImportData,
                            ImportColumnField = CodeGenExportFieldHelper.ImportColumnField(templateEntity, codeGenConfigModel),
                            ParsJnpfKeyConstList = codeGenConfigModel.ParsJnpfKeyConstList,
                            ParsJnpfKeyConstListDetails = codeGenConfigModel.ParsJnpfKeyConstListDetails,
                            ImportDataType = codeGenConfigModel.ImportDataType,
                            DataRuleJson = CodeGenControlsAttributeHelper.GetDataRuleList(templateEntity, codeGenConfigModel),
                            IsSearchMultiple = codeGenConfigModel.IsSearchMultiple,
                            IsTreeTable = codeGenConfigModel.IsTreeTable,
                            ParentField = codeGenConfigModel.ParentField,
                            TreeShowField = codeGenConfigModel.TreeShowField,
                            IsLogicalDelete = codeGenConfigModel.IsLogicalDelete,
                            TableType = codeGenConfigModel.TableType,
                            IsTenantColumn = _tenant.MultiTenancy && _tenant.MultiTenancyType.Equals("COLUMN"),
                            PcKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "pc"),
                            AppKeywordSearchColumn = CodeGenWay.GetCodeGenKeywordSearchColumn(templateEntity, "app"),
                            PcDefaultSortConfig = pcColumnDesignModel.defaultSortConfig != null && pcColumnDesignModel.defaultSortConfig.Any(),
                            AppDefaultSortConfig = appColumnDesignModel.defaultSortConfig != null && appColumnDesignModel.defaultSortConfig.Any(),
                        });
                        var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
                        File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                    }

                    codeGenMainTableConfigModel = codeGenConfigModel;
                }

                break;
        }

        // 强行将文件夹名称定义成 下载代码中输出配置的功能类名
        tableName = formDataModel.className.FirstOrDefault();

        // 还原模板
        controls = TemplateAnalysis.AnalysisTemplateData(fieldsCopy);

        // 捞取子表主键
        if (modelType.Equals(GeneratePatterns.MainBelt) || modelType.Equals(GeneratePatterns.PrimarySecondary))
        {
            foreach (var item in controls)
            {
                if (item.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE))
                    item.TablePrimaryKey = ctPrimaryKey[item.__config__.tableName].ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();
            }
        }

        // 生成前端
        await GenFrondEnd(tableName.ToLowerCase(), codeGenConfigModel.DefaultSidx, formDataModel, controls, codeGenMainTableConfigModel.TableField, templateEntity, fileName);
    }

    /// <summary>
    /// 预览代码.
    /// </summary>
    /// <param name="codePath"></param>
    /// <returns></returns>
    private List<Dictionary<string, object>> PriviewCode(string codePath)
    {
        var dataList = FileHelper.GetAllFiles(codePath);
        List<Dictionary<string, string>> datas = new List<Dictionary<string, string>>();
        List<Dictionary<string, object>> allDatas = new List<Dictionary<string, object>>();
        foreach (var item in dataList)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            var content = FileHelper.FileToString(item.FullName);

            switch (item.Extension)
            {
                case ".cs":
                    {
                        string fileName = item.FullName.ToLower();
                        if (fileName.Contains("listqueryinput") || fileName.Contains("crinput") || fileName.Contains("upinput") || fileName.Contains("upoutput") || fileName.Contains("listoutput") || fileName.Contains("infooutput") || fileName.Contains("detailoutput") || fileName.Contains("inlineeditoroutput"))
                        {
                            data.Add("folderName", "dto");
                        }
                        else if (fileName.Contains("mapper"))
                        {
                            data.Add("folderName", "mapper");
                        }
                        else if (fileName.Contains("entity"))
                        {
                            data.Add("folderName", "entity");
                        }
                        else
                        {
                            data.Add("folderName", "dotnet");
                        }

                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
                case ".fff":
                    {
                        data.Add("folderName", "fff");
                        data.Add("id", SnowflakeIdHelper.NextId());
                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
                case ".vue":
                case ".js":
                case ".ts":
                    {
                        if (item.FullName.ToLower().Contains("app"))
                            data.Add("folderName", "app");
                        else if (item.FullName.ToLower().Contains("pc"))
                            data.Add("folderName", "web");

                        data.Add("id", SnowflakeIdHelper.NextId());
                        data.Add("fileName", item.Name);

                        data.Add("fileContent", content);
                        data.Add("fileType", item.Extension.Replace(".", string.Empty));
                        datas.Add(data);
                    }
                    break;
            }
        }

        // datas 集合去重
        foreach (var item in datas.GroupBy(d => d["folderName"]).Select(d => d.First()).OrderBy(d => d["folderName"]).ToList())
        {
            Dictionary<string, object> dataMap = new Dictionary<string, object>();
            dataMap["fileName"] = item["folderName"];
            dataMap["id"] = SnowflakeIdHelper.NextId();
            dataMap["children"] = datas.FindAll(d => d["folderName"] == item["folderName"]);
            allDatas.Add(dataMap);
        }

        return allDatas;
    }

    /// <summary>
    /// 判断生成模式.
    /// </summary>
    /// <returns>1-纯主表、2-主带子、3-主带副、4-主带副与子.</returns>
    private GeneratePatterns JudgmentGenerationModel(List<DbTableRelationModel> tableRelation, List<FieldsModel> controls)
    {
        // 默认纯主表
        var codeModel = GeneratePatterns.PrimaryTable;

        // 找副表控件
        if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")) && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.PrimarySecondary;
        else if (tableRelation.Count > 1 && controls.Any(x => x.__vModel__.Contains("_jnpf_")))
            codeModel = GeneratePatterns.MainBeltVice;
        else if (tableRelation.Count > 1 && controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)))
            codeModel = GeneratePatterns.MainBelt;
        switch (codeModel)
        {
            case GeneratePatterns.MainBelt:
                // 在子表模式下 设计子表控件数量对不上表扣除主表后数量 强制定义为主子副模式
                if (controls.Count(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) < tableRelation.Count - 1)
                {
                    codeModel = GeneratePatterns.PrimarySecondary;
                }
                break;
        }

        return codeModel;
    }

    /// <summary>
    /// 生成前端.
    /// </summary>
    /// <param name="tableName">表名称.</param>
    /// <param name="defaultSidx">默认排序.</param>
    /// <param name="formDataModel">表单JSON包.</param>
    /// <param name="controls">移除布局控件后的控件列表.</param>
    /// <param name="tableColumns">表字段.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="fileName">文件夹名称.</param>
    private async Task GenFrondEnd(string tableName, string defaultSidx, FormDataModel formDataModel, List<FieldsModel> controls, List<TableColumnConfigModel> tableColumns, VisualDevEntity templateEntity, string fileName)
    {
        var categoryName = (await _dictionaryDataService.GetInfo(templateEntity.Category)).EnCode;
        List<string> targetPathList = new List<string>();
        List<string> templatePathList = new List<string>();

        FrontEndGenConfigModel frondEndGenConfig = new FrontEndGenConfigModel();
        switch (_userManager.VueVersion)
        {
            case 3:
                {
                    CodeGenFrontEndConfigModel frontEndGenConfig = new CodeGenFrontEndConfigModel();

                    // 前端生成 APP与PC合并 4-pc,5-app
                    foreach (int logic in new List<int> { 4 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frontEndGenConfig = CodeGenWay.CodeGenFrontEndEngine(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig.BasicInfo.MianTable = tableName;
                        frontEndGenConfig.BasicInfo.CreatorUserId = _userManager.UserId;

                        switch (templateEntity.Type)
                        {
                            case 3:
                                {
                                    targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                    templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                }

                                break;
                            default:
                                {
                                    switch (logic)
                                    {
                                        case 4:
                                            var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                            switch (templateEntity.WebType)
                                            {
                                                case 1:
                                                    frontEndGenConfig.TableConfig.HasSuperQuery = false;
                                                    frontEndGenConfig.HasSearch = false;
                                                    frontEndGenConfig.Type = 1;
                                                    break;
                                            }

                                            switch (frontEndGenConfig.Type)
                                            {
                                                case 4:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                    break;
                                                default:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, frontEndGenConfig.HasFlow, frontEndGenConfig.HasDetail, frontEndGenConfig.TableConfig.HasSuperQuery);
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                break;
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, frontEndGenConfig, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }

                    foreach (int logic in new List<int> { 5 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frondEndGenConfig = CodeGenWay.SingleTableFrontEnd(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig = CodeGenWay.CodeGenFrontEndEngine(logic, formDataModel, controls, tableColumns, templateEntity);
                        frontEndGenConfig.BasicInfo.MianTable = tableName;
                        frontEndGenConfig.BasicInfo.CreatorUserId = _userManager.UserId;

                        switch (templateEntity.Type)
                        {
                            case 3:
                                {
                                    targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                    templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                }

                                break;
                            default:
                                {
                                    switch (logic)
                                    {
                                        case 4:
                                            var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                            var hasSuperQuery = false;
                                            switch (templateEntity.WebType)
                                            {
                                                case 1:
                                                    hasSuperQuery = false;
                                                    frondEndGenConfig.Type = 1;
                                                    break;
                                                default:
                                                    hasSuperQuery = columnDesignModel.hasSuperQuery;
                                                    break;
                                            }

                                            switch (frondEndGenConfig.Type)
                                            {
                                                case 4:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                                default:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                            }
                                            break;
                                        case 5:
                                            switch (templateEntity.EnableFlow)
                                            {
                                                case 0:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndTemplatePathList(templateEntity.WebType, frondEndGenConfig.IsDetail, true);
                                                    break;
                                                case 1:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTargetPathList(tableName, fileName, templateEntity.WebType);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTemplatePathList(templateEntity.WebType, true);
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                break;
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                NameSpace = frondEndGenConfig.NameSpace,
                                ClassName = frondEndGenConfig.ClassName,
                                FormRef = frondEndGenConfig.FormRef,
                                FormModel = frondEndGenConfig.FormModel,
                                Size = frondEndGenConfig.Size,
                                LabelPosition = frondEndGenConfig.LabelPosition,
                                LabelWidth = frondEndGenConfig.LabelWidth,
                                FormRules = frondEndGenConfig.FormRules,
                                GeneralWidth = frondEndGenConfig.GeneralWidth,
                                FullScreenWidth = frondEndGenConfig.FullScreenWidth,
                                DrawerWidth = frondEndGenConfig.DrawerWidth,
                                FormStyle = frondEndGenConfig.FormStyle,
                                Type = frondEndGenConfig.Type,
                                TreeRelation = frondEndGenConfig.TreeRelation,
                                TreeSelectType = frondEndGenConfig.TreeSelectType,
                                TreeAbleIds = frondEndGenConfig.TreeAbleIds,
                                TreeJnpfKey = frondEndGenConfig.TreeJnpfKey,
                                TreeTitle = frondEndGenConfig.TreeTitle,
                                TreePropsValue = frondEndGenConfig.TreePropsValue,
                                TreeDataSource = frondEndGenConfig.TreeDataSource,
                                TreeDictionary = frondEndGenConfig.TreeDictionary,
                                TreePropsUrl = frondEndGenConfig.TreePropsUrl,
                                TreePropsLabel = frondEndGenConfig.TreePropsLabel,
                                TreePropsChildren = frondEndGenConfig.TreePropsChildren,
                                TreeRelationControlKey = frondEndGenConfig.TreeRelationControlKey,
                                IsTreeRelationMultiple = frondEndGenConfig.IsTreeRelationMultiple,
                                IsExistQuery = frondEndGenConfig.IsExistQuery,
                                PrimaryKey = frondEndGenConfig.PrimaryKey,
                                FormList = frondEndGenConfig.FormList,
                                PopupType = frondEndGenConfig.PopupType,
                                SearchColumnDesign = frondEndGenConfig.SearchColumnDesign,
                                IsKeywordSearchColumn = frondEndGenConfig.SearchColumnDesign?.Any(x => x.IsKeyword),
                                IsAnyDefaultSearch = frondEndGenConfig.SearchColumnDesign?.Any(x => x.DefaultValues != "undefined"),
                                SortFieldDesign = frondEndGenConfig.SortFieldDesign,
                                TopButtonDesign = frondEndGenConfig.TopButtonDesign,
                                ColumnButtonDesign = frondEndGenConfig.ColumnButtonDesign,
                                ColumnDesign = frondEndGenConfig.ColumnDesign,
                                OptionsList = frondEndGenConfig.OptionsList,
                                IsBatchRemoveDel = frondEndGenConfig.IsBatchRemoveDel,
                                IsBatchPrint = frondEndGenConfig.IsBatchPrint,
                                PrintIds = frondEndGenConfig.PrintIds,
                                IsDownload = frondEndGenConfig.IsDownload,
                                IsRemoveDel = frondEndGenConfig.IsRemoveDel,
                                IsDetail = frondEndGenConfig.IsDetail,
                                IsEdit = frondEndGenConfig.IsEdit,
                                IsAdd = frondEndGenConfig.IsAdd,
                                IsSort = frondEndGenConfig.IsSort,
                                IsUpload = frondEndGenConfig.IsUpload,
                                FormAllContols = frondEndGenConfig.FormAllContols,
                                ComplexFormAllContols = frondEndGenConfig.ComplexFormAllContols,
                                CancelButtonText = frondEndGenConfig.CancelButtonText,
                                ConfirmButtonText = frondEndGenConfig.ConfirmButtonText,
                                UseBtnPermission = frondEndGenConfig.UseBtnPermission,
                                UseColumnPermission = frondEndGenConfig.UseColumnPermission,
                                UseFormPermission = frondEndGenConfig.UseFormPermission,
                                DefaultSidx = defaultSidx,
                                WebType = templateEntity.Type == 3 ? templateEntity.Type : templateEntity.WebType,
                                HasPage = frondEndGenConfig.HasPage,
                                IsSummary = frondEndGenConfig.IsSummary,
                                AddTitleName = frondEndGenConfig.TopButtonDesign?.Find(it => it.Value.Equals("add"))?.Label,
                                EditTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("edit"))?.Label,
                                DetailTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("detail"))?.Label,
                                PageSize = frondEndGenConfig.PageSize,
                                Sort = frondEndGenConfig.Sort,
                                HasPrintBtn = frondEndGenConfig.HasPrintBtn,
                                PrintButtonText = frondEndGenConfig.PrintButtonText,
                                PrintId = frondEndGenConfig.PrintId,
                                EnCode = templateEntity.EnCode,
                                FormId = templateEntity.Id,
                                FullName = templateEntity.FullName,
                                Category = categoryName,
                                Tables = templateEntity.Tables.ToJsonString(),
                                DbLinkId = templateEntity.DbLinkId,
                                MianTable = tableName,
                                PropertyJson = frondEndGenConfig.PropertyJson.ToJsonString(),
                                CreatorTime = DateTime.Now.ParseToUnixTime(),
                                CreatorUserId = _userManager.UserId,
                                IsChildDataTransfer = frondEndGenConfig.IsChildDataTransfer,
                                IsChildTableQuery = frondEndGenConfig.IsChildTableQuery,
                                IsChildTableShow = frondEndGenConfig.IsChildTableShow,
                                ColumnList = frondEndGenConfig.ColumnList,
                                HasSuperQuery = frondEndGenConfig.HasSuperQuery,
                                ColumnOptions = frondEndGenConfig.ColumnOptions,
                                IsInlineEditor = frondEndGenConfig.IsInlineEditor,
                                IndexDataType = frondEndGenConfig.IndexDataType,
                                GroupField = frondEndGenConfig.GroupField,
                                GroupShowField = frondEndGenConfig.GroupShowField,
                                PrimaryKeyPolicy = frondEndGenConfig.PrimaryKeyPolicy,
                                IsRelationForm = frondEndGenConfig.IsRelationForm,
                                ChildTableStyle = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) ? frondEndGenConfig.ChildTableStyle : 1,
                                IsFixed = frondEndGenConfig.IsFixed,
                                IsChildrenRegular = frondEndGenConfig.IsChildrenRegular,
                                TreeSynType = frondEndGenConfig.TreeSynType,
                                HasTreeQuery = frondEndGenConfig.HasTreeQuery,
                                ColumnData = frondEndGenConfig.ColumnData.ToJsonString(),
                                SummaryField = frondEndGenConfig.SummaryField.ToJsonString(),
                                ShowSummary = frondEndGenConfig.ShowSummary,
                                DefaultFormControlList = frondEndGenConfig.DefaultFormControlList,
                                IsDefaultFormControl = frondEndGenConfig.IsDefaultFormControl,
                                FormRealControl = frondEndGenConfig.FormRealControl,
                                QueryCriteriaQueryVarianceList = frondEndGenConfig.QueryCriteriaQueryVarianceList,
                                IsDateSpecialAttribute = frondEndGenConfig.IsDateSpecialAttribute,
                                IsTimeSpecialAttribute = frondEndGenConfig.IsTimeSpecialAttribute,
                                AllThousandsField = frondEndGenConfig.AllThousandsField,
                                IsChildrenThousandsField = frondEndGenConfig.IsChildrenThousandsField,
                                SpecifyDateFormatSet = frondEndGenConfig.SpecifyDateFormatSet,
                                AppThousandField = frondEndGenConfig.AppThousandField,
                                HasConfirmAndAddBtn = frondEndGenConfig.HasConfirmAndAddBtn,
                                ConfirmAndAddText = frondEndGenConfig.ConfirmAndAddText,
                                IsDefaultSearchField = frondEndGenConfig.IsDefaultSearchField,
                                DefaultSearchList = frondEndGenConfig.DefaultSearchList,
                                ShowOverflow = frondEndGenConfig.ShowOverflow,
                                TableConfig = frontEndGenConfig.TableConfig,
                                VueVersion = _userManager.VueVersion,
                            }, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }
                }
                break;
            default:
                {
                    // 前端生成 APP与PC合并 4-pc,5-app
                    foreach (int logic in new List<int> { 4, 5 })
                    {
                        // 每次循环前重新定义表单数据
                        formDataModel = templateEntity.FormData.ToObject<FormDataModel>();

                        frondEndGenConfig = CodeGenWay.SingleTableFrontEnd(logic, formDataModel, controls, tableColumns, templateEntity);
                        var defaultSortConfig = "[]";
                        switch (templateEntity.Type)
                        {
                            case 3:
                                {
                                    targetPathList = CodeGenTargetPathHelper.FlowFrontEndTargetPathList(logic, tableName, fileName);
                                    templatePathList = CodeGenTargetPathHelper.FlowFrontEndTemplatePathList(logic, _userManager.VueVersion);
                                }

                                break;
                            default:
                                {
                                    switch (logic)
                                    {
                                        case 4:
                                            var columnDesignModel = templateEntity.ColumnData?.ToObject<ColumnDesignModel>();
                                            defaultSortConfig = columnDesignModel?.defaultSortConfig != null ? columnDesignModel.defaultSortConfig?.ToJsonString() : "[]";
                                            var hasSuperQuery = false;
                                            switch (templateEntity.WebType)
                                            {
                                                case 1:
                                                    hasSuperQuery = false;
                                                    frondEndGenConfig.Type = 1;
                                                    break;
                                                default:
                                                    hasSuperQuery = columnDesignModel.hasSuperQuery;
                                                    break;
                                            }

                                            switch (frondEndGenConfig.Type)
                                            {
                                                case 4:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndInlineEditorTargetPathList(tableName, fileName, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndInlineEditorTemplatePathList(templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                                default:
                                                    targetPathList = CodeGenTargetPathHelper.FrontEndTargetPathList(tableName, fileName, templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    templatePathList = CodeGenTargetPathHelper.FrontEndTemplatePathList(templateEntity.WebType, templateEntity.EnableFlow, frondEndGenConfig.IsDetail, hasSuperQuery);
                                                    break;
                                            }
                                            break;
                                        case 5:
                                            columnDesignModel = templateEntity.AppColumnData?.ToObject<ColumnDesignModel>();
                                            defaultSortConfig = columnDesignModel?.defaultSortConfig != null ? columnDesignModel.defaultSortConfig?.ToJsonString() : "[]";
                                            switch (templateEntity.EnableFlow)
                                            {
                                                case 0:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndTargetPathList(tableName, fileName, templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndTemplatePathList(templateEntity.WebType, frondEndGenConfig.IsDetail);
                                                    break;
                                                case 1:
                                                    targetPathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTargetPathList(tableName, fileName, templateEntity.WebType);
                                                    templatePathList = CodeGenTargetPathHelper.AppFrontEndWorkflowTemplatePathList(templateEntity.WebType);
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                break;
                        }

                        for (int i = 0; i < templatePathList.Count; i++)
                        {
                            string tContent = File.ReadAllText(templatePathList[i]);
                            var tResult = _viewEngine.RunCompileFromCached(tContent, new {
                                NameSpace = frondEndGenConfig.NameSpace,
                                ClassName = frondEndGenConfig.ClassName,
                                FormRef = frondEndGenConfig.FormRef,
                                FormModel = frondEndGenConfig.FormModel,
                                Size = frondEndGenConfig.Size,
                                LabelPosition = frondEndGenConfig.LabelPosition,
                                LabelWidth = frondEndGenConfig.LabelWidth,
                                FormRules = frondEndGenConfig.FormRules,
                                GeneralWidth = frondEndGenConfig.GeneralWidth,
                                FullScreenWidth = frondEndGenConfig.FullScreenWidth,
                                DrawerWidth = frondEndGenConfig.DrawerWidth,
                                FormStyle = frondEndGenConfig.FormStyle,
                                Type = frondEndGenConfig.Type,
                                TreeRelation = frondEndGenConfig.TreeRelation,
                                TreeSelectType = frondEndGenConfig.TreeSelectType,
                                TreeAbleIds = frondEndGenConfig.TreeAbleIds,
                                TreeJnpfKey = frondEndGenConfig.TreeJnpfKey,
                                TreeTitle = frondEndGenConfig.TreeTitle,
                                TreePropsValue = frondEndGenConfig.TreePropsValue,
                                TreeDataSource = frondEndGenConfig.TreeDataSource,
                                TreeDictionary = frondEndGenConfig.TreeDictionary,
                                TreePropsUrl = frondEndGenConfig.TreePropsUrl,
                                TreePropsLabel = frondEndGenConfig.TreePropsLabel,
                                TreePropsChildren = frondEndGenConfig.TreePropsChildren,
                                TreeRelationControlKey = frondEndGenConfig.TreeRelationControlKey,
                                IsTreeRelationMultiple = frondEndGenConfig.IsTreeRelationMultiple,
                                IsExistQuery = frondEndGenConfig.IsExistQuery,
                                PrimaryKey = frondEndGenConfig.PrimaryKey,
                                FormList = frondEndGenConfig.FormList,
                                PopupType = frondEndGenConfig.PopupType,
                                SearchColumnDesign = frondEndGenConfig.SearchColumnDesign,
                                IsKeywordSearchColumn = frondEndGenConfig.SearchColumnDesign?.Any(x => x.IsKeyword),
                                IsAnyDefaultSearch = frondEndGenConfig.SearchColumnDesign?.Any(x => x.DefaultValues != "undefined"),
                                SortFieldDesign = frondEndGenConfig.SortFieldDesign,
                                TopButtonDesign = frondEndGenConfig.TopButtonDesign,
                                ColumnButtonDesign = frondEndGenConfig.ColumnButtonDesign,
                                ColumnDesign = frondEndGenConfig.ColumnDesign,
                                OptionsList = frondEndGenConfig.OptionsList,
                                IsBatchRemoveDel = frondEndGenConfig.IsBatchRemoveDel,
                                IsBatchPrint = frondEndGenConfig.IsBatchPrint,
                                PrintIds = frondEndGenConfig.PrintIds,
                                IsDownload = frondEndGenConfig.IsDownload,
                                IsRemoveDel = frondEndGenConfig.IsRemoveDel,
                                IsDetail = frondEndGenConfig.IsDetail,
                                IsEdit = frondEndGenConfig.IsEdit,
                                IsAdd = frondEndGenConfig.IsAdd,
                                IsSort = frondEndGenConfig.IsSort,
                                IsUpload = frondEndGenConfig.IsUpload,
                                FormAllContols = frondEndGenConfig.FormAllContols,
                                ComplexFormAllContols = frondEndGenConfig.ComplexFormAllContols,
                                CancelButtonText = frondEndGenConfig.CancelButtonText,
                                ConfirmButtonText = frondEndGenConfig.ConfirmButtonText,
                                UseBtnPermission = frondEndGenConfig.UseBtnPermission,
                                UseColumnPermission = frondEndGenConfig.UseColumnPermission,
                                UseFormPermission = frondEndGenConfig.UseFormPermission,
                                DefaultSidx = defaultSidx,
                                WebType = templateEntity.Type == 3 ? templateEntity.Type : templateEntity.WebType,
                                HasPage = frondEndGenConfig.HasPage,
                                IsSummary = frondEndGenConfig.IsSummary,
                                AddTitleName = frondEndGenConfig.TopButtonDesign?.Find(it => it.Value.Equals("add"))?.Label,
                                EditTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("edit"))?.Label,
                                DetailTitleName = frondEndGenConfig.ColumnButtonDesign?.Find(it => it.Value.Equals("detail"))?.Label,
                                PageSize = frondEndGenConfig.PageSize,
                                Sort = frondEndGenConfig.Sort,
                                HasPrintBtn = frondEndGenConfig.HasPrintBtn,
                                PrintButtonText = frondEndGenConfig.PrintButtonText,
                                PrintId = frondEndGenConfig.PrintId,
                                EnCode = templateEntity.EnCode,
                                FormId = templateEntity.Id,
                                FullName = templateEntity.FullName,
                                Category = categoryName,
                                Tables = templateEntity.Tables.ToJsonString(),
                                DbLinkId = templateEntity.DbLinkId,
                                MianTable = tableName,
                                PropertyJson = frondEndGenConfig.PropertyJson.ToJsonString(),
                                CreatorTime = DateTime.Now.ParseToUnixTime(),
                                CreatorUserId = _userManager.UserId,
                                IsChildDataTransfer = frondEndGenConfig.IsChildDataTransfer,
                                IsChildTableQuery = frondEndGenConfig.IsChildTableQuery,
                                IsChildTableShow = frondEndGenConfig.IsChildTableShow,
                                ColumnList = frondEndGenConfig.ColumnList,
                                HasSuperQuery = frondEndGenConfig.HasSuperQuery,
                                ColumnOptions = frondEndGenConfig.ColumnOptions,
                                IsInlineEditor = frondEndGenConfig.IsInlineEditor,
                                IndexDataType = frondEndGenConfig.IndexDataType,
                                GroupField = frondEndGenConfig.GroupField,
                                GroupShowField = frondEndGenConfig.GroupShowField,
                                PrimaryKeyPolicy = frondEndGenConfig.PrimaryKeyPolicy,
                                IsRelationForm = frondEndGenConfig.IsRelationForm,
                                ChildTableStyle = controls.Any(it => it.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)) ? frondEndGenConfig.ChildTableStyle : 1,
                                IsFixed = frondEndGenConfig.IsFixed,
                                IsChildrenRegular = frondEndGenConfig.IsChildrenRegular,
                                TreeSynType = frondEndGenConfig.TreeSynType,
                                HasTreeQuery = frondEndGenConfig.HasTreeQuery,
                                ColumnData = frondEndGenConfig.ColumnData.ToJsonString(),
                                SummaryField = frondEndGenConfig.SummaryField.ToJsonString(),
                                ShowSummary = frondEndGenConfig.ShowSummary,
                                DefaultFormControlList = frondEndGenConfig.DefaultFormControlList,
                                IsDefaultFormControl = frondEndGenConfig.IsDefaultFormControl,
                                FormRealControl = frondEndGenConfig.FormRealControl,
                                QueryCriteriaQueryVarianceList = frondEndGenConfig.QueryCriteriaQueryVarianceList,
                                IsDateSpecialAttribute = frondEndGenConfig.IsDateSpecialAttribute,
                                IsTimeSpecialAttribute = frondEndGenConfig.IsTimeSpecialAttribute,
                                AllThousandsField = frondEndGenConfig.AllThousandsField,
                                IsChildrenThousandsField = frondEndGenConfig.IsChildrenThousandsField,
                                SpecifyDateFormatSet = frondEndGenConfig.SpecifyDateFormatSet,
                                AppThousandField = frondEndGenConfig.AppThousandField,
                                HasConfirmAndAddBtn = frondEndGenConfig.HasConfirmAndAddBtn,
                                ConfirmAndAddText = frondEndGenConfig.ConfirmAndAddText,
                                IsDefaultSearchField = frondEndGenConfig.IsDefaultSearchField,
                                DefaultSearchList = frondEndGenConfig.DefaultSearchList,
                                ShowOverflow = frondEndGenConfig.ShowOverflow,
                                DefaultSortConfig = defaultSortConfig,
                                VueVersion = 2,
                            }, builderAction: builder =>
                            {
                                builder.AddUsing("JNPF.Engine.Entity.Model.CodeGen");
                                builder.AddAssemblyReferenceByName("JNPF.Engine.Entity");
                            });
                            var dirPath = new DirectoryInfo(targetPathList[i]).Parent.FullName;
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            File.WriteAllText(targetPathList[i], tResult, Encoding.UTF8);
                        }
                    }
                }
                break;
        }

        #endregion
    }
}
