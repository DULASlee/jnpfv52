using JNPF.Common.Const;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using SqlSugar;

namespace JNPF.VisualDev.Engine.Core;

/// <summary>
/// 模板解析 基础类.
/// </summary>
public class TemplateParsingBase
{
    public TemplateParsingBase() { }

    /// <summary>
    /// 模板实体.
    /// </summary>
    public VisualDevEntity visualDevEntity { get; set; }

    /// <summary>
    /// 页面类型 （1、纯表单，2、表单加列表，3、表单列表工作流）.
    /// </summary>
    public int WebType { get; set; }

    /// <summary>
    /// 是否有表 (true 有表, false 无表).
    /// </summary>
    public bool IsHasTable { get; set; }

    /// <summary>
    /// 表单配置JSON模型.
    /// </summary>
    public FormDataModel? FormModel { get; set; }

    /// <summary>
    /// 列配置JSON模型.
    /// </summary>
    public ColumnDesignModel ColumnData { get; set; }

    /// <summary>
    /// App列配置JSON模型.
    /// </summary>
    public ColumnDesignModel AppColumnData { get; set; }

    /// <summary>
    /// 所有控件集合.
    /// </summary>
    public List<FieldsModel> AllFieldsModel { get; set; }

    /// <summary>
    /// 所有控件集合(已剔除布局控件).
    /// </summary>
    public List<FieldsModel> FieldsModelList { get; set; }

    /// <summary>
    /// 主表控件集合.
    /// </summary>
    public List<FieldsModel> MainTableFieldsModelList { get; set; }

    /// <summary>
    /// 副表控件集合.
    /// </summary>
    public List<FieldsModel> AuxiliaryTableFieldsModelList { get; set; }

    /// <summary>
    /// 子表控件集合.
    /// </summary>
    public List<FieldsModel> ChildTableFieldsModelList { get; set; }

    /// <summary>
    /// 主/副表控件集合(列表展示数据控件).
    /// </summary>
    public List<FieldsModel> SingleFormData { get; set; }

    /// <summary>
    /// 所有表.
    /// </summary>
    public List<TableModel> AllTable { get; set; }

    /// <summary>
    /// 主表.
    /// </summary>
    public TableModel? MainTable { get; set; }

    /// <summary>
    /// 主表 表名.
    /// </summary>
    public string? MainTableName { get; set; }

    /// <summary>
    /// 主/副表 系统生成控件集合.
    /// </summary>
    public List<FieldsModel> GenerateFields { get; set; }

    /// <summary>
    /// 主表 vModel 字段 字典.
    /// Key : vModel , Value : 主表.vModel.
    /// </summary>
    public Dictionary<string, string> MainTableFields { get; set; }

    /// <summary>
    /// 副表 vModel 字段 字典.
    /// Key : vModel , Value : 副表.vModel.
    /// </summary>
    public Dictionary<string, string> AuxiliaryTableFields { get; set; }

    /// <summary>
    /// 子表 vModel 字段 字典.
    /// Key : 设计子表-vModel , Value : 子表.vModel.
    /// </summary>
    public Dictionary<string, string> ChildTableFields { get; set; }

    /// <summary>
    /// 所有表 vModel 字段 字典.
    /// Key : 设计子表-vModel , Value : 表.vModel.
    /// </summary>
    public Dictionary<string, string> AllTableFields { get; set; }

    /// <summary>
    /// 功能名称.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// 主表主键名.
    /// </summary>
    public string MainPrimary { get; set; }

    /// <summary>
    /// 数据库连接.
    /// </summary>
    public DbLinkEntity DbLink { get; set; }

    /// <summary>
    /// 导入模式.(1 仅新增，2 更新和新增数据).
    /// </summary>
    public string dataType { get; set; } = "1";

    /// <summary>
    /// 导入数据列表.
    /// </summary>
    public List<string> selectKey { get; set; }

    /// <summary>
    /// PC数据过滤 .
    /// </summary>
    public List<object> DataRuleListJson { get; set; }

    /// <summary>
    /// App数据过滤 .
    /// </summary>
    public List<object> AppDataRuleListJson { get; set; }

