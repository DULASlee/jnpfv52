using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Request;

namespace JNPF.Extras.CollectiveOAuth.Config;

/// <summary>
/// OAuth平台的API地址的统一接口，提供以下方法：.
/// <para>1)<see cref="IAuthSource.authorize"/>: 获取授权url. 必须实现.</para>
/// <para>2)<see cref="IAuthSource.accessToken"/>: 获取accessToken的url. 必须实现.</para>
/// <para>3)<see cref="IAuthSource.userInfo"/>: 获取用户信息的url. 必须实现.</para>
/// <para>4)<see cref="IAuthSource.revoke"/>: 获取取消授权的url. 非必须实现接口（部分平台不支持）.</para>
/// <para>5)<see cref="IAuthSource.refresh"/>: 获取刷新授权的url. 非必须实现接口（部分平台不支持）.</para>
/// 注：.
/// <para> ①、如需通过JustAuth扩展实现第三方授权，请参考<see cref="DefaultAuthSourceEnum"/>自行创建对应的枚举类并实现<see cref="IAuthSource"/>接口.</para>
/// <para> ②、如果不是使用的枚举类，那么在授权成功后获取用户信息时，需要单独处理source字段的赋值.</para>
/// <para> ③、如果扩展了对应枚举类时，在<see cref="IAuthRequest.login(AuthCallback)"/>中可以通过<c>xx.toString()</c>获取对应的source.</para>
/// </summary>
public interface IAuthSource
{
    /// <summary>
    /// 授权的api.
    /// </summary>
    /// <returns>utl.</returns>
    string authorize();

    /// <summary>
    /// 获取accessToken的api.
    /// </summary>
    /// <returns>utl.</returns>
    string accessToken();

    /// <summary>
    /// 获取用户信息的api.
    /// </summary>
    /// <returns>utl.</returns>
    string userInfo();

    /// <summary>
    /// 取消授权的api.
    /// </summary>
    /// <returns>utl.</returns>
    string revoke();

    /// <summary>
    /// 刷新授权的api.
    /// </summary>
    /// <returns>utl.</returns>
    string refresh();

    /// <summary>
    /// 获取Source的字符串名字.
    /// </summary>
    /// <returns>utl.</returns>
    string getName();
}