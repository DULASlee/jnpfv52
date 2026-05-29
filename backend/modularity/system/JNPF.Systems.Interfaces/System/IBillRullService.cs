namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 单据规则
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IBillRullService
{
    /// <summary>
    /// 获取流水号.
    /// </summary>
    /// <param name="enCode">流水编码.</param>
    /// <param name="isCache">是否缓存：每个用户会自动占用一个流水号，这个刷新页面也不会跳号.</param>
    /// <returns></returns>
    Task<string> GetBillNumber(string enCode, bool isCache = false);
}