using System;
using System.Threading;

namespace Tool.Interface {
	/// <summary>
	/// Tool logger interface
	/// </summary>
	public interface ILogger {
		/// <summary>
		/// Indicates which thread can access logger and <see langword="null"/> represents every thread can access logger.
		/// </summary>
		Thread SingleThread { get; set; }

		/// <summary>
		/// Single thread mode lock
		/// </summary>
		IDisposable SingleThreadLock { get; }

		/// <summary>
		/// Indicates whether log queue is empty and background logger thread is idle
		/// </summary>
		bool IsIdle { get; }

		/// <summary>
		/// Indicates current dequeued log count
		/// </summary>
		int QueueCount { get; }

		/// <summary>
		/// Initialize current instance
		/// </summary>
		/// <param name="isDebugMode"></param>
		void Initialize(bool isDebugMode);

		/// <summary>
		/// Logs empty line
		/// </summary>
		void LogInfo();

		/// <summary>
		/// Logs info and wraps
		/// </summary>
		/// <param name="value"></param>
		void LogInfo(string value);

		/// <summary>
		/// Logs warning and wraps
		/// </summary>
		/// <param name="value"></param>
		void LogWarning(string value);

		/// <summary>
		/// Logs error and wraps
		/// </summary>
		/// <param name="value"></param>
		void LogError(string value);

		/// <summary>
		/// Logs exception and wraps
		/// </summary>
		/// <param name="value"></param>
		void LogException(Exception value);

		/// <summary>
		/// Logs debug info and wraps
		/// </summary>
		/// <param name="value"></param>
		void LogDebugInfo(string value);

		/// <summary>
		/// Logs text with specified color and wraps
		/// </summary>
		/// <param name="value"></param>
		/// <param name="color"></param>
		void LogLine(string value, ConsoleColor color);

		/// <summary>
		/// Logs text with specified color
		/// </summary>
		/// <param name="value"></param>
		/// <param name="color"></param>
		void Log(string value, ConsoleColor color);

		/// <summary>
		/// Immediately flushes buffer and waits to clear buffer
		/// </summary>
		void Flush();
	}
}
