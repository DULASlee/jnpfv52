namespace JNPF.Schedule;

/// <summary>
/// 作业处理程序 <see cref="IJob"/> 创建工厂
/// </summary>
/// <remarks>主要用于控制如何实例化 <see cref="IJob"/></remarks>
public interface IJobFactory
{
    /// <summary>
    /// 创建作业处理程序实例
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="context"><see cref="JobFactoryContext"/> 上下文</param>
    /// <returns><see cref="IJob"/></returns>
    IJob CreateJob(IServiceProvider serviceProvider, JobFactoryContext context);
}