using System;
using Tool.Interface;

namespace Tool.Loggers {
	/// <summary>
	/// Log action callback
	/// </summary>
	/// <param name="value"></param>
	/// <param name="level"></param>
	/// <param name="color"></param>
	public delegate void LogCallback(string value, LogLevel level, ConsoleColor? color);
}
