using JNPF.Common.Cache;
using System.Runtime.InteropServices;
using Yitter.IdGenerator;

namespace JNPF.Common.Security;

/// <summary>
/// 分布式雪花ID帮助类.
/// </summary>
public class SnowflakeIdHelper
{
    // 定义dll路径
    public const string RegWorkerId_DLL_NAME = "lib\\regworkerid_lib_v1.3.1\\yitidgengo";

    // 根据文档定义三个接口

    // 注册一个 WorkerId，会先注销所有本机已注册的记录
    // ip: redis 服务器地址
    // port: redis 端口
    // password: redis 访问密码，可为空字符串“”
    // maxWorkerId: 最大 WorkerId
    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "RegisterOne", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
    private static extern ushort RegisterOne(string ip, int port, string password, int maxWorkerId);

    // 注销本机已注册的 WorkerId
    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "UnRegister", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
    private static extern void UnRegister();

    // 检查本地WorkerId是否有效（0-有效，其它-无效）
    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "Validate", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
    private static extern int Validate(int workerId);

    /// <summary>
    /// 缓存配置.
    /// </summary>
    private static CacheOptions _cacheOptions = App.GetConfig<CacheOptions>("Cache", true);

    /// <summary>
    /// 生成ID.
    /// </summary>
    /// <returns></returns>
    public static string NextId()
    {
        // 这个if判断在高并发的情况下可能会有问题
        if (YitIdHelper.IdGenInstance == null)
        {
            ushort workerId;
            try
            {
                UnRegister();

                // 如果不用自动注册WorkerId的话，直接传一个数值就可以了
                workerId = RegisterOne(_cacheOptions.ip, _cacheOptions.port, _cacheOptions.password, 63);
            }
            catch (DllNotFoundException)
            {
                // 原生库(yitidgengo)在当前平台不可用时（如 macOS ARM64 无对应版本），
                // 回退到固定 WorkerId，单机部署足够使用。
                workerId = 1;
            }

            // 创建 IdGeneratorOptions 对象，可在构造函数中输入 WorkerId：
            var options = new IdGeneratorOptions(workerId);
            options.WorkerIdBitLength = 16; // 默认值6，限定 WorkerId 最大值为2^6-1，即默认最多支持64个节点。
            options.SeqBitLength = 6; // 默认值6，限制每毫秒生成的ID个数。若生成速度超过5万个/秒，建议加大 SeqBitLength 到 10。
            // options.BaseTime = Your_Base_Time; // 如果要兼容老系统的雪花算法，此处应设置为老系统的BaseTime。
            // ...... 其它参数参考 IdGeneratorOptions 定义。

            // 保存参数（务必调用，否则参数设置不生效）：
            YitIdHelper.SetIdGenerator(options);

            // 以上过程只需全局一次，且应在生成ID之前完成。
        }

        return YitIdHelper.NextId().ToString();
    }
}