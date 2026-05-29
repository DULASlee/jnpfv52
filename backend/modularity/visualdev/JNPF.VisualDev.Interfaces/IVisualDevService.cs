using JNPF.VisualDev.Entitys;
namespace JNPF.VisualDev.Interfaces;

/// <summary>
/// 可视化开发基础抽象类.
/// </summary>
public interface IVisualDevService
{
    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="isGetRelease">是否获取发布版本.</param>
    /// <returns></returns>
    Task<VisualDevEntity> GetInfoById(string id, bool isGetRelease = false);

    /// <summary>
    /// 新增导入数据.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="type">识别重复（0：跳过，1：追加）.</param>
    /// <returns></returns>
    Task CreateImportData(VisualDevEntity input, int type);

    /// <summary>
    /// 功能模板 无表 转 有表.
    /// </summary>
    /// <param name="vEntity">功能实体.</param>
    /// <param name="mainTableName">主表名称.</param>
    /// <returns></returns>
    Task<VisualDevEntity> NoTblToTable(VisualDevEntity vEntity, string mainTableName);

}
