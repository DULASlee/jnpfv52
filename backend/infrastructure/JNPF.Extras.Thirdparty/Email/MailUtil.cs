using JNPF.Common.Configuration;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System.Text;

namespace JNPF.Extras.Thirdparty.Email;

/// <summary>
/// 邮箱帮助类.
/// </summary>
public class MailUtil
{
    private static string mailFilePath = FileVariable.EmailFilePath;

    /// <summary>
    /// 发送：协议Smtp.
    /// </summary>
    /// <param name="mailConfig">配置</param>
    /// <param name="mailModel">信息</param>
    public static void Send(MailParameterInfo mailConfig, MailInfo mailModel)
    {
        MimeMessage message = new MimeMessage();

        // 发送方信息
        message.From.AddRange(new MailboxAddress[] { new MailboxAddress(mailConfig.AccountName, mailConfig.Account) });

        // 发件人
        if (!string.IsNullOrEmpty(mailModel.To))
        {
            List<MailboxAddress> toAddress = new List<MailboxAddress>();
            foreach (var item in mailModel.To.Split(','))
            {
                if (string.IsNullOrEmpty(mailModel.ToName)) mailModel.ToName = SnowflakeIdHelper.NextId();
                toAddress.Add(new MailboxAddress(mailModel.ToName, item));
            }

            message.To.AddRange(toAddress.ToArray());
        }

        // 抄送人
        if (!string.IsNullOrEmpty(mailModel.CC))
        {
            List<MailboxAddress> ccAddress = new List<MailboxAddress>();
            foreach (var item in mailModel.CC.Split(','))
            {
                if (string.IsNullOrEmpty(mailModel.ToName)) mailModel.CCName = SnowflakeIdHelper.NextId();
                ccAddress.Add(new MailboxAddress(mailModel.CCName, item));
            }

            message.Cc.AddRange(ccAddress.ToArray());
        }

        // 密送人
        if (!string.IsNullOrEmpty(mailModel.Bcc))
        {
            List<MailboxAddress> bccAddress = new List<MailboxAddress>();
            foreach (var item in mailModel.Bcc.Split(','))
            {
                if (string.IsNullOrEmpty(mailModel.ToName)) mailModel.BccName = SnowflakeIdHelper.NextId();
                bccAddress.Add(new MailboxAddress(mailModel.BccName, item));
            }

            message.Bcc.AddRange(bccAddress.ToArray());
        }

        message.Subject = mailModel.Subject;
        TextPart body = new TextPart(TextFormat.Html) { Text = mailModel.BodyText };
        MimeEntity entity = body;

        // 附件
        if (mailModel.Attachment != null)
        {
            var mult = new Multipart("mixed") { body };
            foreach (var attachment in mailModel.Attachment)
            {
                var file = new FileInfo(Path.Combine(mailFilePath, attachment.fileId));
                if (file.Exists)
                {
                    var mimePart = new MimePart();
                    mimePart.Content = new MimeContent(file.OpenRead());
                    mimePart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                    mimePart.ContentTransferEncoding = ContentEncoding.Base64;
                    mimePart.FileName = attachment.fileName;
                    mult.Add(mimePart);
                }
            }

            entity = mult;
        }

        message.Body = entity;
        message.Date = DateTime.Now;
        using (var client = new SmtpClient())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(mailConfig.SMTPHost, mailConfig.SMTPPort, mailConfig.Ssl);
            client.Authenticate(mailConfig.Account, mailConfig.Password);
            client.Send(message);
            client.Disconnect(true);
        }
    }

    /// <summary>
    /// 获取：协议Pop3.
    /// </summary>
    /// <param name="mailConfig">配置.</param>
    /// <param name="receiveCount">已收邮件数、注意：如果已收邮件数和邮件数量一致则不获取.</param>
    /// <returns></returns>
    public static List<MailInfo> Get(MailParameterInfo mailConfig, int receiveCount)
    {
        var resultList = new List<MailInfo>();
        using (var client = new Pop3Client())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(mailConfig.POP3Host, mailConfig.POP3Port, mailConfig.Ssl);
            client.Authenticate(mailConfig.Account, mailConfig.Password);
            if (receiveCount == client.Count)
                return resultList;
            for (int i = client.Count - 1; receiveCount <= i; i--)
            {
                var message = client.GetMessage(i);
                var from = (MailboxAddress)message.From[0];
                var attachment = message.Attachments;
                resultList.Add(new MailInfo()
                {
                    UID = message.MessageId,
                    To = from.Address,
                    ToName = from.Name,
                    Subject = message.Subject,
                    BodyText = message.HtmlBody,
                    Attachment = GetEmailAttachments(attachment, message.MessageId),
                    Date = message.Date.ToString().ParseToDateTime(),
                });
            }

            client.Disconnect(true);
        }

        return resultList;
    }

    /// <summary>
    /// 删除：协议Pop3.
    /// </summary>
    /// <param name="mailConfig">配置.</param>
    /// <param name="messageId">messageId.</param>
    public static void Delete(MailParameterInfo mailConfig, string messageId)
    {
        using (var client = new Pop3Client())
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            client.Connect(mailConfig.POP3Host, mailConfig.POP3Port, mailConfig.Ssl);
            client.Authenticate(mailConfig.Account, mailConfig.Password);
            for (int i = 0; i < client.Count; i++)
            {
                if (client.GetMessage(i).MessageId == messageId)
                {
                    client.DeleteMessage(i);
                }
            }
        }
    }

    /// <summary>
    /// 验证连接：协议Smtp、Pop3.
    /// </summary>
    /// <param name="mailConfig">配置.</param>
    /// <returns></returns>
    public static bool CheckConnected(MailParameterInfo mailConfig)
    {
        try
        {
            if (!string.IsNullOrEmpty(mailConfig.SMTPHost))
            {
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    //client.Connect(mailConfig.SMTPHost, mailConfig.SMTPPort, SecureSocketOptions.None);//不验证ssl
                    client.Connect(mailConfig.SMTPHost, mailConfig.SMTPPort, mailConfig.Ssl);
                    // 如果服务器不需要身份验证，您的代码只需不要调用client.Authenticate()
                    client.Authenticate(mailConfig.Account, mailConfig.Password);
                    client.Disconnect(true);
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(mailConfig.POP3Host))
            {
                using (var client = new Pop3Client())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect(mailConfig.POP3Host, mailConfig.POP3Port, mailConfig.Ssl);
                    client.Authenticate(mailConfig.Account, mailConfig.Password);
                    client.Disconnect(true);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.Write("邮箱验证失败原因:" + ex + ",失败详情:" + ex.StackTrace);
            return false;
        }
    }

    #region Method

    /// <summary>
    /// 获取邮件附件.
    /// </summary>
    /// <param name="attachments"></param>
    /// <param name="messageId"></param>
    /// <returns></returns>
    private static List<MailFileParameterInfo> GetEmailAttachments(IEnumerable<MimeEntity> attachments, string messageId)
    {
        var resultList = new List<MailFileParameterInfo>();
        foreach (var attachment in attachments)
        {
            if (attachment.IsAttachment)
            {
                var fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name;
                var fileId = SnowflakeIdHelper.NextId() + "_" + fileName;
                var filePath = Path.Combine(mailFilePath, fileId);
                using (var stream = File.Create(filePath))
                {
                    if (attachment is MessagePart rfc822)
                    {
                        rfc822.Message.WriteTo(stream);
                    }
                    else
                    {
                        var part = (MimePart)attachment;
                        part.Content.DecodeTo(stream);
                    }
                }

                var mailFileInfo = new FileInfo(filePath);
                resultList.Add(new MailFileParameterInfo { fileId = fileId, fileName = fileName, fileSize = FileHelper.ToFileSize(mailFileInfo.Length) });
            }
        }

        return resultList;
    }

    /// <summary>
    /// 转换为Base64.
    /// </summary>
    /// <param name="inputStr"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    private static string ConvertToBase64(string inputStr, Encoding encoding)
    {
        return Convert.ToBase64String(encoding.GetBytes(inputStr));
    }

    /// <summary>
    /// 转换报头为Base64.
    /// </summary>
    /// <param name="inputStr"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    private static string ConvertHeaderToBase64(string inputStr, Encoding encoding)
    {
        var encode = !string.IsNullOrEmpty(inputStr) && inputStr.Any(c => c > 127);
        if (encode)
        {
            return "=?" + encoding.WebName + "?B?" + ConvertToBase64(inputStr, encoding) + "?=";
        }

        return inputStr;
    }

    #endregion
}