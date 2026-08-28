using JNPF.Common.Enums;
using JNPF.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Common.Core.Manager.Files
{
    public interface IFileManager
    {
        #region 导入导出(json文件)

        /// <summary>
        /// 导出.
        /// </summary>
        /// <param name="jsonStr">json数据.</param>
        /// <param name="name">文件名.</param>
        /// <param name="exportFileType">文件后缀.</param>
        /// <returns></returns>
        Task<dynamic> Export(string jsonStr, string name, ExportFileType exportFileType = ExportFileType.json);

        /// <summary>
        /// 导入.
        /// </summary>
        /// <param name="file">文件.</param>
        /// <returns></returns>
        string Import(IFormFile file);
        #endregion

        #region OSS

        /// <summary>
        /// 根据存储类型上传文件.
        /// </summary>
        /// <param name="stream">文件流.</param>
        /// <param name="directoryPath">保存文件夹.</param>
        /// <param name="fileName">新文件名.</param>
        /// <returns></returns>
        Task<bool> UploadFileByType(Stream stream, string directoryPath, string fileName);

        /// <summary>
        /// 根据存储类型下载文件.
        /// </summary>
        /// <param name="filePath">文件路径.</param>
        /// <param name="fileDownLoadName">文件下载名.</param>
        /// <returns></returns>
        Task<FileStreamResult> DownloadFileByType(string filePath, string fileDownLoadName);

        /// <summary>
        /// 获取指定文件夹下所有文件.
        /// </summary>
        /// <param name="filePath">文件前缀.</param>
        /// <returns></returns>
        Task<List<AnnexModel>> GetObjList(string filePath);

        /// <summary>
        /// 删除文件.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        Task DeleteFile(string filePath);

        /// <summary>
        /// 判断文件是否存在.
        /// </summary>
        /// <param name="filePath">文件地址.</param>
        /// <returns></returns>
        Task<bool> ExistsFile(string filePath);

        /// <summary>
        /// 获取指定文件流.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        Task<Stream> GetFileStream(string filePath);

        /// <summary>
        /// 剪切文件.
        /// </summary>
        /// <param name="filePath">源文件地址.</param>
        /// <param name="toFilePath">剪切地址.</param>
        /// <returns></returns>
        Task MoveFile(string filePath, string toFilePath);

        /// <summary>
        /// 复制文件.
        /// </summary>
        /// <param name="filePath">源文件地址.</param>
        /// <param name="toFilePath">剪切地址.</param>
        /// <returns></returns>
        Task CopyFile(string filePath, string toFilePath);

        /// <summary>
        /// 生成缩略图并上传.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="saveFileName"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        Task MakeThumbnail(Stream stream, string saveFileName, string folder);
        #endregion

        /// <summary>
        /// 根据类型获取文件存储路径.
        /// </summary>
        /// <param name="type">文件类型.</param>
        /// <returns></returns>
        public string GetPathByType(string type);

        /// <summary>
        /// 分片上传附件.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<dynamic> UploadChunk([FromForm] ChunkModel input);

        /// <summary>
        /// 分片组装.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<FileControlsModel> Merge([FromForm] ChunkModel input);

        /// <summary>
        /// 获取文件大小.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        string GetFileSize(long size);

        /// <summary>
        /// 获取地址和文件名.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="saveFileName"></param>
        void GetChunkModel(ChunkModel input, string saveFileName);
    }
}
