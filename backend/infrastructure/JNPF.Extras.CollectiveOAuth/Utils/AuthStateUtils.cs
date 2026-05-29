namespace JNPF.Extras.CollectiveOAuth.Utils;

public class AuthStateUtils
{
    /// <summary>
    /// 生成随机state，采用https://github.com/lets-mica/mica的UUID工具.
    /// </summary>
    /// <returns>随机的state字符串.</returns>
    public static string createState()
    {
        return Guid.NewGuid().ToString();
    }
}