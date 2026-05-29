using JNPF.Common.Configuration;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Options;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys.Dto.DocumentPreview;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace JNPF.Extend;

/// <summary>
/// 文件预览
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "DocumentPreview", Order = 600)]
[Route("api/extend/[controller]")]
public class DocumentPreview : IDynamicApiController, ITransient
{
    private readonly IFileManager _fileManager;

    public DocumentPreview(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    #region Get

    /// <summary>
    /// 获取文档列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList_Api([FromQuery] KeywordInput input)
    {
        var filePath = FileVariable.DocumentPreviewFilePath;
        var list = await _fileManager.GetObjList(filePath);
        list = list.FindAll(x => "xlsx".Equals(x.FileType) || "xls".Equals(x.FileType) || "docx".Equals(x.FileType) || "doc".Equals(x.FileType) || "pptx".Equals(x.FileType) || "ppt".Equals(x.FileType));
        if (input.keyword.IsNotEmptyOrNull())
            list = list.FindAll(x => x.FileName.Contains(input.keyword));
        return list.OrderByDescending(x => x.FileTime).Adapt<List<DocumentPreviewListOutput>>();
    }

    /// <summary>
    /// 文件在线预览.
    /// </summary>
    /// <param name="fileId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("{fileId}/Preview")]
    public async Task<dynamic> DocumentPreview_Api(string fileId, [FromQuery] DocumentPreviewPreviewInput input)
    {
        var filePath = FileVariable.DocumentPreviewFilePath;
        var files = await _fileManager.GetObjList(filePath);
        var file = files.Find(x => x.FileId == fileId);
        if (file.IsNotEmptyOrNull())
        {
            string domain = App.GetConfig<AppOptions>("JNPF_App", true).Domain;
            string yozoUrl = App.GetConfig<AppOptions>("JNPF_App", true).YOZO.Domain;
            string yozoKey = App.GetConfig<AppOptions>("JNPF_App", true).YOZO.domainKey;
            var url = string.Format("{0}/api/Extend/DocumentPreview/down/{1}", domain, file.FileName);
            if (!input.previewType.Equals("localPreview"))
            {
                url = string.Format("{0}?k={1}&url={2}", yozoUrl,
                yozoKey, url, input.noCache, input.watermark, input.isCopy, input.pageStart, input.pageEnd, input.type);
            }
            return url;
        }
        else
        {
            throw Oops.Oh(ErrorCode.D8000);
        }
    }

    /// <summary>
    /// 下载.
    /// </summary>
    /// <param name="fileName"></param>
    [HttpGet("down/{fileName}")]
    [IgnoreLog]
    [AllowAnonymous]
    public async Task FileDown(string fileName)
    {
        var filePath = Path.Combine(FileVariable.DocumentPreviewFilePath, fileName);
        var systemFilePath = Path.Combine(FileVariable.SystemFilePath, fileName);
        FileStreamResult fileStreamResult = null;
        if (await _fileManager.ExistsFile(filePath))
            fileStreamResult = await _fileManager.DownloadFileByType(filePath, fileName);
        else
            fileStreamResult = await _fileManager.DownloadFileByType(systemFilePath, fileName);

        byte[] bytes = new byte[fileStreamResult.FileStream.Length];

        fileStreamResult.FileStream.Read(bytes, 0, bytes.Length);

        fileStreamResult.FileStream.Close();
        var httpContext = App.HttpContext;
        httpContext.Response.ContentType = "application/octet-stream";
        httpContext.Response.Headers.Add("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(fileName, System.Text.Encoding.UTF8));
        httpContext.Response.Headers.Add("Content-Length", bytes.Length.ToString());
        httpContext.Response.Body.WriteAsync(bytes);
        httpContext.Response.Body.Flush();
        httpContext.Response.Body.Close();
    }
    #endregion
}