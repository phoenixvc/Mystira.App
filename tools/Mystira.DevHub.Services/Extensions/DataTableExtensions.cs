using System.Data;

namespace Mystira.DevHub.Services.Extensions;

public static class DataTableExtensions
{
    public static string ToCsv(this DataTable table, string delimiter = ",")
    {
        if (table == null || table.Columns.Count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        // Add headers
        for (int i = 0; i < table.Columns.Count; i++)
        {
            sb.Append(table.Columns[i].ColumnName);
            if (i < table.Columns.Count - 1)
            {
                sb.Append(delimiter);
            }
        }
        sb.AppendLine();

        // Add rows
        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                // Handle null values
                if (row[i] != null && row[i] != DBNull.Value)
                {
                    string value = row[i].ToString() ?? string.Empty;
                    // Escape quotes and wrap in quotes if contains delimiter or newlines
                    if (value.Contains(delimiter) || value.Contains("\n") || value.Contains("\""))
                    {
                        value = "\"" + value.Replace("\"", "\"\"") + "\"";
                    }
                    sb.Append(value);
                }

                if (i < table.Columns.Count - 1)
                {
                    sb.Append(delimiter);
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
