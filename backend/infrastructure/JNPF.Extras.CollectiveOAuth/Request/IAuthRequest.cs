using JNPF.Extras.CollectiveOAuth.Models;

namespace JNPF.Extras.CollectiveOAuth.Request;

/**
 * JustAuth {@code Request}公共接口，所有平台的{@code Request}都需要实现该接口
 * <p>
 * {@link AuthRequest#authorize()}
 * {@link AuthRequest#authorize(string)}
 * {@link AuthRequest#login(AuthCallback)}
 * {@link AuthRequest#revoke(AuthToken)}
 * {@link AuthRequest#refresh(AuthToken)}
 *
 * @author yadong.zhang (yadong.zhang0415(a)gmail.com)
 * @since 1.8.
 */
public interface IAuthRequest
{
    /// <summary>
    /// 返回授权url，可自行跳转页面.
    /// <para>不建议使用该方式获取授权地址，不带{@code state}的授权地址，容易受到csrf攻击.</para>
    /// <para>建议使用{@link AuthDefaultRequest#authorize(string)}方法生成授权地址，在回调方法中对{@code state}进行校验.</para>
    /// </summary>
    /// <returns>返回授权地址.</returns>
    string authorize();

    /// <summary>
    /// 返回带.<code>state</code>参数的授权url，授权回调时会带上这个.<code>state</code>.
    /// </summary>
    /// <param name="state">state 验证授权流程的参数，可以防止csrf.</param>
    /// <returns>返回授权地址.</returns>
    string authorize(string state);

    /// <summary>
    /// 第三方登录.
    /// </summary>
    /// <param name="authCallback">用于接收回调参数的实体.</param>
    /// <returns>返回登录成功后的用户信息.</returns>
    AuthResponse login(AuthCallback authCallback);

    /// <summary>
    /// 撤销授权.
    /// </summary>
    /// <param name="authToken">登录成功后返回的Token信息.</param>
    /// <returns>AuthResponse.</returns>
    AuthResponse revoke(AuthToken authToken);

    /// <summary>
    /// 刷新access token （续期）.
    /// </summary>
    /// <param name="authToken">登录成功后返回的Token信息.</param>
    /// <returns>AuthResponse.</returns>
    AuthResponse refresh(AuthToken authToken);
}