using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio
{
    /// <summary>
    /// 行业知识设置
    /// </summary>
    [ApiDescriptionSettings(Tag = "Studio", Name = "TenantIndustry", Order = 110)]
    [Route("api/studio/tenant")]
    public class TenantIndustryService : IDynamicApiController, ITransient
    {
        private readonly ISqlSugarClient _db;

        public TenantIndustryService(ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// 获取行业知识配置
        /// </summary>
        /// <returns></returns>
        [HttpGet("industry")]
        public TenantIndustryDto GetIndustry()
        {
            // TODO: 从数据库读取当前租户的行业配置
            return new TenantIndustryDto
            {
                Industry = "",
                Description = "",
                BusinessRules = "",
                ComplianceRequirements = ""
            };
        }

        /// <summary>
        /// 更新行业知识配置
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPut("industry")]
        public void UpdateIndustry([FromBody] TenantIndustryDto input)
        {
            // TODO: 保存到数据库
        }
    }

    public class TenantIndustryDto
    {
        public string Industry { get; set; }
        public string Description { get; set; }
        public string BusinessRules { get; set; }
        public string ComplianceRequirements { get; set; }
    }
}
