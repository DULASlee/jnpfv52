using JNPF.Common.Dtos.VisualDev;
using JNPF.Common.Filter;
using JNPF.Systems.Entitys.System;
using JNPF.VisualDev.Entitys;
using JNPF.VisualDev.Entitys.Dto.VisualDevModelData;
using JNPF.WorkFlow.Entitys.Entity;

namespace JNPF.VisualDev.Interfaces;

/// <summary>
/// 在线开发运行服务接口.
/// </summary>
public interface IRunService
{
    /// <summary>
    /// 创建在线开发功能.
    /// </summary>
    /// <param name="templateEntity">功能模板实体.</param>
    /// <param name="dataInput">数据输入.</param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<string> Create(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string? tenantId = null);

    /// <summary>
    /// 创建在线开发有表SQL.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="dataInput"></param>
    /// <param name="mainId"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<Dictionary<string, List<Dictionary<string, object>>>> CreateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataCrInput dataInput, string mainId, string? tenantId = null);

    /// <summary>
    /// 修改在线开发功能.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="templateEntity"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    Task Update(string id, VisualDevEntity templateEntity, VisualDevModelDataUpInput visualdevModelDataUpForm);

    /// <summary>
    /// 批量修改在线开发功能（集成助手用）.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="templateEntity"></param>
    /// <param name="visualdevModelDataUpForm"></param>
    /// <returns></returns>
    Task BatchUpdate(List<string>? ids, VisualDevEntity templateEntity, VisualDevModelDataUpInput visualdevModelDataUpForm);

    /// <summary>
    /// 修改在线开发有表sql.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="dataInput"></param>
    /// <param name="mainId"></param>
    /// <returns></returns>
    Task<List<string>> UpdateHaveTableSql(VisualDevEntity templateEntity, VisualDevModelDataUpInput dataInput, string mainId);

    /// <summary>
    /// 删除有表信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    Task DelHaveTableInfo(string id, VisualDevEntity templateEntity);

    /// <summary>
    /// 删除集成助手数据.
    /// </summary>
    /// <param name="templateEntity">模板实体.</param>
    /// <returns></returns>
    Task DelInteAssistant(VisualDevEntity templateEntity);

    /// <summary>
    /// 批量删除有表数据.
    /// </summary>
    /// <param name="ids">id数组.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="visualdevModelDataDeForm"></param>
    /// <returns></returns>
    Task BatchDelHaveTableData(List<string> ids, VisualDevEntity templateEntity, VisualDevModelDataBatchDelInput? visualdevModelDataDeForm = null);

    /// <summary>
    /// 列表数据处理.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="input"></param>
    /// <param name="actionType"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<PageResult<Dictionary<string, object>>> GetListResult(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List", string? tenantId = null);

    /// <summary>
    /// 关联表单列表数据处理.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="input"></param>
    /// <param name="actionType"></param>
    /// <returns></returns>
    Task<PageResult<Dictionary<string, object>>> GetRelationFormList(VisualDevEntity entity, VisualDevModelListQueryInput input, string actionType = "List");

    /// <summary>
    /// 获取有表详情.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="templateEntity">模板实体.</param>
    /// <param name="isInteAssis">是否为集成助手.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetHaveTableInfo(string id, VisualDevEntity templateEntity, bool isInteAssis = false);

    /// <summary>
    /// 获取有表详情转换.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="templateEntity"></param>
    /// <param name="isFlowTask"></param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<string> GetHaveTableInfoDetails(string id, VisualDevEntity templateEntity, bool isFlowTask = false, string? tenantId = null);

    /// <summary>
    /// 生成系统自动生成字段.
    /// </summary>
    /// <param name="fieldsModelListJson">模板数据.</param>
    /// <param name="allDataMap">真实数据.</param>
    /// <param name="IsCreate">创建与修改标识 true创建 false 修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GenerateFeilds(string fieldsModelListJson, Dictionary<string, object> allDataMap, bool IsCreate, List<string>? systemControlList = null);

    /// <summary>
    /// 获取数据库连接,根据linkId.
    /// </summary>
    /// <param name="linkId">数据库连接Id.</param>
    /// <param name="tenantId">租户Id.</param>
    /// <returns></returns>
    Task<DbLinkEntity> GetDbLink(string linkId, string? tenantId = null);

    /// <summary>
    /// 添加、修改 流程表单数据.
    /// </summary>
    /// <param name="fEntity">表单模板.</param>
    /// <param name="formData">表单数据json.</param>
    /// <param name="dataId">主键Id.</param>
    /// <param name="flowId">流程引擎主键Id.</param>
    /// <param name="isUpdate">是否修改.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    /// <returns></returns>
    Task SaveFlowFormData(FlowFormEntity fEntity, string formData, string dataId, string flowId, bool isUpdate = false, List<string>? systemControlList = null);

    /// <summary>
    /// 获取流程表单数据解析详情.
    /// </summary>
    /// <param name="fId">表单模板id.</param>
    /// <param name="dataId">主键Id.</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetFlowFormDataDetails(string fId, string dataId);

    /// <summary>
    /// 流程表单数据传递.
    /// </summary>
    /// <param name="oldFId">旧表单模板Id.</param>
    /// <param name="newFId">传递表单模板Id.</param>
    /// <param name="mapRule">映射规则字段 : Key 原字段, Value 映射字段.</param>
    /// <param name="formData">表单数据.</param>
    /// <param name="isSubFlow">是否子流程.</param>
    /// <param name="systemControlList">不赋值的系统控件Key.</param>
    Task<Dictionary<string, object>> SaveDataToDataByFId(string oldFId, string newFId, List<Dictionary<string, string>> mapRule, Dictionary<string, object> formData, bool isSubFlow = false, List<string>? systemControlList = null);

    /// <summary>
    /// 处理模板默认值 (针对流程表单).
    /// 用户选择 , 部门选择 , 岗位选择 , 角色选择 , 分组选择.
    /// </summary>
    /// <param name="propertyJson">表单json.</param>
    /// <param name="tableJson">关联表单.</param>
    /// <param name="formType">表单类型（1：系统表单 2：自定义表单）.</param>
    /// <returns></returns>
    string GetVisualDevModelDataConfig(string propertyJson, string tableJson, int formType);
}
