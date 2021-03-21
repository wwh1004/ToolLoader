using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using Tool.Interface;

namespace Tool.Loggers {
	internal sealed class ConsoleLogger : ILogger {
		private readonly Context _context;
		private bool _isFreed;

		public static readonly ConsoleLogger Instance = new ConsoleLogger();

		public bool IsLocked => _context.IsLocked;

		private ConsoleLogger() : this(new Context(new LoggerCore())) {
			_context.Creator = this;
			_context.Owner = this;
		}

		private ConsoleLogger(Context context) {
			_context = context;
		}

		public void Log(string value, LogLevel level, ConsoleColor? color = null) {
			CheckFreed();
			if (_context.IsLocked) {
			relock:
				if (_context.Owner != this) {
					lock (_context.LockObj) {
						if (_context.Owner != this) {
							Monitor.Wait(_context.LockObj);
							goto relock;
						}
					}
				}
			}

			_context.Core.LogCore(value, level, color);
		}

		public ILogger EnterLock() {
			CheckFreed();
			if (this != _context.Creator)
				throw new InvalidOperationException("Nested lock is not supported");

			relock:
			lock (_context.LockObj) {
				if (_context.IsLocked) {
					Monitor.Wait(_context.LockObj);
					goto relock;
				}

				_context.Owner = new ConsoleLogger(_context);
				_context.IsLocked = true;
				return _context.Owner;
			}
		}

		public ILogger ExitLock() {
			CheckFreed();
			if (_context.Creator == this)
				throw new InvalidOperationException("No lock can be exited");

			_isFreed = true;
			_context.Owner = _context.Creator;
			_context.IsLocked = false;
			lock (_context.LockObj)
				Monitor.PulseAll(_context.LockObj);
			return _context.Owner;
		}

		private void CheckFreed() {
			if (_isFreed)
				throw new InvalidOperationException("Current logger is freed");
		}

		private sealed class Context {
			public readonly object LockObj = new object();
			public readonly LoggerCore Core;
			public ConsoleLogger Creator;
			public volatile ConsoleLogger Owner;
			public volatile bool IsLocked;

			public Context(LoggerCore core) {
				Core = core;
			}
		}

		#region forwards
		public LogLevel Level {
			get {
				CheckFreed();
				return _context.Core.Level;
			}
			set {
				CheckFreed();
				_context.Core.Level = value;
			}
		}

		public bool IsAsync {
			get {
				CheckFreed();
				return _context.Core.IsAsync;
			}
			set {
				CheckFreed();
				_context.Core.IsAsync = value;
			}
		}

		public bool IsIdle {
			get {
				CheckFreed();
				return _context.Core.IsIdle;
			}
		}

		public int QueueCount {
			get {
				CheckFreed();
				return _context.Core.QueueCount;
			}
		}

		public void Info() {
			_context.Core.Info(this);
		}

		public void Info(string value) {
			_context.Core.Info(value, this);
		}

		public void Warning(string value) {
			_context.Core.Warning(value, this);
		}

		public void Error(string value) {
			_context.Core.Error(value, this);
		}

		public void Verbose1(string value) {
			_context.Core.Verbose1(value, this);
		}

		public void Verbose2(string value) {
			_context.Core.Verbose2(value, this);
		}

		public void Verbose3(string value) {
			_context.Core.Verbose3(value, this);
		}

		public void Exception(Exception value) {
			_context.Core.Exception(value, this);
		}

		public void Flush() {
			CheckFreed();
			_context.Core.Flush();
		}
		#endregion

		#region core
		private sealed class LoggerCore {
			private const int INTERVAL = 5;
			private const int MAX_INTERVAL = 200;
			private const int MAX_CACHE_COUNT = 5000;

			private LogLevel _level;
			private bool _isAsync;
			private bool _isIdle;
			private readonly Thread _asyncWorker;
			private readonly Queue<LogItem> _asyncQueue;
			private readonly object _asyncTrigger;

			public LogLevel Level {
				get => _level;
				set => _level = value;
			}

			public bool IsAsync {
				get => _isAsync;
				set => _isAsync = value;
			}

			public bool IsIdle => _isIdle;

			public int QueueCount => _asyncQueue.Count;

