using System;
using System.Cli;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Tool.Loader {
	internal static unsafe class Loader {
		public const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern char** CommandLineToArgv(string lpCmdLine, int* pNumArgs);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern void* LocalFree(void* hMem);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

		public static void Execute(string[] args) {
			string toolPath;
			object tool;
			Type toolSettingsType;
			string[] toolArguments;
			object[] invokeParameters;

			try {
				Console.Title = GetTitle();
			}
			catch {
			}
			if (args == null || args.Length == 0) {
				// 直接运行加载器或调试时使用
				StringBuilder commandLine;

				commandLine = new StringBuilder();
				Console.WriteLine("Specify tool path:");
				commandLine.Append(Console.ReadLine());
				Console.WriteLine("Specify arguments:");
				commandLine.Append(Console.ReadLine());
				Console.WriteLine();
				Console.WriteLine();
				Execute(CommandLineToArgs(commandLine.ToString()));
				return;
			}
			toolPath = args[0];
			tool = CreateToolInstance(Assembly.LoadFile(Path.GetFullPath(toolPath)), out toolSettingsType);
			try {
				Console.Title = (string)tool.GetType().GetProperty("Title").GetValue(tool, null);
			}
			catch {
			}
			toolArguments = new string[args.Length - 1];
			for (int i = 0; i < toolArguments.Length; i++)
				toolArguments[i] = args[i + 1];
			invokeParameters = new object[] { toolArguments, null };
			if ((bool)typeof(CommandLine).GetMethod("TryParse").MakeGenericMethod(toolSettingsType).Invoke(null, invokeParameters)) {
				MethodInfo executeStub;
				MethodInfo realExecute;
				byte* address;
				byte* target;

				executeStub = typeof(Loader).GetMethod("ExecuteStub", BindingFlags.NonPublic | BindingFlags.Static);
				realExecute = tool.GetType().GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { toolSettingsType }, null);
				RuntimeHelpers.PrepareMethod(executeStub.MethodHandle);
				RuntimeHelpers.PrepareMethod(realExecute.MethodHandle);
				address = GetLastJmpAddress((byte*)executeStub.MethodHandle.GetFunctionPointer());
				target = GetLastJmpAddress((byte*)realExecute.MethodHandle.GetFunctionPointer());
				WriteJmp(address, target);
				ExecuteStub(tool, invokeParameters[1]);
			}
			else {
				Console.Error.WriteLine("Unknown command or invalid arguments.");
				typeof(CommandLine).GetMethod("ShowUsage").MakeGenericMethod(toolSettingsType).Invoke(null, null);
			}
			if (IsN00bUser() || Debugger.IsAttached) {
				Console.WriteLine("Press any key to exit...");
				try {
					Console.ReadKey(true);
				}
				catch {
				}
			}
		}

		private static string GetTitle() {
			string productName;
			string version;
			string copyright;
			int firstBlankIndex;
			string copyrightOwnerName;
			string copyrightYear;

			productName = GetAssemblyAttribute<AssemblyProductAttribute>().Product;
			version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright.Substring(12);
			firstBlankIndex = copyright.IndexOf(' ');
			copyrightOwnerName = copyright.Substring(firstBlankIndex + 1);
			copyrightYear = copyright.Substring(0, firstBlankIndex);
			return $"{productName} v{version} by {copyrightOwnerName} {copyrightYear}";
		}

		private static T GetAssemblyAttribute<T>() {
			return (T)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false)[0];
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
			LocalFree(pArgs);
			return args;
		}

		private static object CreateToolInstance(Assembly assembly, out Type toolSettingsType) {
			Type genericToolType;

			genericToolType = typeof(ArgumentAttribute).Module.GetType("Tool.Interface.ITool`1");
			foreach (Type type in assembly.ManifestModule.GetTypes())
				foreach (Type interfaceType in type.GetInterfaces()) {
					Type[] genericArguments;

					genericArguments = interfaceType.GetGenericArguments();
					if (!interfaceType.IsGenericType || genericArguments.Length != 1)
						continue;
					if (interfaceType.IsAssignableFrom(genericToolType.MakeGenericType(genericArguments[0]))) {
						toolSettingsType = genericArguments[0];
						return Activator.CreateInstance(type);
					}
				}
			throw new InvalidOperationException();
		}

		private static void ExecuteStub(object @this, object settings) {
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			Console.WriteLine("allocate memory");
			try {
				Console.WriteLine("no inline");
			}
			catch {
			}
			try {
				Console.WriteLine("no inline");
			}
			catch {
			}
			throw new InvalidOperationException();
		}

		private static byte* GetLastJmpAddress(byte* pFirstJmp) {
			return *pFirstJmp == 0xE9 ? GetLastJmpAddress(pFirstJmp + 5 + *(int*)(pFirstJmp + 1)) : pFirstJmp;
		}

		private static void WriteJmp(void* address, void* target) {
			byte[] jmpStub;

			if (IntPtr.Size == 4) {
				jmpStub = new byte[] {
					0xE9, 0x00, 0x00, 0x00, 0x00
				};
				fixed (byte* p = jmpStub)
					*(int*)(p + 1) = (int)target - (int)address - 5;
			}
			else {
				jmpStub = new byte[] {
					0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, target
					0xFF, 0xE0                                                  // jmp rax
				};
				fixed (byte* p = jmpStub)
					*(ulong*)(p + 2) = (ulong)target;
			}
			Write(address, jmpStub);
			address = (byte*)address + jmpStub.Length;
		}

		private static void Write(void* address, byte[] value) {
			uint oldProtection;

			VirtualProtect(address, (uint)value.Length, PAGE_EXECUTE_READWRITE, out oldProtection);
			Marshal.Copy(value, 0, (IntPtr)address, value.Length);
			VirtualProtect(address, (uint)value.Length, oldProtection, out oldProtection);
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
