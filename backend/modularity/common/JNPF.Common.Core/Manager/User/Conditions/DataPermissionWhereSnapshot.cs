using System.Text;
using SqlSugar;

namespace JNPF.Common.Core.Manager.User.Conditions;

/// <summary>
/// Canonical WHERE snapshot for characterization tests (ToSql-equivalent invariants).
/// </summary>
public static class DataPermissionWhereSnapshot
{
    /// <summary>
    /// Flatten ConditionalModel leaves to stable text: FieldName|ConditionalType|FieldValue per line.
    /// </summary>
    public static string FromModels(IEnumerable<IConditionalModel> models)
    {
        var sb = new StringBuilder();
        foreach (var model in models)
            AppendModel(sb, model);
        return sb.ToString();
    }

    /// <summary>
    /// SqlSugar ToSql WHERE fragment (no DB round-trip). Falls back to FromModels if ToSql unavailable.
    /// </summary>
    public static string ToSqlWhere(List<IConditionalModel> models, string tableAlias = "t")
    {
        try
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = "Server=.;Database=jnpf_snapshot;Trusted_Connection=True;",
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
            });

            // Strongly-typed dummy entity so Where(List&lt;IConditionalModel&gt;) overload binds.
            var sql = db.Queryable<SnapshotRow>().AS(tableAlias).Where(models).ToSqlString();
            var idx = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? sql.Substring(idx).Trim() : sql.Trim();
        }
        catch
        {
            return "WHERE " + FromModels(models).Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }

    private static void AppendModel(StringBuilder sb, IConditionalModel model)
    {
        switch (model)
        {
            case ConditionalModel m:
                sb.Append(m.FieldName).Append('|')
                    .Append((int)m.ConditionalType).Append('|')
                    .Append(m.FieldValue ?? string.Empty)
                    .AppendLine();
                break;
            case ConditionalCollections c when c.ConditionalList != null:
                foreach (var pair in c.ConditionalList)
                {
                    if (pair.Value is ConditionalModel cm)
                    {
                        sb.Append(cm.FieldName).Append('|')
                            .Append((int)cm.ConditionalType).Append('|')
                            .Append(cm.FieldValue ?? string.Empty)
                            .AppendLine();
                    }
                    else if (pair.Value is IConditionalModel nested)
                    {
                        AppendModel(sb, nested);
                    }
                }

                break;
        }
    }

    private sealed class SnapshotRow
    {
        public string f_id { get; set; }
    }
}
