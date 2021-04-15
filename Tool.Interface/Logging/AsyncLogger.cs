using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Tool.Logging {
	/// <summary>
	/// Async logger.
	/// In derived class, you must override <see cref="LogCore"/>.
	/// </summary>
	public class AsyncLogger : ILogger {
		private readonly LoggerCore _core;
		private bool _isFreed;

		/// <inheritdoc />
		public virtual bool IsLocked => _core.Context.IsLocked;

		/// <summary>
		/// Constructor
		/// </summary>
		protected AsyncLogger() {
			_core = new LoggerCore(new Context(this, this, LogCore));
		}

		private AsyncLogger(LoggerCore core) {
			_core = core;
		}

		/// <summary>
		/// Immediately write the log without buffer. Derived class must override this method!
		/// </summary>
		/// <param name="value"></param>
		/// <param name="level"></param>
		/// <param name="color"></param>
		protected virtual void LogCore(string value, LogLevel level, ConsoleColor? color) {
			throw new NotImplementedException($"In derived class, you must override '{nameof(LogCore)}'");
		}

		/// <inheritdoc />
		public virtual ILogger EnterLock() {
			CheckFreed();
			var context = _core.Context;
			if (this != context.Creator)
				throw new InvalidOperationException("Nested lock is not supported");

			relock:
			lock (context.LockObj) {
				if (context.IsLocked) {
					Monitor.Wait(context.LockObj);
					goto relock;
				}

				context.Owner = new AsyncLogger(_core);
				context.IsLocked = true;
				return context.Owner;
			}
		}

		/// <inheritdoc />
		public virtual ILogger ExitLock() {
			CheckFreed();
			var context = _core.Context;
			if (context.Creator == this)
				throw new InvalidOperationException("No lock can be exited");

			_isFreed = true;
			context.Owner = context.Creator;
			context.IsLocked = false;
			lock (context.LockObj)
				Monitor.PulseAll(context.LockObj);
			return context.Owner;
		}

		/// <summary>
		/// Checks current logger is freed
		/// </summary>
		protected void CheckFreed() {
			if (_isFreed)
				throw new InvalidOperationException("Current logger is freed");
		}

		/// <summary>
		/// Format exception
		/// </summary>
		/// <param name="exception"></param>
		/// <returns></returns>
		protected static string FormatException(Exception? exception) {
			var sb = new StringBuilder();
			DumpException(exception, sb);
			return sb.ToString();
		}

		private static void DumpException(Exception? exception, StringBuilder sb) {
			exception ??= new ArgumentNullException(nameof(exception), "<No exception object>");
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

		#region forwards
		/// <inheritdoc />
		public virtual LogLevel Level {
			get {
				CheckFreed();
				return _core.Level;
			}
			set {
				CheckFreed();
				_core.Level = value;
			}
		}

		/// <inheritdoc />
		public virtual bool IsAsync {
			get {
				CheckFreed();
				return _core.IsAsync;
			}
			set {
				CheckFreed();
				_core.IsAsync = value;
			}
		}

		/// <inheritdoc />
		public virtual bool IsIdle {
			get {
				CheckFreed();
				return LoggerCore.IsIdle;
			}
		}

		/// <inheritdoc />
		public virtual int QueueCount {
			get {
				CheckFreed();
				return LoggerCore.QueueCount;
			}
		}

		/// <inheritdoc />
		public virtual void Info() {
			CheckFreed();
			_core.Info(this);
		}

		/// <inheritdoc />
		public virtual void Info(string? value) {
			CheckFreed();
			_core.Info(value, this);
		}

		/// <inheritdoc />
		public virtual void Warning(string? value) {
			CheckFreed();
			_core.Warning(value, this);
		}

		/// <inheritdoc />
		public virtual void Error(string? value) {
			CheckFreed();
			_core.Error(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose1(string? value) {
			CheckFreed();
			_core.Verbose1(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose2(string? value) {
			CheckFreed();
			_core.Verbose2(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose3(string? value) {
			CheckFreed();
			_core.Verbose3(value, this);
		}

		/// <inheritdoc />
		public virtual void Exception(Exception? value) {
			CheckFreed();
			_core.Exception(value, this);
		}

		/// <inheritdoc />
		public virtual void Log(string? value, LogLevel level, ConsoleColor? color = null) {
			CheckFreed();
			_core.Log(value, level, color, this);
		}

		/// <inheritdoc />
		public virtual void Flush() {
			CheckFreed();
			LoggerCore.Flush();
		}
		#endregion

		#region core
		private delegate void LogCallback(string value, LogLevel level, ConsoleColor? color);

		private sealed class Context {
			public readonly object LockObj = new();

			public ILogger Creator;
			public volatile ILogger Owner;
			public volatile bool IsLocked;
			public readonly LogCallback Callback;

			public Context(ILogger creator, ILogger owner, LogCallback callback) {
				Creator = creator;
				Owner = owner;
				Callback = callback;
			}
		}

		private sealed class LoggerCore {
			private static bool _isIdle = true;
			private static readonly object _logLock = new();
			private static readonly ManualResetEvent _asyncIdleEvent = new(true);
			private static readonly Queue<LogItem> _asyncQueue = new();
			private static readonly object _asyncLock = new();
			private static readonly Thread _asyncWorker = new(AsyncLoop) {
				Name = $"{nameof(AsyncLogger)}.{nameof(AsyncLoop)}",
				IsBackground = true
			};

			private readonly Context _context;
			private LogLevel _level;
			private volatile bool _isAsync;

			public Context Context => _context;

			public LogLevel Level {
				get => _level;
				set => _level = value;
			}

			public bool IsAsync {
				get => _isAsync;
				set {
					if (value == _isAsync)
						return;

					lock (_logLock) {
						_isAsync = value;
						if (!value)
							Flush();
					}
				}
			}

			public static bool IsIdle => _isIdle;

			public static int QueueCount => _asyncQueue.Count;

			public LoggerCore(Context context) {
				_context = context ?? throw new ArgumentNullException(nameof(context));
				_level = LogLevel.Info;
				_isAsync = true;
			}

			public void Info(ILogger logger) {
				Log(string.Empty, LogLevel.Info, null, logger);
			}

			public void Info(string? value, ILogger logger) {
				Log(value, LogLevel.Info, ConsoleColor.Gray, logger);
			}

			public void Warning(string? value, ILogger logger) {
				Log(value, LogLevel.Warning, ConsoleColor.Yellow, logger);
			}

			public void Error(string? value, ILogger logger) {
				Log(value, LogLevel.Error, ConsoleColor.Red, logger);
			}

			public void Verbose1(string? value, ILogger logger) {
				Log(value, LogLevel.Verbose1, ConsoleColor.DarkGray, logger);
			}

			public void Verbose2(string? value, ILogger logger) {
				Log(value, LogLevel.Verbose2, ConsoleColor.DarkGray, logger);
			}

			public void Verbose3(string? value, ILogger logger) {
				Log(value, LogLevel.Verbose3, ConsoleColor.DarkGray, logger);
			}

			public void Exception(Exception? value, ILogger logger) {
				Error(FormatException(value), logger);
			}

			public void Log(string? value, LogLevel level, ConsoleColor? color, ILogger logger) {
				if (_context.IsLocked) {
				relock:
					if (_context.Owner != logger) {
						lock (_context.LockObj) {
							if (_context.Owner != logger) {
								Monitor.Wait(_context.LockObj);
								goto relock;
							}
						}
					}
				}

				if (level > Level)
					return;

				value ??= string.Empty;
				lock (_logLock) {
					if (_isAsync) {
						lock (_asyncLock) {
							_asyncQueue.Enqueue(new(_context.Callback, value, level, color));
							if ((_asyncWorker.ThreadState & ThreadState.Unstarted) != 0)
								_asyncWorker.Start();
							Monitor.Pulse(_asyncLock);
						}
					}
					else {
						_context.Callback(value, level, color);
					}
				}
			}

			public static void Flush() {
				_asyncIdleEvent.WaitOne();
			}

			private static void AsyncLoop() {
				var sb = new StringBuilder();
				while (true) {
					lock (_asyncLock) {
						if (_asyncQueue.Count == 0) {
							_isIdle = true;
							_asyncIdleEvent.Set();
							Monitor.Wait(_asyncLock);
						}
						_isIdle = false;
						_asyncIdleEvent.Reset();
					}
					// 等待输出被触发

					LogItem[] logItems;
					lock (_asyncLock) {
						logItems = _asyncQueue.ToArray();
						_asyncQueue.Clear();
					}
					var currentsByCallback = logItems.GroupBy(t => t.Callback).Select(t => new Queue<LogItem>(t)).ToArray();
					// 获取全部要输出的内容

					foreach (var currents in currentsByCallback) {
						// 按回调方法分组输出
						var callback = currents.Peek().Callback;
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
							callback(sb.ToString(), current.Level, color);
						} while (currents.Count > 0);
					}
				}
			}

			private class LogItem {
				public LogCallback Callback;
				public string Value;
				public LogLevel Level;
				public ConsoleColor? Color;

				public LogItem(LogCallback callback, string value, LogLevel level, ConsoleColor? color) {
					Callback = callback;
					Value = value;
					Level = level;
					Color = color;
				}
			}
		}
		#endregion
	}
}
