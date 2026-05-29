using Aspose.Cells;
using JNPF.DependencyInjection;
using Spire.Presentation;

namespace JNPF.Common.Security;

/// <summary>
/// PDF帮助类.
/// </summary>
[SuppressSniffer]
public class PDFHelper
{
    /// <summary>
    /// Aspose组件Excel转成pdf文件.
    /// </summary>
    /// <param name="fileName">word文件路径.</param>
    public static void AsposeExcelToPDF(string fileName)
    {
        try
        {
            string pdfSavePath = fileName.Substring(0, fileName.LastIndexOf(".")) + ".pdf";
            if (!FileHelper.IsExistFile(pdfSavePath))
            {
                Workbook excel = new Workbook(fileName);
                if (excel != null)
                    excel.Save(pdfSavePath, SaveFormat.Pdf);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Aspose组件word转成pdf文件.
    /// </summary>
    /// <param name="fileName">word文件路径.</param>
    public static void AsposeWordToPDF(string fileName)
    {
        try
        {
            string pdfSavePath = fileName.Substring(0, fileName.LastIndexOf(".")) + ".pdf";
            if (!FileHelper.IsExistFile(pdfSavePath))
            {
                var document = new Aspose.Words.Document(fileName);
                if (document != null)
                    document.Save(pdfSavePath, Aspose.Words.SaveFormat.Pdf);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// PPT转换PDF.
    /// </summary>
    /// <param name="fileName">文件名称.</param>
    public static void PPTToPDF(string fileName)
    {
        try
        {
            if (!FileHelper.IsExistFile(fileName.Substring(0, fileName.LastIndexOf(".")) + ".pdf"))
            {
                Presentation presentation = new Presentation();
                presentation.LoadFromFile(fileName);
                presentation.SaveToFile(fileName.Substring(0, fileName.LastIndexOf(".")) + ".pdf", FileFormat.PDF);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
}