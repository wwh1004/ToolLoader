using System;
using System.IO;
using System.Text;

namespace Tool.Logging {
	/// <summary>
	/// Default logger implement
	/// </summary>
	public sealed class DefaultLogger : AsyncLogger {
		private static readonly byte[] Newline = Encoding.ASCII.GetBytes(Environment.NewLine);

		private readonly bool _writeConsole;
		private readonly Stream? _stream;
		private readonly Encoding? _encoding;

		/// <summary>
		/// Logger instance which only writes console
		/// </summary>
		public static ILogger ConsoleOnlyInstance { get; } = new DefaultLogger();

		/// <summary>
		/// Constructor
		/// Do NOT make it public! Multi <see cref="AsyncLogger.LogCallback"/> instances will cause <see cref="AsyncLogger.LoggerCore.AsyncLoop"/> slowly!
		/// </summary>
		private DefaultLogger() {
			_writeConsole = true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writeConsole"></param>
		/// <param name="stream"></param>
		public DefaultLogger(bool writeConsole, Stream stream) : this(writeConsole, stream, Encoding.UTF8) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="writeConsole"></param>
		/// <param name="stream"></param>
		/// <param name="encoding"></param>
		public DefaultLogger(bool writeConsole, Stream stream, Encoding encoding) {
			_writeConsole = writeConsole;
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			_encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
		}

		/// <inheritdoc />
		protected override void LogCore(string value, LogLevel level, ConsoleColor? color) {
			if (_writeConsole) {
				ConsoleColor oldColor = default;
				if (color.HasValue) {
					oldColor = Console.ForegroundColor;
					Console.ForegroundColor = color.Value;
				}
				Console.WriteLine(value ?? string.Empty);
				if (color.HasValue)
					Console.ForegroundColor = oldColor;
			}
			if (_stream is not null) {
				if (!string.IsNullOrEmpty(value)) {
					byte[] bytes = _encoding!.GetBytes(value);
					_stream.Write(bytes, 0, bytes.Length);
				}
				_stream.Write(Newline, 0, Newline.Length);
				_stream.Flush();
			}
		}
	}
}
