using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.AdvancedQuery
{
    /// <summary>
    /// 高级查询创建输入.
    /// </summary>
    [SuppressSniffer]
    public class AdvancedQuerySchemeCrInput
    {
        /// <summary>
        /// 方案名称.
        /// </summary>
        public string fullName { get; set; }

        /// <summary>
        /// 匹配逻辑.
        /// </summary>
        public string matchLogic { get; set; }

        /// <summary>
        /// 条件规则Json.
        /// </summary>
        public string conditionJson { get; set; }

        /// <summary>
        /// 菜单主键.
        /// </summary>
        public string moduleId { get; set; }

        /// <summary>
        /// 所属用户.
        /// </summary>
        public string creatorUserId { get; set; }
    }
}
