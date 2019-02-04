using System;
using System.Diagnostics;
using Mdlib.PE;

namespace Tool.Loader.Auto {
	internal static class Program {
		private static void Main(string[] args) {
			if (args == null || args.Length == 0)
				return;

			bool lastIsF;
			string assemblyPath;
			string newLoaderName;
			ProcessStartInfo startInfo;
			Process process;

			lastIsF = false;
			assemblyPath = null;
			foreach (string arg in args) {
				if (lastIsF) {
					assemblyPath = arg;
					break;
				}
				else {
					if (arg == "-f")
						lastIsF = true;
				}
			}
			using (IPEImage peImage = PEImageFactory.Create(assemblyPath)) {
				string version;

				version = peImage.Metadata.StorageSignature.DisplayVersionString;
				if (peImage.Is64Bit)
					newLoaderName = version.StartsWith("v4") ? "Tool.Loader.CLR40.x64.exe" : "Tool.Loader.CLR20.x64.exe";
				else
					newLoaderName = version.StartsWith("v4") ? "Tool.Loader.CLR40.x86.exe" : "Tool.Loader.CLR20.x86.exe";
			}
			Console.WriteLine("Using loader: " + newLoaderName);
			Console.WriteLine();
			startInfo = new ProcessStartInfo(newLoaderName, GetArgument(Environment.CommandLine)) {
				CreateNoWindow = false,
				UseShellExecute = false
			};
			process = new Process {
				StartInfo = startInfo
			};
			process.Start();
			process.WaitForExit();
			if (IsN00bUser() || Debugger.IsAttached) {
				Console.WriteLine("Press any key to exit...");
				try {
					Console.ReadKey(true);
				}
				catch {
				}
			}
		}

		private static string GetArgument(string commandLine) {
			bool hasQuote;
			int startIndex;

			commandLine = commandLine.Trim();
			hasQuote = commandLine[0] == '"';
			startIndex = hasQuote ? (commandLine.IndexOf('"', 1) + 1) : commandLine.IndexOf(' ');
			return commandLine.Substring(startIndex).Trim();
		}

		private static bool IsN00bUser() {
			if (HasEnv("VisualStudioDir"))
				return false;
			if (HasEnv("SHELL"))
				return false;
			return HasEnv("windir") && !HasEnv("PROMPT");
		}

		private static bool HasEnv(string name) {
			foreach (object key in Environment.GetEnvironmentVariables().Keys) {
				string env;

				env = key as string;
				if (env == null)
					continue;
				if (string.Equals(env, name, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
