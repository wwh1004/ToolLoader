using System;
using Tool.Interface;

namespace Tool.Loggers {
	internal sealed class ConsoleLogger : BufferedLogger {
		public static ILogger Instance { get; } = new ConsoleLogger();

		private ConsoleLogger() : base(Callback) {
		}

		private static void Callback(string value, LogLevel level, ConsoleColor? color) {
			ConsoleColor oldColor = default;
			if (color.HasValue) {
				oldColor = Console.ForegroundColor;
				Console.ForegroundColor = color.Value;
			}
			Console.WriteLine(value);
			if (color.HasValue)
				Console.ForegroundColor = oldColor;
		}
	}
}
