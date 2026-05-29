using JNPF.Common.Models.NPOI;
using JNPF.DependencyInjection;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.Reflection;
using System.Text;
using Color = System.Drawing.Color;

namespace JNPF.Common.Security;

/// <summary>
/// Excel导出操作类
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2017.03.09.
/// </summary>
[SuppressSniffer]
public class ExcelExportHelper<T>
{
    /// <summary>
    /// Excel导出.
    /// </summary>
    /// <param name="MemoryStream">MemoryStream.</param>
    /// <param name="addFilePath">导出地址.</param>
    public static void Export(MemoryStream file, string addFilePath)
    {
        ExportWrite(addFilePath, file);
    }

    /// <summary>
    /// Excel导出.
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="excelConfig">导出设置包含文件名、标题、列设置.</param>
    /// <param name="addFilePath">导出地址.</param>
    public static void Export(List<T> list, ExcelConfig excelConfig, string addFilePath)
    {
        MemoryStream file = ExportMemoryStream(list, excelConfig);
        ExportWrite(addFilePath, file);
    }

    /// <summary>
    /// Excel导出.
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="excelConfig">导出设置包含文件名、标题、列设置.</param>
    /// <param name="addFilePath">导出地址.</param>
    /// <param name="columnList">列名称.</param>
    public static void Export(List<Dictionary<string, object>> list, ExcelConfig excelConfig, string addFilePath, List<string> columnList)
    {
        MemoryStream file = ExportMemoryStream(list, excelConfig, columnList);
        ExportWrite(addFilePath, file);
    }

    /// <summary>
    /// Excel导出.
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="excelConfig">导出设置包含文件名、标题、列设置.</param>
    public static MemoryStream ToStream(List<T> list, ExcelConfig excelConfig)
    {
        return ExportMemoryStream(list, excelConfig);
    }

