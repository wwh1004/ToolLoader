using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Tool.Interface;

namespace Tool.Loader {
	internal static unsafe class Loader {
		[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CommandLineToArgvW", ExactSpelling = true, SetLastError = true)]
		private static extern char** CommandLineToArgv(string lpCmdLine, int* pNumArgs);

		private static readonly string ConsoleTitle = GetAssemblyAttribute<AssemblyProductAttribute>().Product + " v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " by " + GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright.Substring(17);

		private static T GetAssemblyAttribute<T>() => (T)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false)[0];

		public static void Execute(string[] args) {
			string toolPath;
			string filePath;
			string[] otherArgs;
			ITool tool;

			Console.Title = ConsoleTitle;
			if (args == null || args.Length == 0) {
				// 直接运行加载器
				StringBuilder commandLine;

				commandLine = new StringBuilder();
				Console.WriteLine("Enter tool path:");
				commandLine.Append(Console.ReadLine());
				commandLine.Append(" ");
				Console.WriteLine("Enter .NET Assembly path:");
				commandLine.Append(Console.ReadLine());
				commandLine.Append(" ");
				Console.WriteLine("Enter other args:");
				commandLine.Append(Console.ReadLine());
				Execute(CommandLineToArgs(commandLine.ToString()));
				Environment.Exit(0);
			}
			toolPath = args[0];
			if (args.Length == 1) {
				// 仅为工具提供指定CLR环境
				filePath = null;
				otherArgs = null;
			}
			else {
				// 使用指定参数与指定CLR环境启动工具
				filePath = args[1];
				otherArgs = new string[args.Length - 2];
				if (otherArgs.Length != 0)
					Array.Copy(args, 2, otherArgs, 0, otherArgs.Length);
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
		private static string[] CommandLineToArgs(string commandLine) {
			char** pArgs;
			int length;
			string[] args;

			pArgs = CommandLineToArgv(commandLine, &length);
			if (pArgs == null)
				return null;
			args = new string[length];
			for (int i = 0; i < length; i++)
				args[i] = new string(pArgs[i]);
			return args;
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
