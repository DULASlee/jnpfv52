using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.AdvancedQuery
{
    /// <summary>
    /// 高级查询信息输出.
    /// </summary>
    [SuppressSniffer]
    public class AdvancedQuerySchemeInfoOutput : AdvancedQuerySchemeCrInput
    {
        /// <summary>
        /// 主键id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 创建时间.
        /// </summary>
        public string creatorTime { get; set; }

    }
}
