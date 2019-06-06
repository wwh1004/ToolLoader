using System;
using System.Cli;
using System.IO;

namespace TestTool {
	internal sealed class ToolSettings {
		private string _assemblyPath;

#pragma warning disable IDE0051
		[Argument("-f", IsRequired = true, Type = "FILE", Description = "程序集路径")]
		private string AssemblyPathCliSetter {
			set => AssemblyPath = value;
		}
#pragma warning restore IDE0051

		public string AssemblyPath {
			get => _assemblyPath;
			set {
				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException(nameof(value));
				if (!File.Exists(value))
					throw new FileNotFoundException($"{value} does NOT exists");

				_assemblyPath = Path.GetFullPath(value);
			}
		}
	}
}
