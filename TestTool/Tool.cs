using System;
using Tool;
using Tool.Interface;

namespace TestTool {
	internal sealed class Tool : ITool<ToolSettings> {
		public string Title => "Test";

		public void Execute(ToolSettings settings) {
			Logger.Initialize(false);
			string separator = new string('*', settings.AssemblyPath.Length);
			Logger.LogInfo(separator);
			Logger.LogInfo(settings.AssemblyPath);
			Logger.LogInfo(separator);
			Logger.LogInfo("Exception test");
			Logger.LogException(new ApplicationException("test"));
			Logger.Flush();
			Console.ReadKey(true);
			throw new ApplicationException("test");
		}
	}
}
