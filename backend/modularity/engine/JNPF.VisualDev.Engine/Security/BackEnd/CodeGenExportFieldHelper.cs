using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.Engine.Entity.Model;
using JNPF.VisualDev.Engine.Core;
using JNPF.VisualDev.Entitys;
using System.Text;

namespace JNPF.VisualDev.Engine.Security;

/// <summary>
/// 代码生成导出字段帮助类.
/// </summary>
public class CodeGenExportFieldHelper
{
    /// <summary>
    /// 获取主表字段名.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="comlexList">复杂表头.</param>
    /// <returns></returns>
    public static string ExportColumnField(List<IndexGridFieldModel>? list, List<ComplexHeaderModel> comlexList)
    {
        StringBuilder columnSb = new StringBuilder();
        if (list != null)
        {
            foreach (var item in list)
            {
                if (comlexList.Any(x => x.childColumns.Any(xx => xx.Equals(item.prop))))
                {
                    var columns = comlexList.FirstOrDefault(x => x.childColumns.Any(yy => yy.Equals(item.prop))).childColumns;
                    item.currentIndex = list.IndexOf(list.Find(x => x.id.Equals(columns.FirstOrDefault())));
                    if (columns.FirstOrDefault().Equals(item.id)) item.currentIndex--;
                }
                else
                {
                    item.currentIndex = list.IndexOf(list.Find(x => x.id.Equals(item.id)));
                }
            }

            list = list.OrderBy(x => x.currentIndex).ToList();

            foreach (var item in list)
            {
                if (comlexList.Any(x => x.childColumns.Any(xx => xx.Equals(item.prop))))
                {
                    var comlex = comlexList.FirstOrDefault(x => x.childColumns.Any(xx => xx.Equals(item.prop)));

                    // 复杂表头格式 label 调整
                    var comlexLabel = string.Format("{0}@@{1}@@{2}@@{3}", comlex.id, comlex.fullName, comlex.align, item.label);
                    columnSb.AppendFormat("{{\\\"value\\\":\\\"{0}\\\",\\\"field\\\":\\\"{1}\\\"}},", comlexLabel, item.prop);
                }
                else
                {
                    columnSb.AppendFormat("{{\\\"value\\\":\\\"{0}\\\",\\\"field\\\":\\\"{1}\\\"}},", item.label, item.prop);
                }
            }
        }

        return columnSb.ToString();
    }

    /// <summary>
    /// 获取导入字段.
    /// </summary>
    /// <param name="templateEntity"></param>
    /// <param name="configModel"></param>
    /// <returns></returns>
    public static string ImportColumnField(VisualDevEntity templateEntity, JNPF.Engine.Entity.Model.CodeGen.CodeGenConfigModel configModel)
    {
        var resDic = new Dictionary<string, string>();

        foreach (var item in configModel.TableField)
        {
            var columnName = templateEntity.EnableFlow.Equals(1) ? item.LowerColumnName : item.OriginalColumnName;
            if (item.IsAuxiliary) resDic.Add(string.Format("jnpf_{0}_jnpf_{1}", item.TableName, item.LowerColumnName), string.Format("jnpf_{0}_jnpf_{1}", item.TableName, columnName));
            else resDic.Add(item.LowerColumnName, columnName);
        }

        if (configModel.TableRelations != null && configModel.TableRelations.Any())
        {
            foreach (var table in configModel.TableRelations)
            {
                foreach (var item in table.ChilderColumnConfigList)
                {
                    var columnName = templateEntity.EnableFlow.Equals(1) ? item.LowerColumnName : item.OriginalColumnName;
                    resDic.Add(string.Format("{0}-{1}", table.ControlModel, item.LowerColumnName), string.Format("{0}-{1}", table.ControlModel, columnName));
                }
            }
        }

        var res = new List<string>();
        if (templateEntity.ColumnData.IsNotEmptyOrNull())
        {
            var columnDesignModel = templateEntity.ColumnData.ToObject<ColumnDesignModel>();
            if (columnDesignModel.type.Equals(3) || columnDesignModel.type.Equals(5)) columnDesignModel.complexHeaderList.Clear();

            if (columnDesignModel.uploaderTemplateJson != null && columnDesignModel.uploaderTemplateJson.selectKey != null)
            {
                foreach (var item in columnDesignModel.uploaderTemplateJson.selectKey)
                {
                    if (columnDesignModel.complexHeaderList.Any(x => x.childColumns.Any(xx => xx.Equals(item))))
                    {
                        var chItems = columnDesignModel.complexHeaderList.First(x => x.childColumns.Any(xx => xx.Equals(item))).childColumns;
                        chItems.ForEach(it =>
                        {
                            if (columnDesignModel.uploaderTemplateJson.selectKey.Contains(it) && !res.Contains(resDic[it])) res.Add(resDic[it]);
                        });
                    }
                    else
                    {
                        if (!res.Contains(resDic[item])) res.Add(resDic[item]);
                    }
                }
            }
        }

        return "{\"" + string.Join("\",\"", res) + "\"}";
    }
}
