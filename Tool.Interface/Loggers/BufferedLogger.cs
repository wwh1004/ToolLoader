using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Tool.Interface;

namespace Tool.Loggers {
	/// <summary>
	/// Buffered logger
	/// </summary>
	public class BufferedLogger : ILogger {
		private readonly Context _context;
		private bool _isFreed;

		/// <inheritdoc />
		public virtual bool IsLocked => _context.IsLocked;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="callback"></param>
		public BufferedLogger(LogCallback callback) : this(new Context(new LoggerCore(callback))) {
			_context.Creator = this;
			_context.Owner = this;
		}

		private BufferedLogger(Context context) {
			_context = context;
		}

		/// <inheritdoc />
		public virtual void Log(string value, LogLevel level, ConsoleColor? color = null) {
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

		/// <inheritdoc />
		public virtual ILogger EnterLock() {
			CheckFreed();
			if (this != _context.Creator)
				throw new InvalidOperationException("Nested lock is not supported");

			relock:
			lock (_context.LockObj) {
				if (_context.IsLocked) {
					Monitor.Wait(_context.LockObj);
					goto relock;
				}

				_context.Owner = new BufferedLogger(_context);
				_context.IsLocked = true;
				return _context.Owner;
			}
		}

		/// <inheritdoc />
		public virtual ILogger ExitLock() {
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

		/// <summary>
		/// Checks current logger is freed
		/// </summary>
		protected void CheckFreed() {
			if (_isFreed)
				throw new InvalidOperationException("Current logger is freed");
		}

		private sealed class Context {
			public readonly object LockObj = new();
			public readonly LoggerCore Core;
			public BufferedLogger Creator;
			public volatile BufferedLogger Owner;
			public volatile bool IsLocked;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
			public Context(LoggerCore core) {
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
				Core = core;
			}
		}

		#region forwards
		/// <inheritdoc />
		public virtual LogLevel Level {
			get {
				CheckFreed();
				return _context.Core.Level;
			}
			set {
				CheckFreed();
				_context.Core.Level = value;
			}
		}

		/// <inheritdoc />
		public virtual bool IsAsync {
			get {
				CheckFreed();
				return _context.Core.IsAsync;
			}
			set {
				CheckFreed();
				_context.Core.IsAsync = value;
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
			_context.Core.Info(this);
		}

		/// <inheritdoc />
		public virtual void Info(string value) {
			_context.Core.Info(value, this);
		}

		/// <inheritdoc />
		public virtual void Warning(string value) {
			_context.Core.Warning(value, this);
		}

		/// <inheritdoc />
		public virtual void Error(string value) {
			_context.Core.Error(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose1(string value) {
			_context.Core.Verbose1(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose2(string value) {
			_context.Core.Verbose2(value, this);
		}

		/// <inheritdoc />
		public virtual void Verbose3(string value) {
			_context.Core.Verbose3(value, this);
		}

		/// <inheritdoc />
		public virtual void Exception(Exception value) {
			_context.Core.Exception(value, this);
		}

		/// <inheritdoc />
		public virtual void Flush() {
			CheckFreed();
			LoggerCore.Flush();
		}
		#endregion

		#region core
		private sealed class LoggerCore {
			private static bool _isIdle = true;
			private static readonly object _logLock = new();
			private static readonly ManualResetEvent _asyncIdleEvent = new(true);
			private static readonly Queue<LogItem> _asyncQueue = new();
			private static readonly object _asyncLock = new();
			private static readonly Thread _asyncWorker = new(AsyncLoop) {
				Name = $"{nameof(BufferedLogger)}.{nameof(AsyncLoop)}",
				IsBackground = true
			};

			private LogCallback _callback;
			private LogLevel _level;
			private volatile bool _isAsync;

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

			public LoggerCore(LogCallback callback) {
				_callback = callback ?? throw new ArgumentNullException(nameof(callback));
				_level = LogLevel.Info;
				_isAsync = true;
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
				if (level > Level)
					return;

				lock (_logLock) {
					if (_isAsync) {
						lock (_asyncLock) {
							_asyncQueue.Enqueue(new(_callback, value, level, color));
							if ((_asyncWorker.ThreadState & ThreadState.Unstarted) != 0)
								_asyncWorker.Start();
							Monitor.Pulse(_asyncLock);
						}
					}
					else {
						_callback(value, level, color);
					}
				}
			}

			public static void Flush() {
				_asyncIdleEvent.WaitOne();
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

					Queue<LogItem>[] currentsByCallback;
					lock (_asyncLock) {
						currentsByCallback = _asyncQueue.GroupBy(t => t.Callback).Select(t => new Queue<LogItem>(t)).ToArray();
						_asyncQueue.Clear();
					}
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