    /// <summary>
    /// Excel导出.
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="path">模板路径.</param>
    /// <param name="addFilePath">保存文件名.</param>
    public static void Export(List<ExcelTemplateModel> list, string path, string addFilePath)
    {
        try
        {
            MemoryStream file = ExcelExportTemplate(list, path);
            ExportWrite(addFilePath, file);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 导出文件.
    /// </summary>
    /// <param name="filePath">文件保存路径.</param>
    /// <param name="memoryStream">文件流.</param>
    public static void ExportWrite(string filePath, MemoryStream memoryStream)
    {
        try
        {
            // 使用using可以最后不用关闭fs 比较方便
            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                // 转化为byte格式存储
                byte[] buffer = memoryStream.ToArray();
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
                buffer = null;
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    #region List<T>导出到Excel模板

    /// <summary>
    /// 数据导出到Excel模板.
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="filePath">模板路径.</param>
    public static MemoryStream ExcelExportTemplate(List<ExcelTemplateModel> list, string filePath)
    {
        try
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            ISheet? sheet = null;

            // 2003
            if (filePath.IndexOf(".xlsx") == -1)
            {
                HSSFWorkbook hssfworkbook = new HSSFWorkbook(fileStream);
                sheet = hssfworkbook.GetSheetAt(0);
                SetPurchaseOrder(sheet, list);
                sheet.ForceFormulaRecalculation = true;
                MemoryStream ms = new MemoryStream();
                hssfworkbook.Write(ms);
                ms.Flush();
                return ms;
            }

            // 2007
            else
            {
                XSSFWorkbook xssfworkbook = new XSSFWorkbook(fileStream);
                sheet = xssfworkbook.GetSheetAt(0);
                SetPurchaseOrder(sheet, list);
                sheet.ForceFormulaRecalculation = true;
                MemoryStream ms = new MemoryStream();
                xssfworkbook.Write(ms);
                ms.Flush();
                return ms;
            }

        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 赋值单元格.
    /// </summary>
    /// <param name="sheet"></param>
    /// <param name="list"></param>
    private static void SetPurchaseOrder(ISheet sheet, List<ExcelTemplateModel> list)
    {
        try
        {
            foreach (ExcelTemplateModel item in list)
            {
                IRow? row = null;
                ICell? cell = null;
                row = sheet.GetRow(item.row);
                if (row == null)
                    row = sheet.CreateRow(item.row);
                cell = row.GetCell(item.cell);
                if (cell == null)
                    cell = row.CreateCell(item.cell);
                cell.SetCellValue(item.value);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 合并单元格.
    /// </summary>
    /// <param name="sheet">要合并单元格所在的sheet.</param>
    /// <param name="rowstart">开始行的索引.</param>
    /// <param name="rowend">结束行的索引.</param>
    /// <param name="colstart">开始列的索引.</param>
    /// <param name="colend">结束列的索引.</param>
    public static void SetCellRangeAddress(ISheet sheet, int rowstart, int rowend, int colstart, int colend)
    {
        CellRangeAddress cellRangeAddress = new CellRangeAddress(rowstart, rowend, colstart, colend);
        sheet.AddMergedRegion(cellRangeAddress);
    }

    #endregion

    #region  List<T>导出到Excel的MemoryStream

    /// <summary>
    /// 导出到Excel的MemoryStream Export().
    /// </summary>
    /// <param name="list">数据源.</param>
    /// <param name="excelConfig">导出设置包含文件名、标题、列设置.</param>
    public static MemoryStream ExportMemoryStream(List<T> list, ExcelConfig excelConfig)
    {
        try
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            #region 右击文件 属性信息

            DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
            dsi.Company = "NPOI";
            workbook.DocumentSummaryInformation = dsi;
            SummaryInformation si = PropertySetFactory.CreateSummaryInformation();

            // 填加xls文件作者信息
            si.Author = "引迈信息技术有限公司";

            // 填加xls文件创建程序信息
            si.ApplicationName = "引迈信息技术有限公司";

            // 填加xls文件最后保存者信息
            si.LastAuthor = "引迈信息技术有限公司";

            // 填加xls文件作者信息
            si.Comments = "引迈信息技术有限公司";

            // 填加xls文件标题信息
            si.Title = "标题信息";

            // 填加文件主题信息
            si.Subject = "主题信息";
            si.CreateDateTime = System.DateTime.Now;
            workbook.SummaryInformation = si;

            #endregion

            #region 设置标题样式

            ICellStyle headStyle = workbook.CreateCellStyle();
            int[] arrColWidth = new int[properties.Length];

            // 列名
            string[] arrColName = new string[properties.Length];

            // 样式表
            // headStyle.BorderBottom = BorderStyle.Thin;
            // headStyle.BorderLeft = BorderStyle.Thin;
            // headStyle.BorderRight = BorderStyle.Thin;
            // headStyle.BorderTop = BorderStyle.Thin;
            ICellStyle[] arryColumStyle = new ICellStyle[properties.Length];
            headStyle.Alignment = HorizontalAlignment.Left;
            if (excelConfig.Background != new Color())
            {
                if (excelConfig.Background != new Color())
                {
                    headStyle.FillPattern = FillPattern.SolidForeground;
                    headStyle.FillForegroundColor = GetXLColour(workbook, excelConfig.Background);
                }
            }

            IFont font = workbook.CreateFont();
            font.FontHeightInPoints = excelConfig.TitlePoint;
            if (excelConfig.ForeColor != new Color())
                font.Color = GetXLColour(workbook, excelConfig.ForeColor);
            font.Boldweight = 700;
            headStyle.SetFont(font);

            #endregion

            #region 列头及样式

            ICellStyle cHeadStyle = workbook.CreateCellStyle();
            cHeadStyle.Alignment = HorizontalAlignment.Left;
            IFont cfont = workbook.CreateFont();
            cfont.FontHeightInPoints = excelConfig.HeadPoint;
            cHeadStyle.SetFont(cfont);

            #endregion

            #region 设置内容单元格样式

            int i = 0;
            foreach (PropertyInfo column in properties)
            {
                ICellStyle columnStyle = workbook.CreateCellStyle();
                columnStyle.Alignment = HorizontalAlignment.Left;
                arrColWidth[i] = Encoding.GetEncoding(0).GetBytes(column.Name).Length;
                arrColName[i] = column.Name;

                ExcelColumnModel columnModel = excelConfig.ColumnModel.Find(t => t.Column == column.Name);
                if (columnModel != null)
                {
                    arrColName[i] = columnModel.ExcelColumn;
                    if (columnModel.Width != 0)
                        arrColWidth[i] = columnModel.Width;

                    if (columnModel.Background != new Color())
                    {
                        if (columnModel.Background != new Color())
                        {
                            columnStyle.FillPattern = FillPattern.SolidForeground;
                            columnStyle.FillForegroundColor = GetXLColour(workbook, columnModel.Background);
                        }
                    }

                    if (columnModel.Font != null || columnModel.Point != 0 || columnModel.ForeColor != new Color())
                    {
                        IFont columnFont = workbook.CreateFont();
                        columnFont.FontHeightInPoints = 10;
                        if (columnModel.Font != null)
                            columnFont.FontName = columnModel.Font;
                        if (columnModel.Point != 0)
                            columnFont.FontHeightInPoints = columnModel.Point;
                        if (columnModel.ForeColor != new Color())
                            columnFont.Color = GetXLColour(workbook, columnModel.ForeColor);
                        columnStyle.SetFont(font);
                    }

                    arryColumStyle[i] = columnStyle;
                    i++;
                }
            }

            #endregion

            #region 填充数据

            ICellStyle dateStyle = workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd HH:mm:ss");
            int rowIndex = 0;

            #region 新建表，填充表头，填充列头，样式

            // if (rowIndex == 65535 || rowIndex == 0)
            // {
            // if (rowIndex != 0)
            // {
            //    sheet = workbook.CreateSheet();
            // }

            #region 表头及样式
            {
                if (excelConfig.Title != null)
                {
                    IRow headerRow = sheet.CreateRow(0);
                    if (excelConfig.TitleHeight != 0)
                    {
                        headerRow.Height = (short)(excelConfig.TitleHeight * 20);
                    }

                    headerRow.HeightInPoints = 25;
                    headerRow.CreateCell(0).SetCellValue(excelConfig.Title);
                    headerRow.GetCell(0).CellStyle = headStyle;
                    sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, excelConfig.ColumnModel.Count - 1)); // ------------------
                }
            }
            #endregion

            #region 列头及样式
            {
                int rownum = 0;
                if (excelConfig.Title != null)
                {
                    rowIndex = 2;
                    rownum = 1;
                }
                else
                {
                    rowIndex = 1;
                }

                IRow headerRow = sheet.CreateRow(rownum);

                #region 如果设置了列标题就按列标题定义列头，没定义直接按字段名输出

                int headIndex = 0;
                foreach (ExcelColumnModel excelColumnModel in excelConfig.ColumnModel)
                {
                    headerRow.CreateCell(headIndex).SetCellValue(excelColumnModel.ExcelColumn);
                    headerRow.GetCell(headIndex).CellStyle = cHeadStyle;

                    // 设置列宽
                    sheet.SetColumnWidth(headIndex, (arrColWidth[headIndex] + 1) * 256);
                    headIndex++;
                }
            }

            #endregion

            #endregion

            // }

            #endregion

            if (list != null)
            {
                foreach (T item in list)
                {
                    #region 填充内容

                    if ((rowIndex % 65536) == 0)
                    {
                        if (rowIndex != 0)
                        {
                            IRow headerRow = sheet.GetRow(0);
                            sheet = workbook.CreateSheet();
                            IRow dataRow_headerRow = sheet.CreateRow(0);
                            int k = 0;
                            foreach (ICell cell in headerRow.Cells)
                            {
                                ICell newCell = dataRow_headerRow.CreateCell(k);
                                newCell.CellStyle = arryColumStyle[k];
                                string drValue = cell.StringCellValue;
                                SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                                k++;
                            }

                            rowIndex = 1;
                        }
                    }

                    IRow dataRow = sheet.CreateRow(rowIndex);
                    int ordinal = 0;
                    foreach (ExcelColumnModel column in excelConfig.ColumnModel)
                    {
                        PropertyInfo? colunValue = properties.Where(it => it.Name == column.Column).FirstOrDefault();
                        if (colunValue != null)
                        {
                            ICell newCell = dataRow.CreateCell(ordinal);
                            newCell.CellStyle = arryColumStyle[ordinal];
                            string? drValue = colunValue.GetValue(item, null) == null ? string.Empty : colunValue.GetValue(item, null).ToString();
                            SetCell(newCell, dateStyle, colunValue.PropertyType, drValue);
                            ordinal++;
                        }
                    }

                    #endregion

                    rowIndex++;
                }
            }

            #endregion

            MemoryStream ms = new MemoryStream();
            workbook.Write(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #endregion

    #region  List<Dictionary<string, object>>导出到Excel的MemoryStream

    public static MemoryStream ExportMemoryStream(List<Dictionary<string, object>> list, ExcelConfig excelConfig, List<string> columnList)
    {
        try
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();

            #region 右击文件 属性信息
            {
                DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "NPOI";
                workbook.DocumentSummaryInformation = dsi;
                SummaryInformation si = PropertySetFactory.CreateSummaryInformation();

                // 填加xls文件作者信息
                si.Author = "引迈信息技术有限公司";

                // 填加xls文件创建程序信息
                si.ApplicationName = "引迈信息技术有限公司";

                // 填加xls文件最后保存者信息
                si.LastAuthor = "引迈信息技术有限公司";

                // 填加xls文件作者信息
                si.Comments = "引迈信息技术有限公司";

                // 填加xls文件标题信息
                si.Title = "标题信息";

                // 填加文件主题信息
                si.Subject = "主题信息";
                si.CreateDateTime = System.DateTime.Now;
                workbook.SummaryInformation = si;
            }
            #endregion

            #region 设置标题样式
            ICellStyle headStyle = workbook.CreateCellStyle();
            int[] arrColWidth = new int[columnList.Count];

            // 列名
            string[] arrColName = new string[columnList.Count];

            // 样式表
            // headStyle.BorderBottom = BorderStyle.Thin;
            // headStyle.BorderLeft = BorderStyle.Thin;
            // headStyle.BorderRight = BorderStyle.Thin;
            // headStyle.BorderTop = BorderStyle.Thin;
            ICellStyle[] arryColumStyle = new ICellStyle[columnList.Count];
            headStyle.Alignment = HorizontalAlignment.Left;
            if (excelConfig.Background != new Color())
            {
                if (excelConfig.Background != new Color())
                {
                    headStyle.FillPattern = FillPattern.SolidForeground;
                    headStyle.FillForegroundColor = GetXLColour(workbook, excelConfig.Background);
                }
            }

            IFont font = workbook.CreateFont();
            font.FontHeightInPoints = excelConfig.TitlePoint;
            if (excelConfig.ForeColor != new Color())
                font.Color = GetXLColour(workbook, excelConfig.ForeColor);

            font.Boldweight = 700;
            headStyle.SetFont(font);
            #endregion

            #region 列头及样式

            ICellStyle cHeadStyle = workbook.CreateCellStyle();
            cHeadStyle.Alignment = HorizontalAlignment.Left;
            IFont cfont = workbook.CreateFont();
            cfont.FontHeightInPoints = excelConfig.HeadPoint;
            cHeadStyle.SetFont(cfont);

            #endregion

            #region 设置内容单元格样式

            int i = 0;

            foreach (string column in columnList)
            {
                ICellStyle columnStyle = workbook.CreateCellStyle();
                columnStyle.Alignment = HorizontalAlignment.Left;
                arrColWidth[i] = Encoding.GetEncoding(0).GetBytes(column).Length;
                arrColName[i] = column;

                if (excelConfig.ColumnModel != null)
                {
                    ExcelColumnModel columnModel = excelConfig.ColumnModel.Find(t => t.ExcelColumn == column);
                    if (columnModel != null)
                    {
                        arrColName[i] = columnModel.ExcelColumn;
                        if (columnModel.Width != 0)
                            arrColWidth[i] = columnModel.Width;
                        if (columnModel.Background != new Color())
                        {
                            if (columnModel.Background != new Color())
                            {
                                columnStyle.FillPattern = FillPattern.SolidForeground;
                                columnStyle.FillForegroundColor = GetXLColour(workbook, columnModel.Background);
                            }
                        }

                        if (columnModel.Font != null || columnModel.Point != 0 || columnModel.ForeColor != new Color())
                        {
                            IFont columnFont = workbook.CreateFont();
                            columnFont.FontHeightInPoints = 10;
                            if (columnModel.Font != null)
                                columnFont.FontName = columnModel.Font;
                            if (columnModel.Point != 0)
                                columnFont.FontHeightInPoints = columnModel.Point;
                            if (columnModel.ForeColor != new Color())
                                columnFont.Color = GetXLColour(workbook, columnModel.ForeColor);

                            columnStyle.SetFont(font);
                        }



                        #region 下拉选项

                        if ((columnModel.Options!=null && columnModel.Options.Length > 0)|| columnModel.IsOptionFromList)
                        {
                            // 创建一个下拉选项列表
                            List<string> options = new List<string>();

                            if ((columnModel.Options != null && columnModel.Options.Length > 0)) 
                            {
                                options = columnModel.Options.Where(aa => aa != null).Distinct().ToList();

                                if (options != null)
                                {
                                    var strOpt = string.Join("", options);
                                    //长度大于250时excel不支持
                                    if (strOpt.Length > 250)
                                    {
                                        columnModel.IsOptionFromList = true;
                                    }
                                }

                            }
                            else if (columnModel.IsOptionFromList)
                            {
                                string keyName = columnModel.Column;
                                var tempList = list.SelectMany(aa => aa.Where(aa => aa.Key == keyName))
                                                    .Select(aa => aa.Value.ToString()).ToList();
                                options = tempList.ToList(); //获取目前数据中存在的选项

                                if (options.Count == 0)
                                {
                                    options = columnModel.Options.Take(5).ToList();
                                }
                            }

                            options = options.Distinct().ToList();

                            // 创建下拉选项数据验证
                            IDataValidationHelper validationHelper = sheet.GetDataValidationHelper();
                            IDataValidationConstraint validationConstraint = validationHelper.CreateExplicitListConstraint(options.ToArray());
                            CellRangeAddressList addressList = new CellRangeAddressList(1, 100000, i, i); // 从第2行开始到第1001行，第1列设置下拉选项（假设标题在第1行）
                            IDataValidation validation = validationHelper.CreateValidation(validationConstraint, addressList);

                            validation.ShowErrorBox = false; // 显示错误提示框,非强制下拉选项
                          

                            // 添加数据验证到工作表
                            sheet.AddValidationData(validation);
 

                        }

                        #endregion
                    }
                }

                arryColumStyle[i] = columnStyle;
                i++;
            }

            #endregion

            #region 填充数据

            ICellStyle dateStyle = workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd");
            int rowIndex = 0;
            #region 新建表，填充表头，填充列头，样式

            //if (rowIndex == 65535 || rowIndex == 0)
            //{
            //    if (rowIndex != 0)
            //    {
            //        sheet = workbook.CreateSheet();
            //    }

            #region 表头及样式
            {
                if (excelConfig.Title != null)
                {
                    IRow headerRow = sheet.CreateRow(0);
                    if (excelConfig.TitleHeight != 0)
                    {
                        headerRow.Height = (short)(excelConfig.TitleHeight * 20);
                    }
                    headerRow.HeightInPoints = 25;
                    headerRow.CreateCell(0).SetCellValue(excelConfig.Title);
                    headerRow.GetCell(0).CellStyle = headStyle;
                    sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, excelConfig.ColumnModel.Count - 1)); // ------------------
                }
            }
            #endregion

            #region 列头及样式
            {
                var _rownum = 0;
                if (excelConfig.Title != null)
                {
                    rowIndex = 2;
                    _rownum = 1;
                }
                else
                {
                    rowIndex = 1;
                }

                IRow headerRow = sheet.CreateRow(_rownum);

                #region 如果设置了列标题就按列标题定义列头，没定义直接按字段名输出

                int headIndex = 0;
                foreach (ExcelColumnModel excelColumnModel in excelConfig.ColumnModel)
                {
                    headerRow.CreateCell(headIndex).SetCellValue(excelColumnModel.ExcelColumn);
                    headerRow.GetCell(headIndex).CellStyle = cHeadStyle;
                    // 设置列宽
                    sheet.SetColumnWidth(headIndex, (arrColWidth[headIndex] + 1) * 256);
                    headIndex++;
                }

                #endregion
            }

            #endregion

            //}

            #endregion

            if (list != null)
            {
                foreach (Dictionary<string, object> item in list)
                {
                    #region 填充内容

                    if ((rowIndex % 65536) == 0)
                    {
                        if (rowIndex != 0)
                        {
                            IRow headerRow = sheet.GetRow(0);
                            sheet = workbook.CreateSheet();
                            IRow dataRow_headerRow = sheet.CreateRow(0);
                            int k = 0;
                            foreach (ICell cell in headerRow.Cells)
                            {
                                ICell newCell = dataRow_headerRow.CreateCell(k);
                                newCell.CellStyle = arryColumStyle[k];
                                string drValue = cell.StringCellValue;
                                SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                                k++;
                            }

                            rowIndex = 1;
                        }
                    }

                    IRow dataRow = sheet.CreateRow(rowIndex);
                    int ordinal = 0;
                    foreach (ExcelColumnModel column in excelConfig.ColumnModel)
                    {
                        var colunValue = item.Where(i => i.Key.ToLower().Equals(column.Column.ToLower()) || i.Key.Contains(string.Format("({0})", column.Column))).FirstOrDefault();
                        if (colunValue.Key == null) colunValue = item.Where(i => i.Key.Contains(column.Column)).FirstOrDefault();
                        if (item.Where(i => i.Key.Contains(column.Column)).Count() > 1 && colunValue.Key.Contains(")"))
                            colunValue = item.Where(i => i.Key.Contains(column.Column + ")")).FirstOrDefault();
                        if (colunValue.Key != null && colunValue.Value != null)
                        {
                            ICell newCell = dataRow.CreateCell(ordinal);
                            newCell.CellStyle = arryColumStyle[ordinal];
                            string drValue = colunValue.Value.ToString();
                            SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                            ordinal++;
                        }
                        else
                        {
                            ICell newCell = dataRow.CreateCell(ordinal);
                            newCell.CellStyle = arryColumStyle[ordinal];
                            string drValue = string.Empty;
                            SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                            ordinal++;
                        }
                    }

                    foreach (var column in item)
                    {
                        ExcelColumnModel columnModel = excelConfig.ColumnModel.Find(t => t.Column == column.Key);
                        if (columnModel != null)
                        {

                        }
                    }

                    #endregion
                    rowIndex++;
                }
            }
            #endregion

            MemoryStream ms = new MemoryStream();
            workbook.Write(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            throw;
        }

    }

    public static MemoryStream ExportMemoryStream(List<Dictionary<string, object>> list, ExcelConfig excelConfig, List<string> columnList, Dictionary<string, int> firstColumns)
    {
        try
        {
            HSSFWorkbook workbook = new HSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();

            #region 右击文件 属性信息
            {
                DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
                dsi.Company = "NPOI";
                workbook.DocumentSummaryInformation = dsi;
                SummaryInformation si = PropertySetFactory.CreateSummaryInformation();

                // 填加xls文件作者信息
                si.Author = "引迈信息技术有限公司";

                // 填加xls文件创建程序信息
                si.ApplicationName = "引迈信息技术有限公司";

                // 填加xls文件最后保存者信息
                si.LastAuthor = "引迈信息技术有限公司";

                // 填加xls文件作者信息
                si.Comments = "引迈信息技术有限公司";

                // 填加xls文件标题信息
                si.Title = "标题信息";

                // 填加文件主题信息
                si.Subject = "主题信息";
                si.CreateDateTime = System.DateTime.Now;
                workbook.SummaryInformation = si;
            }
            #endregion

            #region 设置标题样式
            ICellStyle headStyle = workbook.CreateCellStyle();
            int[] arrColWidth = new int[columnList.Count];

            // 列名
            string[] arrColName = new string[columnList.Count];

            // 样式表
            // headStyle.BorderBottom = BorderStyle.Thin;
            // headStyle.BorderLeft = BorderStyle.Thin;
            // headStyle.BorderRight = BorderStyle.Thin;
            // headStyle.BorderTop = BorderStyle.Thin;
            ICellStyle[] arryColumStyle = new ICellStyle[columnList.Count];
            headStyle.Alignment = HorizontalAlignment.Left;
            if (excelConfig.Background != new Color())
            {
                if (excelConfig.Background != new Color())
                {
                    headStyle.FillPattern = FillPattern.SolidForeground;
                    headStyle.FillForegroundColor = GetXLColour(workbook, excelConfig.Background);
                }
            }

            IFont font = workbook.CreateFont();
            font.FontHeightInPoints = excelConfig.TitlePoint;
            if (excelConfig.ForeColor != new Color())
                font.Color = GetXLColour(workbook, excelConfig.ForeColor);

            font.Boldweight = 700;
            headStyle.SetFont(font);
            #endregion

            #region 列头及样式

            ICellStyle cHeadStyle = workbook.CreateCellStyle();
            cHeadStyle.Alignment = HorizontalAlignment.CenterSelection;
            cHeadStyle.VerticalAlignment = VerticalAlignment.Center;
            IFont cfont = workbook.CreateFont();
            cfont.FontHeightInPoints = excelConfig.HeadPoint;
            cHeadStyle.SetFont(cfont);

            #endregion

            #region 设置内容单元格样式

            int i = 0;

            foreach (string column in columnList)
            {
                ICellStyle columnStyle = workbook.CreateCellStyle();
                columnStyle.Alignment = HorizontalAlignment.Left;
                arrColWidth[i] = Encoding.GetEncoding(0).GetBytes(column).Length;
                arrColName[i] = column;

                if (excelConfig.ColumnModel != null)
                {
                    ExcelColumnModel columnModel = excelConfig.ColumnModel.Find(t => t.ExcelColumn == column);
                    if (columnModel != null)
                    {
                        arrColName[i] = columnModel.ExcelColumn;
                        if (columnModel.Width != 0)
                            arrColWidth[i] = columnModel.Width;
                        if (columnModel.Background != new Color())
                        {
                            if (columnModel.Background != new Color())
                            {
                                columnStyle.FillPattern = FillPattern.SolidForeground;
                                columnStyle.FillForegroundColor = GetXLColour(workbook, columnModel.Background);
                            }
                        }

                        if (columnModel.Font != null || columnModel.Point != 0 || columnModel.ForeColor != new Color())
                        {
                            IFont columnFont = workbook.CreateFont();
                            columnFont.FontHeightInPoints = 10;
                            if (columnModel.Font != null)
                                columnFont.FontName = columnModel.Font;
                            if (columnModel.Point != 0)
                                columnFont.FontHeightInPoints = columnModel.Point;
                            if (columnModel.ForeColor != new Color())
                                columnFont.Color = GetXLColour(workbook, columnModel.ForeColor);

                            columnStyle.SetFont(font);
                        }
                    }
                }

                arryColumStyle[i] = columnStyle;
                i++;
            }

            #endregion

            #region 填充数据

            ICellStyle dateStyle = workbook.CreateCellStyle();
            dateStyle.Alignment = HorizontalAlignment.CenterSelection;
            dateStyle.VerticalAlignment = VerticalAlignment.Center;
            sheet.HorizontallyCenter = true;
            sheet.VerticallyCenter = true;
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd");
            int rowIndex = 0;
            #region 新建表，填充表头，填充列头，样式

            //if (rowIndex == 65535 || rowIndex == 0)
            //{
            //    if (rowIndex != 0)
            //    {
            //        sheet = workbook.CreateSheet();
            //    }

            #region 表头及样式
            {
                if (excelConfig.Title != null)
                {
                    IRow headerRow = sheet.CreateRow(0);
                    if (excelConfig.TitleHeight != 0)
                    {
                        headerRow.Height = (short)(excelConfig.TitleHeight * 20);
                    }
                    headerRow.HeightInPoints = 25;
                    headerRow.CreateCell(0).SetCellValue(excelConfig.Title);
                    headerRow.GetCell(0).CellStyle = headStyle;
                    sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, excelConfig.ColumnModel.Count - 1)); // ------------------
                }
            }
            #endregion

            #region 列头及样式

            // 需要合并的表头
            var headerDic = new Dictionary<int, int>();
            var startRow = 0; // 起始行
            foreach (var item in firstColumns)
            {
                // 复杂表头可能为空
                if (item.Key.Contains("jnpf-singlefield"))
                {
                    headerDic[startRow] = startRow + item.Value - 1;
                }
                startRow += item.Value;
            }

            {
                var _rownum = 0;
                if (excelConfig.Title != null)
                {
                    rowIndex = 2;
                    _rownum = 1;
                }
                else
                {
                    rowIndex = 1;
                }
                IRow firstRow = sheet.CreateRow(_rownum);
                var mergedIndex = 0;

                foreach (var item in firstColumns)
                {
                    firstRow.CreateCell(mergedIndex).SetCellValue(item.Key);
                    firstRow.GetCell(mergedIndex).CellStyle = cHeadStyle;
                    if (item.Value > 1 && !item.Key.Contains("jnpf-singlefield")) sheet.AddMergedRegion(new CellRangeAddress(0, 0, mergedIndex, mergedIndex + item.Value - 1));
                    mergedIndex += item.Value;
                }

                if (excelConfig.Title != null)
                {
                    rowIndex = 3;
                    _rownum = 2;
                }
                else
                {
                    rowIndex = 2;
                    _rownum = 1;
                }

                IRow headerRow = sheet.CreateRow(_rownum);

                #region 如果设置了列标题就按列标题定义列头，没定义直接按字段名输出

                int headIndex = 0;
                foreach (ExcelColumnModel excelColumnModel in excelConfig.ColumnModel)
                {
                    var oldRownum = _rownum;
                    foreach (var item in headerDic)
                    {
                        if (headIndex >= item.Key && headIndex <= item.Value)
                        {
                            headerRow.RowNum = oldRownum - 1;
                            break;
                        }
                        else
                        {
                            headerRow.RowNum = oldRownum;
                        }
                    }

                    headerRow.CreateCell(headIndex).SetCellValue(excelColumnModel.ExcelColumn);
                    headerRow.GetCell(headIndex).CellStyle = cHeadStyle;
                    // 设置列宽
                    sheet.SetColumnWidth(headIndex, (arrColWidth[headIndex] + 1) * 256);
                    headIndex++;
                }

                #endregion
            }

            #endregion

            //}

            #endregion

            if (list != null)
            {
                var id = string.Empty;
                var rowDic = new Dictionary<int, int>();
                var dataStartRow = 0;

                foreach (Dictionary<string, object> item in list)
                {
                    #region 填充内容

                    if ((rowIndex % 65536) == 0)
                    {
                        if (rowIndex != 0)
                        {
                            IRow headerRow = sheet.GetRow(0);
                            sheet = workbook.CreateSheet();
                            IRow dataRow_headerRow = sheet.CreateRow(0);
                            int k = 0;
                            foreach (ICell cell in headerRow.Cells)
                            {
                                ICell newCell = dataRow_headerRow.CreateCell(k);
                                newCell.CellStyle = arryColumStyle[k];
                                string drValue = cell.StringCellValue;
                                SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                                k++;
                            }

                            rowIndex = 1;
                        }
                    }

                    IRow dataRow = sheet.CreateRow(rowIndex);
                    int ordinal = 0;
                    foreach (ExcelColumnModel column in excelConfig.ColumnModel)
                    {
                        var colunValue = item.Where(i => i.Key.ToLower().Equals(column.Column.ToLower()) || i.Key.Contains(string.Format("({0})", column.Column))).FirstOrDefault();
                        if (colunValue.Key == null) colunValue = item.Where(i => i.Key.Contains(column.Column)).FirstOrDefault();
                        if (colunValue.Key != null && colunValue.Value != null)
                        {
                            ICell newCell = dataRow.CreateCell(ordinal);
                            newCell.CellStyle = arryColumStyle[ordinal];
                            string drValue = colunValue.Value.ToString();
                            SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                            ordinal++;
                        }
                        else
                        {
                            ICell newCell = dataRow.CreateCell(ordinal);
                            newCell.CellStyle = arryColumStyle[ordinal];
                            string drValue = string.Empty;
                            SetCell(newCell, dateStyle, drValue.GetType(), drValue);
                            ordinal++;
                        }
                    }

                    #endregion

                    // 合并数据行数
                    if (id.Equals(item["id"]))
                    {
                        rowDic[dataStartRow]++;
                    }
                    else
                    {
                        id = item["id"].ToString();
                        dataStartRow = rowIndex;
                        rowDic[dataStartRow] = rowIndex;
                    }

                    rowIndex++;
                }

                #region 合并单元格

                // 表头
                foreach (var item in headerDic)
                {
                    for (int n = 0; n < item.Value - item.Key + 1; n++)
                    {
                        sheet.AddMergedRegion(new CellRangeAddress(0, 1, item.Key + n, item.Key + n));
                    }
                }

                // 数据
                var cellStyle = workbook.CreateCellStyle();
                cellStyle.VerticalAlignment = VerticalAlignment.Center;
                foreach (var item in rowDic)
                {
                    if (!item.Key.Equals(item.Value))
                    {
                        for (int m = 0; m < excelConfig.ColumnModel.Count; m++)
                        {
                            if (!excelConfig.ColumnModel[m].Column.Contains("tableField"))
                            {
                                // 合并
                                sheet.AddMergedRegion(new CellRangeAddress(item.Key, item.Value, m, m));

                                // 数据居中
                                var cell = sheet.GetRow(item.Key).GetCell(m);
                                cell.CellStyle = cellStyle;
                            }
                        }
                    }
                }

                #endregion
            }
            #endregion

            MemoryStream ms = new MemoryStream();
            workbook.Write(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception)
        {
            throw;
        }

    }

    #endregion

    #region 设置表格内容
    private static void SetCell(ICell newCell, ICellStyle dateStyle, Type dataType, string drValue)
    {
        string callDataType = dataType.GenericTypeArguments.Count() == 0 ? dataType.ToString() : dataType.GenericTypeArguments.FirstOrDefault().ToString();

        switch (callDataType)
        {
            // 字符串类型
            case "System.String":
                newCell.SetCellValue(drValue);
                break;

            // 日期类型
            case "System.DateTime":
                {
                    System.DateTime dateV;
                    if (System.DateTime.TryParse(drValue, out dateV))
                        newCell.SetCellValue(dateV);
                    else
                        newCell.SetCellValue(string.Empty);
                    // 格式化显示
                    newCell.CellStyle = dateStyle;
                }

                break;

            // 布尔型
            case "System.Boolean":
                bool boolV = false;
                bool.TryParse(drValue, out boolV);
                newCell.SetCellValue(boolV);
                break;

            // 整型
            case "System.Int16":
            case "System.Int32":
            case "System.Int64":
            case "System.Byte":
                int intV = 0;
                int.TryParse(drValue, out intV);
                newCell.SetCellValue(intV);
                break;

            // 浮点型
            case "System.Decimal":
            case "System.Double":
                double doubV = 0;
                double.TryParse(drValue, out doubV);
                newCell.SetCellValue(doubV);
                break;

            // 空值处理
            case "System.DBNull":
                newCell.SetCellValue(string.Empty);
                break;
            default:
                newCell.SetCellValue(drValue);
                break;
        }
    }
    #endregion

    #region RGB颜色转NPOI颜色

    /// <summary>
    /// RGB颜色转NPOI颜色.
    /// </summary>
    /// <param name="workbook"></param>
    /// <param name="SystemColour"></param>
    /// <returns></returns>
    private static short GetXLColour(HSSFWorkbook workbook, Color SystemColour)
    {
        short s = 0;
        HSSFPalette XlPalette = workbook.GetCustomPalette();
        NPOI.HSSF.Util.HSSFColor XlColour = XlPalette.FindColor(SystemColour.R, SystemColour.G, SystemColour.B);
        if (XlColour == null)
        {
            if (NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE < 255)
            {
                XlColour = XlPalette.FindSimilarColor(SystemColour.R, SystemColour.G, SystemColour.B);
                s = XlColour.Indexed;
            }

        }
        else
            s = XlColour.Indexed;
        return s;
    }

    #endregion

    #region 类型转换

    object valueType(Type t, string value)
    {
        object o = null;
        string strt = "String";
        if (t.Name == "Nullable`1")
            strt = t.GetGenericArguments().FirstOrDefault().Name;
        switch (strt)
        {
            case "Decimal":
                o = decimal.Parse(value);
                break;
            case "Int":
                o = int.Parse(value);
                break;
            case "Float":
                o = float.Parse(value);
                break;
            case "DateTime":
                o = DateTime.Parse(value);
                break;
            default:
                o = value;
                break;
        }

        return o;
    }

    #endregion
}