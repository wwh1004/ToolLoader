using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using Tool.Interface;

namespace Tool {
	/// <summary>
	/// Logger with default implement
	/// </summary>
	public static class Logger {
		/// <summary>
		/// Gets default logger implement
		/// </summary>
		public static ILogger DefaultImpl => DefaultLogger.Instance;

		/// <summary>
		/// Gets or sets customized logger
		/// </summary>
		public static ILogger ExternImpl { get; set; }

		/// <summary>
		/// Indicates which thread can access logger and <see langword="null"/> represents every thread can access logger.
		/// </summary>
		public static Thread SingleThread { get => GetLogger().SingleThread; set => GetLogger().SingleThread = value; }

		/// <summary>
		/// Single thread mode lock
		/// </summary>
		public static IDisposable SingleThreadLock => GetLogger().SingleThreadLock;

		/// <summary>
		/// Indicates whether log queue is empty and background logger thread is idle
		/// </summary>
		public static bool IsIdle => GetLogger().IsIdle;

		/// <summary>
		/// Indicates current dequeued log count
		/// </summary>
		public static int QueueCount => GetLogger().QueueCount;

		/// <summary>
		/// Initialize current instance
		/// </summary>
		/// <param name="isDebugMode"></param>
		public static void Initialize(bool isDebugMode) { GetLogger().Initialize(isDebugMode); }

		/// <summary>
		/// Logs empty line
		/// </summary>
		public static void LogInfo() { GetLogger().LogInfo(); }

		/// <summary>
		/// Logs info and wraps
		/// </summary>
		/// <param name="value"></param>
		public static void LogInfo(string value) { GetLogger().LogInfo(value); }

		/// <summary>
		/// Logs warning and wraps
		/// </summary>
		/// <param name="value"></param>
		public static void LogWarning(string value) { GetLogger().LogWarning(value); }

		/// <summary>
		/// Logs error and wraps
		/// </summary>
		/// <param name="value"></param>
		public static void LogError(string value) { GetLogger().LogError(value); }

		/// <summary>
		/// Logs exception and wraps
		/// </summary>
		/// <param name="value"></param>
		public static void LogException(Exception value) { GetLogger().LogException(value); }

		/// <summary>
		/// Logs debug info and wraps
		/// </summary>
		/// <param name="value"></param>
		public static void LogDebugInfo(string value) { GetLogger().LogDebugInfo(value); }

		/// <summary>
		/// Logs text with specified color and wraps
		/// </summary>
		/// <param name="value"></param>
		/// <param name="color"></param>
		public static void LogLine(string value, ConsoleColor color) { GetLogger().LogLine(value, color); }

		/// <summary>
		/// Logs text with specified color
		/// </summary>
		/// <param name="value"></param>
		/// <param name="color"></param>
		public static void Log(string value, ConsoleColor color) { GetLogger().Log(value, color); }

		/// <summary>
		/// Immediately flushes buffer and waits to clear buffer
		/// </summary>
		public static void Flush() { GetLogger().Flush(); }

		private static ILogger GetLogger() { return ExternImpl ?? DefaultLogger.Instance; }

		private sealed class DefaultLogger : ILogger {
			private const int INTERVAL = 5;
			private const int MAX_INTERVAL = 200;
			private const int MAX_TEXT_COUNT = 5000;

			private static bool _isSyncMode;
			private static volatile Thread _singleThread;
			private static bool _isIdle = true;
			private static readonly Queue<ColorfulText> _queue = new Queue<ColorfulText>();
			private static readonly object _ioLock = new object();
			private static readonly object _stLock = new object();
			private static ConsoleColor _lastColor;
			private static bool _isInitialized;

			public static Thread SingleThread {
				get => _singleThread;
				set {
				relock:
					lock (_stLock) {
						var singleThread = _singleThread;
						if (!(singleThread is null) && Thread.CurrentThread != singleThread) {
							Monitor.Wait(_stLock);
							goto relock;
						}
						// 如果不符合设置设置SingleThread的条件，需要等待
						if (singleThread is null || Thread.CurrentThread == singleThread) {
							_singleThread = value;
							if (value is null)
								Monitor.PulseAll(_stLock);
							// 设置为null则取消阻塞其它线程
						}
					}
				}
			}

			public static IDisposable SingleThreadLock => new AutoSingleThreadLock();

			public static bool IsIdle => _isIdle;

			public static int QueueCount => _queue.Count;

			public static void Initialize(bool isDebugMode) {
				if (_isInitialized)
					return;

				bool isSyncMode = isDebugMode || Debugger.IsAttached;
				_isSyncMode = isSyncMode;
				if (!isSyncMode) {
					new Thread(IOLoop) {
						Name = $"{nameof(DefaultLogger)}.{nameof(IOLoop)}",
						IsBackground = true
					}.Start();
				}
				_isInitialized = true;
			}

			public static void LogInfo() {
				LogLine(string.Empty, ConsoleColor.Gray);
			}

			public static void LogInfo(string value) {
				LogLine(value, ConsoleColor.Gray);
			}

			public static void LogWarning(string value) {
				LogLine(value, ConsoleColor.Yellow);
			}

			public static void LogError(string value) {
				LogLine(value, ConsoleColor.Red);
			}

			public static void LogException(Exception value) {
				if (value is null)
					throw new ArgumentNullException(nameof(value));

				LogError(ExceptionToString(value));
			}

