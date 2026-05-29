using JNPF.Common.Filter;

namespace JNPF.Systems.Entitys.Dto.System.BillRule
{
    public class BillRuleListQueryInput : PageInputBase
    {
        /// <summary>
        /// 分类id.
        /// </summary>
        public string categoryId { get; set; }

        /// <summary>
        /// 启用标识.
        /// </summary>
        public int? enabledMark { get; set; }
    }
}
