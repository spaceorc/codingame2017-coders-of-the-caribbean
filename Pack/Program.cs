using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Pack
{
	internal class Program
	{
		[STAThread]
		private static int Main(string[] args)
		{
			try
			{
				var gameDir = new DirectoryInfo(args[0]);
				var fileInfos = gameDir.GetFiles("*.cs", SearchOption.AllDirectories).Where(f => !IsExcluded(f)).ToList();
				var result = new StringBuilder();
				result.AppendLine("// Author: spaceorc. Source: https://github.com/spaceorc/codingame2017-coders-of-the-caribbean");

				var usings = new HashSet<string>();
				var contents = new List<Tuple<FileInfo, string, int>>();
				foreach (var fileInfo in fileInfos)
				{
					Console.Out.WriteLine($"Preprocessing {fileInfo.Name}");
					using (var fileStream = fileInfo.OpenRead())
					using (var fileReader = new StreamReader(fileStream))
					{
						var fileContent = fileReader.ReadToEnd();
						var preprocessed = Preprocess(fileInfo.Name, fileContent);
						contents.Add(Tuple.Create(fileInfo, preprocessed.Item2, preprocessed.Item3));
						usings.UnionWith(preprocessed.Item1);
					}
				}

				result.AppendLine();
				foreach (var u in usings.Where(x => !string.IsNullOrEmpty(x)))
				{
					result.AppendLine(u);
				}

				result.AppendLine();
				result.AppendLine("namespace Game");
				result.AppendLine("{");
				
				foreach (var tuple in contents.OrderBy(t => t.Item3).ThenBy(t => t.Item1.DirectoryName.ToLowerInvariant()))
				{
					Console.Out.WriteLine($"Writing {tuple.Item1.Name}");
					result.AppendLine(tuple.Item2);
				}

				result.AppendLine("}");
				Clipboard.SetText(result.ToString());
				Console.Out.WriteLine("Result was copied to clipboard");

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return -1;
			}
		}

		private static Tuple<string[], string, int> Preprocess(string fileName, string fileContent)
		{
			var orderRegex = new Regex(@"//\s*pack\s*:\s*(?<order>\d+)", RegexOptions.Singleline | RegexOptions.Compiled);
			var orderMatch = orderRegex.Match(fileContent);
			int order = int.MaxValue;
			if (orderMatch.Success)
				order = int.Parse(orderMatch.Groups["order"].Value);
			var regex = new Regex(@"^(?<using>.*?)(?<header>namespace.*?\n{)", RegexOptions.Singleline | RegexOptions.Compiled);
			var match = regex.Match(fileContent);
			if (!match.Success)
				throw new InvalidOperationException($"Couldn't preprocess file {fileName}");

			var usingsString = match.Groups["using"].Value.Trim();
			var usings = usingsString.Split('\n').Select(x => x.Trim()).Where(x => !x.StartsWith("using Game")).ToArray();
			var content = fileContent.Substring(match.Groups["header"].Index + match.Groups["header"].Length).TrimEnd().TrimEnd('}').TrimEnd();

			return Tuple.Create(usings, content, order);
		}

		private static bool IsExcluded(FileInfo fileInfo)
		{
			if (fileInfo.Name.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
				return true;
			for (var d = fileInfo.Directory; d != null; d = d.Parent)
				if (d.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)
				    || d.Name.Equals("bin", StringComparison.OrdinalIgnoreCase)
				    || d.Name.Equals(".vs", StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}
	}
}