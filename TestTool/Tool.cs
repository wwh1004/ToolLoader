using System;
using Tool.Interface;

namespace TestTool {
	internal sealed class Tool : ITool<ToolSettings> {
		public string Title => "Test";

		public void Execute(ToolSettings settings) {
			string separator;

			separator = new string('*', settings.AssemblyPath.Length);
			Console.WriteLine(separator);
			Console.WriteLine(settings.AssemblyPath);
			Console.WriteLine(separator);
			Console.WriteLine("Exception test");
			Console.ReadKey(true);
			throw new ApplicationException("test");
		}
	}
}
