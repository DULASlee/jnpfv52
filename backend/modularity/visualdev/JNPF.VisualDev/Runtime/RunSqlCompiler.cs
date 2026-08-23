using JNPF.DependencyInjection;

namespace JNPF.VisualDev.Runtime;

/// <summary>
/// M3 编译层 — SQL 编译引擎组件（规格 4.3，契约 C-M3-RunSqlCompiler@v1）.
/// 职责：将模型配置编译为 SQL/Json/条件模型（纯计算形态）.
/// 纪律：零 DI 依赖（构造白名单守护）；SQL 执行不归本组件（留调用方侧，经 IRuntimeDataStore 漏斗）.
/// 施工状态：S1 骨架（Task 3.1）；七方法迁入按 Task 3.2/3.3 推进.
/// </summary>
public class RunSqlCompiler : ISingleton
{
    // Task 3.2：七方法纯移动（GetListQuerySql/GetInfoQuerySql/GetQueryJson/GetSuperQueryJson/
    //           GetSuperQueryInput/GetIConditionalModelListByTableName/GetVisualDevModelDataConfig）
    // Task 3.3：DB 依赖参数化剥离（方法内 DB 读取改参数传入，调用侧供数）
}
