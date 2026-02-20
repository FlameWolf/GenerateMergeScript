using System.Text;

namespace GenerateMergeScript;

public static class StringBuilderExtensions
{
	public static void RemoveCommaBeforeLastNewLine(this StringBuilder strBuild)
	{
		var strLen = strBuild.Length;
		if (strLen < 2 || strBuild[strLen - 1] != '\n')
		{
			return;
		}
		if (strLen > 2 && strBuild[strLen - 1] == '\n' && strBuild[strLen - 2] == '\r' && strBuild[strLen - 3] == ',')
		{
			strBuild.Length = strLen - 3;
			strBuild.AppendLine();
		}
		else if (strBuild[strLen - 1] == '\n' && strBuild[strLen - 2] == ',')
		{
			strBuild.Length = strLen - 2;
			strBuild.AppendLine();
		}
	}
}