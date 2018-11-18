using System;
using Tool.Interface;

namespace TestTool {
	internal sealed class Tool : ITool<ToolSettings> {
		public string Title => "Test";

		public void Execute(ToolSettings settings) {
			Console.WriteLine(settings.AssemblyPath);
			Console.ReadKey(true);
		}
	}
}
