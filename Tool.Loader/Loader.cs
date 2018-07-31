using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Tool.Interface;

namespace Tool.Loader {
	internal static class Loader {
		public static void Execute(string[] args) {
			string toolPath;
			string filePath;
			string[] otherArgs;
			ITool tool;

			if (args != null && args.Length > 1) {
				toolPath = args[0];
				filePath = args[1];
				otherArgs = new string[args.Length - 2];
				if (otherArgs.Length != 0)
					Array.Copy(args, 2, otherArgs, 0, otherArgs.Length);
			}
			else {
				Console.WriteLine("Enter tool path:");
				toolPath = Console.ReadLine();
				Console.WriteLine("Enter .NET Assembly path:");
				filePath = Console.ReadLine();
				Console.WriteLine("Enter other args:");
				otherArgs = Console.ReadLine().Split(' ');
			}
			tool = CreateToolInstance(Assembly.LoadFile(Path.GetFullPath(toolPath)));
			Console.Title = tool.Title;
			tool.Execute(filePath, otherArgs);
			if (IsN00bUser() || Debugger.IsAttached) {
				Console.Error.WriteLine("\n\nPress any key to exit...\n");
				try {
					Console.ReadKey(true);
				}
				catch (InvalidOperationException) {
				}
			}
		}

		private static ITool CreateToolInstance(Assembly assembly) {
			Type toolType;

			toolType = typeof(ITool);
			foreach (Type type in assembly.ManifestModule.GetTypes())
				foreach (Type interfaceType in type.GetInterfaces())
					if (interfaceType == toolType)
						return (ITool)Activator.CreateInstance(type);
			throw new InvalidOperationException();
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
