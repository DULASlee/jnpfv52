

using Senparc.Weixin.WxOpen.AdvancedAPIs;
using Senparc.Weixin;
using Senparc.Weixin.WxOpen.Containers;
using Senparc.Weixin.Entities.TemplateMessage;

namespace JNPF.Extras.Thirdparty.WeChat
{
    public class WeChatApp
    {
        private string _appId;
        private string _appSecret;

        public WeChatApp(string appId, string appSecret)
        {
            _appId = appId;
            _appSecret = appSecret;

            // 注册小程序的AppId和AppSecret
            AccessTokenContainer.Register(_appId, _appSecret);
        }

        /// <summary>
        /// 访问令牌.
        /// </summary>
        public string accessToken { get; private set; }
        public string GetAccessToken()
        {
            // 从容器中获取access_token
            return AccessTokenContainer.GetAccessToken(_appId);
        }

        // 发送订阅消息
        public async Task<string> SendSubscribeMessage(string openId, string templateId, Dictionary<string, string> dataItems, string pagePath = "pages/index/index")
        {
            try
            {
                // 创建模板消息数据集合
                var templateMessageData = new TemplateMessageData();
                foreach (var item in dataItems)
                {
                    templateMessageData.Add(item.Key,new TemplateMessageDataValue(item.Value));
                }

                // 发送订阅消息
                var result = await MessageApi.SendSubscribeAsync(_appId, openId, templateId, templateMessageData, page: pagePath, timeOut: 20000);

                // 检查发送结果
                if (result.errcode == ReturnCode.请求成功)
                {
                    return "订阅消息发送成功";
                }
                else
                {
                    return $"订阅消息发送失败：{result.errmsg}";
                }
            }
            catch (Exception ex)
            {
                // 处理异常情况
                return $"发送订阅消息时发生异常：{ex.Message}";
            }
        }
    }
}