			public LoggerCore() {
				_level = LogLevel.Info;
				_isAsync = Debugger.IsAttached;
				_isIdle = true;
				_asyncWorker = new Thread(AsyncLoop) {
					Name = $"{nameof(ConsoleLogger)}.{nameof(AsyncLoop)}",
					IsBackground = true
				};
				_asyncWorker.Start();
				_asyncQueue = new Queue<LogItem>();
				_asyncTrigger = new object();
			}

			public void Info(ILogger logger) {
				Log(string.Empty, LogLevel.Info, null, logger);
			}

			public void Info(string value, ILogger logger) {
				Log(value, LogLevel.Info, ConsoleColor.Gray, logger);
			}

			public void Warning(string value, ILogger logger) {
				Log(value, LogLevel.Warning, ConsoleColor.Yellow, logger);
			}

			public void Error(string value, ILogger logger) {
				Log(value, LogLevel.Error, ConsoleColor.Red, logger);
			}

			public void Verbose1(string value, ILogger logger) {
				Log(value, LogLevel.Verbose1, ConsoleColor.DarkGray, logger);
			}

			public void Verbose2(string value, ILogger logger) {
				Log(value, LogLevel.Verbose2, ConsoleColor.DarkGray, logger);
			}

			public void Verbose3(string value, ILogger logger) {
				Log(value, LogLevel.Verbose3, ConsoleColor.DarkGray, logger);
			}

			public void Exception(Exception value, ILogger logger) {
				if (value is null)
					throw new ArgumentNullException(nameof(value));

				Error(ExceptionToString(value), logger);
			}

			public void Log(string value, LogLevel level, ConsoleColor? color, ILogger logger) {
				logger.Log(value, level, color);
			}

			public void LogCore(string value, LogLevel level, ConsoleColor? color) {
				if (_isAsync) {
					_asyncQueue.Enqueue(new LogItem(value, level, color));
					lock (_asyncTrigger)
						Monitor.Pulse(_asyncTrigger);
				}
				else {
					WriteConsole(value, level, color);
				}
			}

			public void Flush() {
				while (!_isIdle)
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

			private void AsyncLoop() {
				var sb = new StringBuilder();
				while (true) {
					lock (((ICollection)_asyncQueue).SyncRoot) {
						if (_asyncQueue.Count == 0) {
							_isIdle = true;
							lock (_asyncTrigger)
								Monitor.Wait(_asyncTrigger);
						}
						_isIdle = false;
					}
					// 等待输出被触发

					int delayCount = 0;
					int oldCount;
					do {
						oldCount = _asyncQueue.Count;
						Thread.Sleep(INTERVAL);
						delayCount++;
					} while (_asyncQueue.Count > oldCount && delayCount < MAX_INTERVAL / INTERVAL && _asyncQueue.Count < MAX_CACHE_COUNT);
					// 也许此时有其它要输出的内容

					var currents = default(Queue<LogItem>);
					lock (((ICollection)_asyncQueue).SyncRoot) {
						currents = new Queue<LogItem>(_asyncQueue);
						_asyncQueue.Clear();
					}
					// 获取全部要输出的内容

					do {
						var current = currents.Dequeue();
						var color = current.Color;
						sb.Length = 0;
						sb.Append(current.Value);
						while (true) {
							if (currents.Count == 0)
								break;

							var next = currents.Peek();
							if (next.Level != current.Level)
								break;

							if (!color.HasValue && next.Color.HasValue)
								color = next.Color;
							// 空行的颜色是null，获取第一个非null的颜色作为合并日志的颜色
							if (next.Color.HasValue && next.Color != color)
								break;
							// 如果下一行的颜色不是null并且与当前颜色不同，跳出优化

							sb.AppendLine();
							sb.Append(currents.Dequeue().Value);
						}
						// 合并日志等级与颜色相同的，减少重绘带来的性能损失
						WriteConsole(sb.ToString(), current.Level, color);
					} while (currents.Count > 0);
				}
			}

			private void WriteConsole(string value, LogLevel level, ConsoleColor? color) {
				if (level > Level)
					return;

				ConsoleColor oldColor = default;
				if (color.HasValue) {
					oldColor = Console.ForegroundColor;
					Console.ForegroundColor = color.Value;
				}
				Console.WriteLine(value);
				if (color.HasValue)
					Console.ForegroundColor = oldColor;
			}

			private struct LogItem {
				public string Value;
				public LogLevel Level;
				public ConsoleColor? Color;

				public LogItem(string value, LogLevel level, ConsoleColor? color) {
					Value = value;
					Level = level;
					Color = color;
				}
			}
		}
		#endregion
	}
}
