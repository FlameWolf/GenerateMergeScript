using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using TextCopy;

namespace GenerateMergeScript;

internal class Program
{
	static readonly string connectionString = @"";
	static readonly string tableName = "";
	static readonly string whereClause = "";
	static readonly DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

	static string ConvertDateToString(DateTime input)
	{
		if (DateOnly.FromDateTime(input) == today)
		{
			return "GETUTCDATE()";
		}
		return $"'{input:yyyy-MM-dd}'";
	}

	static async Task Main(string[] args)
	{
		using var sqlConn = new SqlConnection(connectionString);
		var sqlCmd = new SqlCommand
		(
			string.IsNullOrWhiteSpace(whereClause) ?
				$"SELECT * FROM {tableName}" :
				$"SELECT * FROM {tableName} WHERE {whereClause}",
			sqlConn
		);
		var strBuild = new StringBuilder();
		strBuild.AppendLine($"MERGE {tableName} AS T");
		strBuild.AppendLine("USING");
		strBuild.AppendLine("(");
		strBuild.AppendLine("\tSELECT * FROM");
		strBuild.AppendLine("\t(");
		strBuild.AppendLine("\t\tVALUES");
		try
		{
			await sqlConn.OpenAsync();
			using var sqlRead = sqlCmd.ExecuteReader();
			var columncount = sqlRead.FieldCount;
			var columnNames = new List<string>();
			for (int i = 0; i < columncount; i++)
			{
				columnNames.Add($"[{sqlRead.GetName(i)}]");
			}
			var keyColumnName = sqlRead
				.GetColumnSchema()
				.FirstOrDefault(col => col.IsKey == true)?
				.ColumnName ?? columnNames[0];
			var columnsExcludingKey = columnNames.Where(c => c != keyColumnName);
			while (sqlRead.Read())
			{
				var columnValues = new List<string>();
				for (int i = 0; i < columncount; i++)
				{
					var value = sqlRead.GetValue(i);
					columnValues.Add(value switch
					{
						DBNull => "NULL",
						bool boolValue => boolValue ? "1" : "0",
						DateTime dateValue => $"{ConvertDateToString(dateValue)}",
						string stringValue => $"'{stringValue.Replace("'", "''")}'",
						_ => value?.ToString() ?? string.Empty
					});
				}
				strBuild.AppendLine($"\t\t({string.Join(", ", columnValues)}),");
			}
			strBuild.Length -= 3;
			strBuild.AppendLine();
			strBuild.AppendLine("\t)");
			strBuild.Append("\tAS [SourceTable] (");
			strBuild.Append($"{string.Join(", ", columnNames)}");
			strBuild.AppendLine(")");
			strBuild.AppendLine(") AS S");
			strBuild.AppendLine($"ON T.{keyColumnName} = S.{keyColumnName}");
			strBuild.AppendLine("WHEN MATCHED THEN UPDATE SET");
			foreach (var columnName in columnsExcludingKey)
			{
				strBuild.AppendLine($"\tT.{columnName} = S.{columnName},");
			}
			strBuild.Length -= 3;
			strBuild.AppendLine("\nWHEN NOT MATCHED BY TARGET THEN");
			strBuild.AppendLine($"\tINSERT ({string.Join(", ", columnsExcludingKey)})");
			strBuild.AppendLine($"\tVALUES ({string.Join(", ", columnsExcludingKey.Select(c => $"S.{c}"))});");
			Console.WriteLine(strBuild.ToString());
			await ClipboardService.SetTextAsync(strBuild.ToString());
			Console.Write("Query copied to clipboard");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred: {ex.Message}");
		}
		finally
		{
			if (sqlConn.State == ConnectionState.Open)
			{
				await sqlConn.CloseAsync();
			}
		}
	}
}
