using JNPF.Common.Filter;

namespace JNPF.WorkFlow.Entitys.Dto.FlowDelegete
{
    public class FlowDelegateQuery : PageInputBase
    {
        /// <summary>
        /// 1:委托设置,2:委托给我的.
        /// </summary>
        public string myOrDelagateToMe { get; set; }
    }
}
