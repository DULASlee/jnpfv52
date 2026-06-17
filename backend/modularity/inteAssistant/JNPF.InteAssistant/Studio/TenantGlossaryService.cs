using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant.Studio
{
    /// <summary>
    /// 业务术语表
    /// </summary>
    [ApiDescriptionSettings(Tag = "Studio", Name = "TenantGlossary", Order = 111)]
    [Route("api/studio/tenant")]
    public class TenantGlossaryService : IDynamicApiController, ITransient
    {
        private readonly ISqlSugarClient _db;

        public TenantGlossaryService(ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// 获取术语列表
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        [HttpGet("glossary")]
        public List<TenantGlossaryDto> GetGlossary([FromQuery] string keyword = "")
        {
            // TODO: 从数据库读取术语列表
            return new List<TenantGlossaryDto>();
        }

        /// <summary>
        /// 创建术语
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("glossary")]
        public TenantGlossaryDto CreateGlossary([FromBody] TenantGlossaryDto input)
        {
            // TODO: 保存到数据库
            return input;
        }

        /// <summary>
        /// 更新术语
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPut("glossary/{id}")]
        public TenantGlossaryDto UpdateGlossary(string id, [FromBody] TenantGlossaryDto input)
        {
            // TODO: 更新数据库
            return input;
        }

        /// <summary>
        /// 删除术语
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("glossary/{id}")]
        public void DeleteGlossary(string id)
        {
            // TODO: 从数据库删除
        }
    }

    public class TenantGlossaryDto
    {
        public string Id { get; set; }
        public string Term { get; set; }
        public string Definition { get; set; }
        public string Synonyms { get; set; }
        public string Examples { get; set; }
    }
}