			public static void LogDebugInfo(string value) {
				LogLine(value, ConsoleColor.DarkGray);
			}

			public static void LogLine(string value, ConsoleColor color) {
				Log(value + Environment.NewLine, color);
			}

			public static void Log(string value, ConsoleColor color) {
				if (!_isInitialized)
					throw new InvalidOperationException();

				if (_isSyncMode) {
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = color;
					Console.Write(value);
					Console.ForegroundColor = oldColor;
					return;
				}
			relock:
				lock (_stLock) {
					var singleThread = _singleThread;
					if (!(singleThread is null) && Thread.CurrentThread != singleThread) {
						Monitor.Wait(_stLock);
						goto relock;
					}
					lock (((ICollection)_queue).SyncRoot) {
						if (string.IsNullOrEmpty(value))
							color = _lastColor;
						// 优化空行显示
						_queue.Enqueue(new ColorfulText(value, color));
						_lastColor = color;
					}
					lock (_ioLock)
						Monitor.Pulse(_ioLock);
				}
			}

			public static void Flush() {
				if (_isSyncMode)
					return;
				while (!_isIdle || _queue.Count != 0)
					Thread.Sleep(INTERVAL / 3);
			}

			private static string ExceptionToString(Exception exception) {
				if (exception is null)
					throw new ArgumentNullException(nameof(exception));

				var sb = new StringBuilder();
				DumpException(exception, sb);
				return sb.ToString();
			}

			private static void DumpException(Exception exception, StringBuilder sb) {
				sb.AppendLine($"Type: {Environment.NewLine}{exception.GetType().FullName}");
				sb.AppendLine($"Message: {Environment.NewLine}{exception.Message}");
				sb.AppendLine($"Source: {Environment.NewLine}{exception.Source}");
				sb.AppendLine($"StackTrace: {Environment.NewLine}{exception.StackTrace}");
				sb.AppendLine($"TargetSite: {Environment.NewLine}{exception.TargetSite}");
				sb.AppendLine("----------------------------------------");
				if (!(exception.InnerException is null))
					DumpException(exception.InnerException, sb);
				if (exception is ReflectionTypeLoadException reflectionTypeLoadException) {
					foreach (var loaderException in reflectionTypeLoadException.LoaderExceptions)
						DumpException(loaderException, sb);
				}
			}

			private static void IOLoop() {
				if (_isSyncMode)
					throw new InvalidOperationException();

				var sb = new StringBuilder();
				while (true) {
					_isIdle = true;
					if (_queue.Count == 0) {
						lock (_ioLock)
							Monitor.Wait(_ioLock);
					}
					_isIdle = false;
					// 等待输出被触发

					int delayCount = 0;
					int oldCount;
					do {
						oldCount = _queue.Count;
						Thread.Sleep(INTERVAL);
						delayCount++;
					} while (_queue.Count > oldCount && delayCount < MAX_INTERVAL / INTERVAL && _queue.Count < MAX_TEXT_COUNT);
					// 也许此时有其它要输出的内容

					var currents = default(Queue<ColorfulText>);
					lock (((ICollection)_queue).SyncRoot) {
						currents = new Queue<ColorfulText>(_queue);
						_queue.Clear();
					}
					// 获取全部要输出的内容

					do {
						var current = currents.Dequeue();
						sb.Length = 0;
						sb.Append(current.Text);
						while (true) {
							if (currents.Count == 0)
								break;
							var next = currents.Peek();
							if (next.Color != current.Color)
								break;
							currents.Dequeue();
							sb.Append(next.Text);
						}
						// 合并颜色相同，减少重绘带来的性能损失
						var oldColor = Console.ForegroundColor;
						Console.ForegroundColor = current.Color;
						Console.Write(sb.ToString());
						Console.ForegroundColor = oldColor;
					} while (currents.Count > 0);
				}
			}

			#region ILogger interface
			public static readonly DefaultLogger Instance = new DefaultLogger();

			private DefaultLogger() { }

			Thread ILogger.SingleThread { get => SingleThread; set => SingleThread = value; }

			IDisposable ILogger.SingleThreadLock => SingleThreadLock;

			bool ILogger.IsIdle => IsIdle;

			int ILogger.QueueCount => QueueCount;

			void ILogger.Initialize(bool isDebugMode) { Initialize(isDebugMode); }

			void ILogger.LogInfo() { LogInfo(); }

			void ILogger.LogInfo(string value) { LogInfo(value); }

			void ILogger.LogWarning(string value) { LogWarning(value); }

			void ILogger.LogError(string value) { LogError(value); }

			void ILogger.LogException(Exception value) { LogException(value); }

			void ILogger.LogDebugInfo(string value) { LogDebugInfo(value); }

			void ILogger.LogLine(string value, ConsoleColor color) { LogLine(value, color); }

			void ILogger.Log(string value, ConsoleColor color) { Log(value, color); }

			void ILogger.Flush() { Flush(); }
			#endregion

			private struct ColorfulText {
				public string Text;
				public ConsoleColor Color;

				public ColorfulText(string text, ConsoleColor color) {
					Text = text;
					Color = color;
				}
			}

			private sealed class AutoSingleThreadLock : IDisposable {
				public AutoSingleThreadLock() {
					SingleThread = Thread.CurrentThread;
					Flush();
				}

				void IDisposable.Dispose() {
					if (SingleThread is null)
						throw new InvalidOperationException();
					SingleThread = null;
				}
			}
		}
	}
}
