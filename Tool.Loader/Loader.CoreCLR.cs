using System;
using System.Cli;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using Tool.Interface;

namespace Tool.Loader {
	internal static unsafe class Loader {
		private const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern char** CommandLineToArgv(string lpCmdLine, int* pNumArgs);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern void* LocalFree(void* hMem);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

#if !NETCORE21
		private static AssemblyDependencyResolver _dependencyResolver;
		private static HashSet<string> _resolvingAssemblies;
#else
		private static HashSet<string> _resolvingAssemblies;
#endif

		public static void Execute(string[] args) {
			try {
				Console.Title = GetTitle();
			}
			catch {
			}
			if (args is null || args.Length == 0) {
				// 直接运行加载器或调试时使用
				var commandLine = new StringBuilder();
				Console.WriteLine("Specify tool path:");
				commandLine.Append(Console.ReadLine());
				Console.WriteLine("Specify arguments:");
				commandLine.Append(Console.ReadLine());
				Console.WriteLine();
				Console.WriteLine();
				Execute(CommandLineToArgs(commandLine.ToString()));
				return;
			}
			string toolPath = Path.GetFullPath(args[0]);
#if !NETCORE21
			_dependencyResolver = new AssemblyDependencyResolver(toolPath);
			_resolvingAssemblies = new HashSet<string>();
			AssemblyLoadContext.Default.Resolving += ResolveAssembly;
			AssemblyLoadContext.Default.ResolvingUnmanagedDll += ResolveUnmanagedDll;
#else
			_resolvingAssemblies = new HashSet<string>();
			AssemblyLoadContext.Default.Resolving += ResolveAssembly;
#endif
			object tool = CreateToolInstance(GetOrLoadAssembly(toolPath), out var toolSettingsType);
			try {
				Console.Title = (string)tool.GetType().GetProperty("Title").GetValue(tool, null);
			}
			catch {
			}
			string[] toolArguments = new string[args.Length - 1];
			for (int i = 0; i < toolArguments.Length; i++)
				toolArguments[i] = args[i + 1];
			object[] invokeParameters = new object[] { toolArguments, null };
			if ((bool)typeof(CommandLine).GetMethod("TryParse").MakeGenericMethod(toolSettingsType).Invoke(null, invokeParameters)) {
				var executeStub = typeof(Loader).GetMethod("ExecuteStub", BindingFlags.NonPublic | BindingFlags.Static);
				var realExecute = tool.GetType().GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { toolSettingsType }, null);
				RuntimeHelpers.PrepareMethod(executeStub.MethodHandle);
				RuntimeHelpers.PrepareMethod(realExecute.MethodHandle);
				byte* address = GetLastJmpAddress((byte*)executeStub.MethodHandle.GetFunctionPointer());
				byte* target = GetLastJmpAddress((byte*)realExecute.MethodHandle.GetFunctionPointer());
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

#if !NETCORE21
		private static Assembly ResolveAssembly(AssemblyLoadContext loadContext, AssemblyName assemblyName) {
			try {
				if (!_resolvingAssemblies.Add(assemblyName.FullName))
					return null;
				var assembly = loadContext.LoadFromAssemblyName(assemblyName);
				_resolvingAssemblies.Remove(assemblyName.FullName);
				return assembly;
			}
			catch {
				string assemblyPath = _dependencyResolver.ResolveAssemblyToPath(assemblyName);
				if (assemblyPath is null)
					return null;
				return loadContext.LoadFromAssemblyPath(assemblyPath);
			}
		}

		private static IntPtr ResolveUnmanagedDll(Assembly assembly, string unmanagedDllName) {
			string unmanagedDllPath = _dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
			if (unmanagedDllPath is null)
				return IntPtr.Zero;
			throw new NotImplementedException($"Can't load {unmanagedDllName}");
		}
#else
		private static Assembly ResolveAssembly(AssemblyLoadContext loadContext, AssemblyName assemblyName) {
			try {
				if (!_resolvingAssemblies.Add(assemblyName.FullName))
					return null;
				var assembly = loadContext.LoadFromAssemblyName(assemblyName);
				_resolvingAssemblies.Remove(assemblyName.FullName);
				return assembly;
			}
			catch {
				string assemblyPath = Path.Combine(Environment.CurrentDirectory, assemblyName.Name + ".dll");
				if (!File.Exists(assemblyPath))
					return null;
				return loadContext.LoadFromAssemblyPath(assemblyPath);
			}
		}
#endif

		private static string GetTitle() {
			string productName = GetAssemblyAttribute<AssemblyProductAttribute>().Product;
			string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			string copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright.Substring(12);
			int firstBlankIndex = copyright.IndexOf(' ');
			string copyrightOwnerName = copyright.Substring(firstBlankIndex + 1);
			string copyrightYear = copyright.Substring(0, firstBlankIndex);
			return $"{productName} v{version} by {copyrightOwnerName} {copyrightYear}";
		}

		private static T GetAssemblyAttribute<T>() {
			return (T)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false)[0];
		}

		private static string[] CommandLineToArgs(string commandLine) {
			int length;
			char** pArgs = CommandLineToArgv(commandLine, &length);
			if (pArgs == null)
				return null;
			string[] args = new string[length];
			for (int i = 0; i < length; i++)
				args[i] = new string(pArgs[i]);
			LocalFree(pArgs);
			return args;
		}

		private static Assembly GetOrLoadAssembly(string assemblyPath) {
			string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
			var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => string.Equals(t.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
			return assembly ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
		}

		private static object CreateToolInstance(Assembly assembly, out Type toolSettingsType) {
			var toolTypeGenericDefinition = typeof(ITool<>);
			foreach (var type in assembly.ManifestModule.GetTypes()) {
				foreach (var interfaceType in type.GetInterfaces()) {
					if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != toolTypeGenericDefinition)
						continue;
					var genericArguments = interfaceType.GetGenericArguments();
					if (genericArguments.Length != 1)
						continue;
					toolSettingsType = genericArguments[0];
					return Activator.CreateInstance(type);
				}
			}
			throw new InvalidOperationException("Tool type not found");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
		private static void ExecuteStub(object @this, object settings) {
			throw new Exception("ExecuteStub");
		}

		private static byte* GetLastJmpAddress(byte* pFirstJmp) {
			return *pFirstJmp == 0xE9 ? GetLastJmpAddress(pFirstJmp + 5 + *(int*)(pFirstJmp + 1)) : pFirstJmp;
		}

		private static void WriteJmp(void* address, void* target) {
			byte[] jmpStub;
			if (sizeof(void*) == 4) {
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
			VirtualProtect(address, (uint)value.Length, PAGE_EXECUTE_READWRITE, out uint oldProtection);
			Marshal.Copy(value, 0, (IntPtr)address, value.Length);
			VirtualProtect(address, (uint)value.Length, oldProtection, out _);
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
				if (!(key is string env))
					continue;
				if (string.Equals(env, name, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
