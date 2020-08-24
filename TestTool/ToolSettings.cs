using System;
using System.Cli;
using System.IO;

namespace TestTool {
	internal sealed class ToolSettings {
		private string _assemblyPath;

		[Argument("-f", IsRequired = true, Type = "FILE", Description = "Assembly path")]
		internal string AssemblyPathCliSetter {
			set => AssemblyPath = value;
		}

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