    /// <summary>
    /// 模板解析帮助 构造 (功能表单).
    /// </summary>
    /// <param name="formJson">表单Json.</param>
    /// <param name="tables">涉及表Json.</param>
    /// <param name="isFlowTask"></param>
    public TemplateParsingBase(string formJson, string tables, bool isFlowTask = false)
    {
        InitByFormType(formJson, tables, 2);
    }

    /// <summary>
    /// 模板解析帮助 构造.
    /// </summary>
    /// <param name="formJson">表单Json.</param>
    /// <param name="tables">涉及表Json.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    public TemplateParsingBase(string formJson, string tables, int formType)
    {
        InitByFormType(formJson, tables, formType);
    }

    /// <summary>
    /// 模板解析帮助 构造.
    /// </summary>
    /// <param name="entity">功能实体</param>
    /// <param name="isFlowTask"></param>
    public TemplateParsingBase(VisualDevEntity entity, bool isFlowTask = false)
    {
        visualDevEntity = entity;
        WebType = entity.WebType;

        //modify by harry 
        //this.DbLink  =  _runService.GetDbLink(entity.DbLinkId);

        if (entity.FlowId.IsNotEmptyOrNull() && entity.EnableFlow.Equals(1)) WebType = 3;

        // 数据视图
        if (entity.WebType.Equals(4))
        {
            FullName = entity.FullName;
            IsHasTable = false;
            InitColumnData(entity);
            AllFieldsModel = new List<FieldsModel>();
            ColumnData.columnList.ForEach(item =>
            {
                AllFieldsModel.Add(new FieldsModel() { __vModel__ = item.__vModel__, __config__ = new ConfigModel() { label = item.label, jnpfKey = item.__config__.jnpfKey } });
            });
            AppColumnData.columnList.ForEach(item =>
            {
                AllFieldsModel.Add(new FieldsModel() { __vModel__ = item.__vModel__, __config__ = new ConfigModel() { label = item.label, jnpfKey = item.__config__.jnpfKey } });
            });
            AllFieldsModel = AllFieldsModel.DistinctBy(x => x.__vModel__).ToList();
            FieldsModelList = AllFieldsModel;
            AuxiliaryTableFieldsModelList = AllFieldsModel;
            MainTableFieldsModelList = AllFieldsModel;
            SingleFormData = AllFieldsModel;
        }
        else
        {
            FormDataModel formModel = entity.FormData.ToObjectOld<FormDataModel>();
            DataFormatReplace(formModel.fields);
            FormModel = formModel; // 表单Json模型
            IsHasTable = !string.IsNullOrEmpty(entity.Tables) && !"[]".Equals(entity.Tables); // 是否有表
            AllFieldsModel = TemplateAnalysis.AnalysisTemplateData(formModel.fields.ToJsonString().ToObjectOld<List<FieldsModel>>()); // 所有控件集合
            FieldsModelList = TemplateAnalysis.AnalysisTemplateData(formModel.fields); // 已剔除布局控件集合
            MainTable = entity.Tables.ToList<TableModel>().Find(m => m.typeId.Equals("1")); // 主表
            MainTableName = MainTable?.table; // 主表名称
            AddChlidTableFeildsModel();

            // 处理旧控件 部分没有 tableName
            FieldsModelList.Where(x => string.IsNullOrWhiteSpace(x.__config__.tableName)).ToList().ForEach(item =>
            {
                if (item.__vModel__.Contains("_jnpf_")) item.__config__.tableName = item.__vModel__.ReplaceRegex(@"_jnpf_(\w+)", string.Empty).Replace("jnpf_", string.Empty); // 副表
                else item.__config__.tableName = MainTableName != null ? MainTableName : string.Empty; // 主表
            });
            AllTable = entity.Tables.ToObject<List<TableModel>>(); // 所有表
            AuxiliaryTableFieldsModelList = FieldsModelList.Where(x => x.__vModel__.Contains("_jnpf_")).ToList(); // 单控件副表集合
            ChildTableFieldsModelList = FieldsModelList.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList(); // 子表集合
            MainTableFieldsModelList = FieldsModelList.Except(AuxiliaryTableFieldsModelList).Except(ChildTableFieldsModelList).ToList(); // 主表控件集合
            SingleFormData = FieldsModelList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList(); // 非子表集合
            GenerateFields = GetGenerateFields(); // 系统生成控件

            MainTableFields = new Dictionary<string, string>();
            AuxiliaryTableFields = new Dictionary<string, string>();
            ChildTableFields = new Dictionary<string, string>();
            AllTableFields = new Dictionary<string, string>();
            MainTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
            {
                MainTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
                AllTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
            });
            AuxiliaryTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
            {
                AuxiliaryTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
                AllTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
            });
            ChildTableFieldsModelList.ForEach(item =>
            {
                item.__config__.children.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
                {
                    ChildTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
                    AllTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
                });
            });
            InitColumnData(entity);
        }
    }

    /// <summary>
    /// 模板解析帮助 构造(代码生成用).
    /// </summary>
    /// <param name="dblink">数据连接.</param>
    /// <param name="fieldList">控件集合.</param>
    /// <param name="tables">主/副/子 表.</param>
    /// <param name="mainPrimary">主表主键.</param>
    /// <param name="webType">页面类型 （1、纯表单，2、表单加列表，3、表单列表工作流）.</param>
    /// <param name="primaryKeyPolicy">主键策略(1 雪花ID 2 自增长ID).</param>
    /// <param name="uploaderKey">导入导出数据列名集合.</param>
    /// <param name="_dataType">导入类型 1 新增, 2 新增和修改.</param>
    /// <param name="enableFlow">是否开启流程 1 开启.</param>
    /// <param name="flowFormId">流程表单Id.</param>
    public TemplateParsingBase(
        DbLinkEntity dblink,
        List<FieldsModel> fieldList,
        List<DbTableRelationModel> tables,
        string mainPrimary,
        int webType,
        int primaryKeyPolicy,
        List<string> uploaderKey,
        string _dataType,
        int enableFlow = 0,
        string flowFormId = "")
    {
        if (enableFlow.Equals(1)) visualDevEntity = new VisualDevEntity() { EnableFlow = 1, Id = flowFormId };
        else visualDevEntity = new VisualDevEntity();
        DbLink = dblink;
        AllTable = tables.ToObject<List<TableModel>>(); // 所有表
        var complexHeaderList = GetComplexHeaderList(fieldList, uploaderKey);
        DataFormatReplace(fieldList);
        FieldsModelList = fieldList;
        AllFieldsModel = FieldsModelList.Copy();
        MainTable = AllTable.Find(m => m.typeId.Equals("1")); // 主表
        MainTableName = MainTable?.table; // 主表名称
        MainPrimary = mainPrimary;
        AddCodeGenChlidTableFeildsModel();

        // 处理旧控件 部分没有 tableName
        FieldsModelList.Where(x => string.IsNullOrWhiteSpace(x.__config__.tableName)).ToList().ForEach(item =>
        {
            if (item.__vModel__.Contains("_jnpf_")) item.__config__.tableName = item.__vModel__.ReplaceRegex(@"_jnpf_(\w+)", string.Empty).Replace("jnpf_", string.Empty); // 副表
            else item.__config__.tableName = MainTableName != null ? MainTableName : string.Empty; // 主表
        });
        AuxiliaryTableFieldsModelList = FieldsModelList.Where(x => x.__vModel__.Contains("_jnpf_")).ToList(); // 单控件副表集合
        ChildTableFieldsModelList = FieldsModelList.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList(); // 子表集合
        MainTableFieldsModelList = FieldsModelList.Except(AuxiliaryTableFieldsModelList).Except(ChildTableFieldsModelList).ToList(); // 主表控件集合
        SingleFormData = FieldsModelList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList(); // 非子表集合
        GenerateFields = GetGenerateFields(); // 系统生成控件

        MainTableFields = new Dictionary<string, string>();
        AuxiliaryTableFields = new Dictionary<string, string>();
        ChildTableFields = new Dictionary<string, string>();
        AllTableFields = new Dictionary<string, string>();
        MainTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
        {
            MainTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
            AllTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
        });
        AuxiliaryTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
        {
            AuxiliaryTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
            AllTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
        });
        ChildTableFieldsModelList.ForEach(item =>
        {
            item.__config__.children.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
            {
                ChildTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
                AllTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
            });
        });

        WebType = webType;
        FormModel = new FormDataModel();
        FormModel.primaryKeyPolicy = primaryKeyPolicy;
        ColumnData = new ColumnDesignModel();
        ColumnData.type = 1;
        if (complexHeaderList != null && complexHeaderList.Any()) ColumnData.complexHeaderList = complexHeaderList;
        AppColumnData = new ColumnDesignModel();
        selectKey = uploaderKey;
        dataType = _dataType;
    }

    /// <summary>
    /// 验证模板.
    /// </summary>
    /// <returns>true 通过.</returns>
    public bool VerifyTemplate()
    {
        if (FieldsModelList != null && FieldsModelList.Any(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE))
        {
            foreach (FieldsModel? item in ChildTableFieldsModelList)
            {
                FieldsModel? tc = AuxiliaryTableFieldsModelList.Find(x => x.__vModel__.Contains(item.__config__.tableName + "_jnpf_"));
                if (tc != null) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 获取系统生成字段空格键.
    /// </summary>
    /// <returns></returns>
    public List<FieldsModel> GetGenerateFields()
    {
        // 系统生成字段 key
        var gfList = new List<string>() { JnpfKeyConst.BILLRULE, JnpfKeyConst.CREATEUSER, JnpfKeyConst.CREATETIME, JnpfKeyConst.MODIFYUSER, JnpfKeyConst.MODIFYTIME, JnpfKeyConst.CURRPOSITION, JnpfKeyConst.CURRORGANIZE, JnpfKeyConst.UPLOADFZ };

        return SingleFormData.Where(x => gfList.Contains(x.__config__.jnpfKey)).ToList();
    }

    /// <summary>
    /// 处理子表内的控件 添加到所有控件.
    /// </summary>
    private void AddChlidTableFeildsModel()
    {
        var ctList = new List<FieldsModel>();
        AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList().ForEach(item =>
        {
            item.__config__.children.Where(it => it.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(it => it.__vModel__ = item.__vModel__ + "-" + it.__vModel__);
            ctList.AddRange(TemplateAnalysis.AnalysisTemplateData(item.__config__.children));
        });
        AllFieldsModel.AddRange(ctList);
    }

    /// <summary>
    /// 处理子表内的控件 添加到所有控件.
    /// </summary>
    private void AddCodeGenChlidTableFeildsModel()
    {
        var ctList = new List<FieldsModel>();
        AllFieldsModel.Where(x => x.__config__.jnpfKey.Equals(JnpfKeyConst.TABLE)).ToList().ForEach(item =>
        {
            item.__config__.children.Where(it => it.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(it =>
            {
                it.__config__.label = it.__config__.label.Replace(it.__vModel__, item.__vModel__ + "-" + it.__vModel__);
                it.__vModel__ = item.__vModel__ + "-" + it.__vModel__;
            });
            ctList.AddRange(item.__config__.children);
        });
        AllFieldsModel.AddRange(ctList);
    }

    /// <summary>
    /// 根据表单类型初始化.
    /// </summary>
    /// <param name="formJson">表单Json.</param>
    /// <param name="tables">涉及表Json.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    private void InitByFormType(string formJson, string tables, int formType)
    {
        if (formType.Equals(1))
        {
            AllFieldsModel = new List<FieldsModel>();
            var fields = formJson.ToObject<List<Dictionary<string, object>>>();
            fields.ForEach(it =>
            {
                if (it.ContainsKey("filedId"))
                    AllFieldsModel.Add(new FieldsModel() { __vModel__ = it["filedId"].ToString(), __config__ = new ConfigModel() { label = it["filedName"].ToString(), jnpfKey = JnpfKeyConst.COMINPUT } });
            });
            FieldsModelList = AllFieldsModel;
        }
        else
        {
            FormDataModel formModel = formJson.ToObjectOld<FormDataModel>();
            DataFormatReplace(formModel.fields);
            FormModel = formModel; // 表单Json模型
            IsHasTable = !string.IsNullOrEmpty(tables) && !"[]".Equals(tables) && tables.IsNullOrEmpty(); // 是否有表
            AllFieldsModel = TemplateAnalysis.AnalysisTemplateData(formModel.fields.ToJsonString().ToObjectOld<List<FieldsModel>>()); // 所有控件集合
            FieldsModelList = TemplateAnalysis.AnalysisTemplateData(formModel.fields); // 已剔除布局控件集合
            MainTable = tables.ToList<TableModel>().Find(m => m.typeId.Equals("1")); // 主表
            MainTableName = MainTable?.table; // 主表名称
            AddChlidTableFeildsModel();

            // 处理旧控件 部分没有 tableName
            FieldsModelList.Where(x => string.IsNullOrWhiteSpace(x.__config__.tableName)).ToList().ForEach(item =>
            {
                if (item.__vModel__.Contains("_jnpf_")) item.__config__.tableName = item.__vModel__.ReplaceRegex(@"_jnpf_(\w+)", string.Empty).Replace("jnpf_", string.Empty); // 副表
                else item.__config__.tableName = MainTableName != null ? MainTableName : string.Empty; // 主表
            });
            AllTable = tables.ToObject<List<TableModel>>(); // 所有表
            AuxiliaryTableFieldsModelList = FieldsModelList.Where(x => x.__vModel__.Contains("_jnpf_")).ToList(); // 单控件副表集合
            ChildTableFieldsModelList = FieldsModelList.Where(x => x.__config__.jnpfKey == JnpfKeyConst.TABLE).ToList(); // 子表集合
            MainTableFieldsModelList = FieldsModelList.Except(AuxiliaryTableFieldsModelList).Except(ChildTableFieldsModelList).ToList(); // 主表控件集合
            SingleFormData = FieldsModelList.Where(x => x.__config__.jnpfKey != JnpfKeyConst.TABLE).ToList(); // 非子表集合
            GenerateFields = GetGenerateFields(); // 系统生成控件

            MainTableFields = new Dictionary<string, string>();
            AuxiliaryTableFields = new Dictionary<string, string>();
            ChildTableFields = new Dictionary<string, string>();
            AllTableFields = new Dictionary<string, string>();
            MainTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
            {
                MainTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
                AllTableFields.Add(x.__vModel__, x.__config__.tableName + "." + x.__vModel__);
            });
            AuxiliaryTableFieldsModelList.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
            {
                AuxiliaryTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
                AllTableFields.Add(x.__vModel__, x.__vModel__.Replace("_jnpf_", ".").Replace("jnpf_", string.Empty));
            });
            ChildTableFieldsModelList.ForEach(item =>
            {
                item.__config__.children.Where(x => x.__vModel__.IsNotEmptyOrNull()).ToList().ForEach(x =>
                {
                    ChildTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
                    AllTableFields.Add(item.__vModel__ + "-" + x.__vModel__, item.__config__.tableName + "." + x.__vModel__);
                });
            });

            ColumnData = new ColumnDesignModel();
            AppColumnData = new ColumnDesignModel();
        }
    }

    /// <summary>
    /// 初始化列配置模型.
    /// </summary>
    private void InitColumnData(VisualDevEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.ColumnData)) ColumnData = entity.ColumnData.ToObject<ColumnDesignModel>(); // 列配置模型
        else ColumnData = new ColumnDesignModel();

        if (!string.IsNullOrWhiteSpace(entity.AppColumnData)) AppColumnData = entity.AppColumnData.ToObject<ColumnDesignModel>(); // 列配置模型
        else AppColumnData = new ColumnDesignModel();

        if (AppColumnData.columnList != null && AppColumnData.columnList.Any())
        {
            AppColumnData.columnList.ForEach(item =>
            {
                var addColumn = ColumnData.columnList.Find(x => x.prop == item.prop);
                if (addColumn == null) ColumnData.columnList.Add(item);
            });
        }

        if (AppColumnData.searchList != null && AppColumnData.searchList.Any())
        {
            AppColumnData.searchList.ForEach(item =>
            {
                var addSearch = ColumnData.searchList.Find(x => x.__config__.jnpfKey == item.__config__.jnpfKey);
                if (addSearch == null) ColumnData.searchList.Add(item);
            });
        }

        FullName = entity.FullName;

        if (ColumnData.uploaderTemplateJson != null && ColumnData.uploaderTemplateJson.selectKey != null)
        {
            dataType = ColumnData.uploaderTemplateJson.dataType;
            selectKey = new List<string>();

            // 列顺序
            AllFieldsModel.ForEach(item =>
            {
                if (ColumnData.uploaderTemplateJson.selectKey.Any(x => x.Equals(item.__vModel__))) selectKey.Add(item.__vModel__);
            });
        }

        // 数据过滤
        if (ColumnData.ruleList.IsNotEmptyOrNull() && ColumnData.ruleList.conditionList.Any())
        {
            DataRuleListJson = new List<object>();
            var conModels = new List<object>();
            var whereType = ColumnData.ruleList.matchLogic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
            conModels = AssembleItemRule(ColumnData.ruleList.conditionList.Copy(), whereType);
            if (conModels.Any()) DataRuleListJson.Add(new { ConditionalList = conModels });
        }

        if (AppColumnData.ruleListApp.IsNotEmptyOrNull() && AppColumnData.ruleListApp.conditionList.Any())
        {
            AppDataRuleListJson = new List<object>();
            var conModels = new List<object>();
            var whereType = AppColumnData.ruleListApp.matchLogic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
            conModels = AssembleItemRule(AppColumnData.ruleListApp.conditionList.Copy(), whereType);
            if (conModels.Any()) AppDataRuleListJson.Add(new { ConditionalList = conModels });
        }
    }

    /// <summary>
    /// 获取数据过滤条件.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private List<ConditionalModel> GetItemRule(DataFilteringConditionGroupsListModel model)
    {
        var conModels = new List<ConditionalModel>();
        var between = new List<string>();
        var cSharpTypeName = string.Empty;
        if (model.fieldValue.IsNotEmptyOrNull())
        {
            if (model.symbol.Equals("between")) between = model.fieldValue.ToObject<List<string>>();
            switch (model.jnpfKey)
            {
                case JnpfKeyConst.COMSELECT:
                case JnpfKeyConst.CURRORGANIZE:
                    if (model.fieldValue.IsNotEmptyOrNull() && model.symbol.Equals("==") || model.symbol.Equals("<>"))
                    {
                        model.fieldValue = model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                    }
                    else
                    {
                        if (model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[[")) model.fieldValue = model.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last() + "\"]").ToList();
                        else if (model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[")) model.fieldValue = model.fieldValue.ToObject<List<string>>().Select(x => x + "\"]").ToList();
                    }
                    break;
                case JnpfKeyConst.CREATETIME:
                case JnpfKeyConst.MODIFYTIME:
                case JnpfKeyConst.DATE:
                    {
                        if (model.symbol.Equals("between"))
                        {
                            var startTime = between.First().TimeStampToDateTime();
                            var endTime = between.Last().TimeStampToDateTime();
                            between[0] = startTime.ToString();
                            between[1] = endTime.ToString();
                        }
                        else
                        {
                            model.fieldValue = model.fieldValue.ToString().TimeStampToDateTime();
                        }
                        cSharpTypeName = "datetime";
                    }
                    break;
                case JnpfKeyConst.TIME:
                    {
                        if (!model.symbol.Equals("between"))
                        {
                            model.fieldValue = string.Format("{0:" + model.format + "}", Convert.ToDateTime(model.fieldValue));
                        }
                    }
                    break;
                case JnpfKeyConst.CASCADER:
                case JnpfKeyConst.ADDRESS:
                    if (model.fieldValue.IsNotEmptyOrNull() && (model.symbol.Equals("==") || model.symbol.Equals("<>")))
                        model.fieldValue = model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "");
                    break;
                case JnpfKeyConst.RATE:
                case JnpfKeyConst.SLIDER:
                case JnpfKeyConst.CALCULATE:
                    cSharpTypeName = "decimal";
                    break;

            }
        }

        var conditionalType = ConditionalType.Equal;
        switch (model.symbol)
        {
            case ">=":
                conditionalType = ConditionalType.GreaterThanOrEqual;
                break;
            case ">":
                conditionalType = ConditionalType.GreaterThan;
                break;
            case "==":
                conditionalType = ConditionalType.Equal;
                break;
            case "<=":
                conditionalType = ConditionalType.LessThanOrEqual;
                break;
            case "<":
                conditionalType = ConditionalType.LessThan;
                break;
            case "<>":
                conditionalType = ConditionalType.NoEqual;
                break;
            case "like":
                if (model.fieldValue != null && model.fieldValue.ToString().Contains("[")) model.fieldValue = model.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                conditionalType = ConditionalType.Like;
                break;
            case "notLike":
                if (model.fieldValue != null && model.fieldValue.ToString().Contains("[")) model.fieldValue = model.fieldValue.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
                conditionalType = ConditionalType.NoLike;
                break;
            case "in":
            case "notIn":
                if (model.fieldValue != null && model.fieldValue.ToString().Contains("["))
                {
                    var isListValue = false;
                    var itemField = AllFieldsModel.Find(x => x.__vModel__.Contains(model.__vModel__));
                    if (itemField != null && (itemField.multiple || model.jnpfKey.Equals(JnpfKeyConst.CHECKBOX) || model.jnpfKey.Equals(JnpfKeyConst.CASCADER) || model.jnpfKey.Equals(JnpfKeyConst.ADDRESS)))
                        isListValue = true;
                    if (model.jnpfKey.Equals(JnpfKeyConst.COMSELECT)) isListValue = false;
                    var ids = new List<string>();
                    if (model.fieldValue.ToString().Replace("\r\n", "").Replace(" ", "").Contains("[[")) ids = model.fieldValue.ToObject<List<List<string>>>().Select(x => x.Last()).ToList();
                    else ids = model.fieldValue.ToObject<List<string>>();

                    if (model.symbol.Equals("notIn"))
                    {
                        model.field = string.Format("{0}--And", model.field);
                        conModels.Add(new ConditionalModel
                        {
                            FieldName = model.field,
                            ConditionalType = ConditionalType.IsNot,
                            FieldValue = null
                        });
                        conModels.Add(new ConditionalModel
                        {
                            FieldName = model.field,
                            ConditionalType = ConditionalType.IsNot,
                            FieldValue = string.Empty
                        });
                    }

                    for (var i = 0; i < ids.Count; i++)
                    {
                        var it = ids[i];
                        conModels.Add(new ConditionalModel
                        {
                            FieldName = model.field,
                            ConditionalType = model.symbol.Equals("in") ? (model.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.Equal : ConditionalType.Like) : (model.jnpfKey.Equals(JnpfKeyConst.TREESELECT) ? ConditionalType.NoEqual : ConditionalType.NoLike),
                            FieldValue = isListValue ? it.ToJsonString() : it
                        });
                    }
                }
                return conModels;
            case "null":
                if (model.jnpfKey.Equals(JnpfKeyConst.CALCULATE) || model.jnpfKey.Equals(JnpfKeyConst.NUMINPUT) || model.jnpfKey.Equals(JnpfKeyConst.RATE) || model.jnpfKey.Equals(JnpfKeyConst.SLIDER))
                    conditionalType = ConditionalType.EqualNull;
                else
                    conditionalType = ConditionalType.IsNullOrEmpty;
                break;
            case "notNull":
                conditionalType = ConditionalType.IsNot;
                if (model.fieldValue.IsNullOrEmpty())
                    model.fieldValue = null;
                break;
            case "between":
                conModels.Add(new ConditionalModel
                {
                    FieldName = string.Format("{0}--And", model.field),
                    ConditionalType = ConditionalType.GreaterThanOrEqual,
                    FieldValue = between.First(),
                    FieldValueConvertFunc = it => Convert.ToDateTime(it),
                    CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
                });
                conModels.Add(new ConditionalModel
                {
                    FieldName = string.Format("{0}--And", model.field),
                    ConditionalType = ConditionalType.LessThanOrEqual,
                    FieldValue = between.Last(),
                    FieldValueConvertFunc = it => Convert.ToDateTime(it),
                    CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
                });
                return conModels;
        }

        conModels.Add(new ConditionalModel
        {
            FieldName = model.field,
            ConditionalType = conditionalType,
            FieldValue = model.fieldValue == null ? null : model.fieldValue.ToString(),
            CSharpTypeName = cSharpTypeName.IsNotEmptyOrNull() ? cSharpTypeName : null
        });

        return conModels;
    }

    /// <summary>
    /// 组装数据过滤条件.
    /// </summary>
    /// <param name="conditionList"></param>
    /// <param name="whereType"></param>
    /// <returns></returns>
    private List<object> AssembleItemRule(List<DataFilteringConditionListModel> conditionList, int whereType)
    {
        var conModels = new List<object>();
        conditionList.ForEach(item =>
        {
            var groupList = new List<object>();
            var groupWhere = item.logic.ToUpper().Equals("AND") ? (int)WhereType.And : (int)WhereType.Or;
            item.groups.ForEach(con =>
            {
                var conList = new List<object>();
                var ItemRule = GetItemRule(con);
                ItemRule.ForEach(model =>
                {
                    var where = (int)WhereType.Or;
                    if (model.FieldName.Contains("--And"))
                    {
                        model.FieldName = model.FieldName.Replace("--And", "");
                        where = (int)WhereType.And;
                    }
                    if (model.Equals(ItemRule.FirstOrDefault()))
                        where = (int)WhereType.Null;
                    model.CSharpTypeName = model.CSharpTypeName == null ? "" : model.CSharpTypeName;
                    conList.Add(new { Key = where, Value = new { FieldName = model.FieldName, FieldValue = model.FieldValue, ConditionalType = model.ConditionalType, CSharpTypeName = model.CSharpTypeName } });
                });

                if (conList.Any())
                    groupList.Add(new { Key = groupWhere, Value = new { ConditionalList = conList } });
            });
            if (groupList.Any()) conModels.Add(new { Key = whereType, Value = new { ConditionalList = groupList } });
        });
        return conModels;
    }

    /// <summary>
    /// 处理日期格式.
    /// </summary>
    private void DataFormatReplace(List<FieldsModel> fList)
    {
        foreach (FieldsModel item in fList)
        {
            if (item.__config__.jnpfKey.Equals(JnpfKeyConst.DATE)) item.format = item.format.Replace("YYYY-MM-DD", "yyyy-MM-dd").Replace("YYYY", "yyyy");
            else if (item.__config__.children != null && item.__config__.children.Any()) DataFormatReplace(item.__config__.children);
        }
    }

    /// <summary>
    /// 处理复杂表头 (代码生成专用).
    /// </summary>
    /// <returns></returns>
    private static List<ComplexHeaderModel> GetComplexHeaderList(List<FieldsModel> paramList, List<string> selectKey)
    {
        var complexHeaderList = new List<ComplexHeaderModel>();
        if (paramList != null && paramList.Any(x => x.__config__.label.Contains("@@")))
        {
            var pList = paramList.Copy();
            foreach (var it in paramList)
            {
                if (it.__config__.label.Contains("@@"))
                {
                    var hList = it.__config__.label.Split("@@");
                    if (!complexHeaderList.Any(x => x.id.Equals(hList.First())))
                    {
                        var addItem = new ComplexHeaderModel()
                        {
                            id = hList[0],
                            fullName = hList[1],
                            align = hList[2],
                            childColumns = new List<string>()
                        };
                        addItem.childColumns = pList.Where(x => x.__config__.label.Contains(addItem.id + "@@" + addItem.fullName) && selectKey.Contains(x.__vModel__)).Select(x => x.__vModel__).ToList();
                        if (addItem.childColumns.Any()) complexHeaderList.Add(addItem);
                    }

                    it.__config__.label = hList.Last();
                }
            }
        }

        return complexHeaderList;
    }
}