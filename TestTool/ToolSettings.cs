using System.IO;
using Tool.Interface;

namespace TestTool {
	internal sealed class ToolSettings {
		private string _assemblyPath;

		[CliArgument("-f", IsRequired = true)]
		private string CliAssemblyPath {
			set => _assemblyPath = Path.GetFullPath(value);
		}

		public string AssemblyPath => _assemblyPath;
	}
}
