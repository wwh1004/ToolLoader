using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Tool.Interface;

namespace Tool.Loader;

static unsafe class Loader {
	const uint PAGE_EXECUTE_READWRITE = 0x40;

	[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
	static extern char** CommandLineToArgv(string lpCmdLine, int* pNumArgs);

	[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
	static extern void* LocalFree(void* hMem);

	[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
	static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

	public static void Execute(string[] args) {
		try {
			Console.Title = GetTitle(Assembly.GetExecutingAssembly());
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
			var parsedArgs = CommandLineToArgs(commandLine.ToString());
			if (parsedArgs is null)
				throw new InvalidOperationException("Can't parse command line arguments.");
			Execute(parsedArgs);
			return;
		}
		var toolPath = args[0];
		var toolAssembly = GetOrLoadAssembly(toolPath);
		var tool = CreateToolInstance(toolAssembly, out var optionsType);
		try {
			var title = (string?)tool.GetType().GetProperty("Title").GetValue(tool, null);
			if (string.IsNullOrEmpty(title))
				title = GetTitle(toolAssembly);
			Console.Title = title;
		}
		catch {
		}
		var toolArguments = new string[args.Length - 1];
		for (int i = 0; i < toolArguments.Length; i++)
			toolArguments[i] = args[i + 1];
		var invokeParameters = new object?[] { toolArguments, null, null };
		if ((bool)typeof(CommandLine).GetMethod("TryParse").MakeGenericMethod(optionsType).Invoke(null, invokeParameters) && !(bool)invokeParameters[2]!) {
			var executeStub = typeof(Loader).GetMethod("ExecuteStub", BindingFlags.NonPublic | BindingFlags.Static);
			var realExecute = tool.GetType().GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new[] { optionsType }, null);
			RuntimeHelpers.PrepareMethod(executeStub.MethodHandle);
			RuntimeHelpers.PrepareMethod(realExecute.MethodHandle);
			var address = (byte*)executeStub.MethodHandle.GetFunctionPointer();
			var target = (byte*)realExecute.MethodHandle.GetFunctionPointer();
			if (address != target)
				WriteJmp(address, target);
			ExecuteStub(tool, invokeParameters[1]!);
		}
		else {
			typeof(CommandLine).GetMethod("ShowUsage").MakeGenericMethod(optionsType).Invoke(null, null);
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

	static string GetTitle(Assembly assembly) {
		var productName = GetAssemblyAttribute<AssemblyProductAttribute>(assembly).Product;
		var version = assembly.GetName().Version.ToString();
		var copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly).Copyright.Substring(12);
		int firstBlankIndex = copyright.IndexOf(' ');
		var copyrightOwnerName = copyright.Substring(firstBlankIndex + 1);
		var copyrightYear = copyright.Substring(0, firstBlankIndex);
		return $"{productName} v{version} by {copyrightOwnerName} {copyrightYear}";
	}

	static T GetAssemblyAttribute<T>(Assembly assembly) {
		return (T)assembly.GetCustomAttributes(typeof(T), false)[0];
	}

	static string[]? CommandLineToArgs(string commandLine) {
		int length;
		var pArgs = CommandLineToArgv(commandLine, &length);
		if (pArgs == null)
			return null;
		var args = new string[length];
		for (int i = 0; i < length; i++)
			args[i] = new string(pArgs[i]);
		LocalFree(pArgs);
		return args;
	}

	static Assembly GetOrLoadAssembly(string assemblyPath) {
		var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
		var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(t => string.Equals(t.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
		return assembly ?? Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
	}

	static object CreateToolInstance(Assembly assembly, out Type optionsType) {
		var toolTypeGenericDefinition = typeof(ITool<>);
		foreach (var type in assembly.ManifestModule.GetTypes()) {
			foreach (var interfaceType in type.GetInterfaces()) {
				if (!interfaceType.IsGenericType || interfaceType.GetGenericTypeDefinition() != toolTypeGenericDefinition)
					continue;
				var genericArguments = interfaceType.GetGenericArguments();
				if (genericArguments.Length != 1)
					continue;
				optionsType = genericArguments[0];
				return Activator.CreateInstance(type);
			}
		}
		throw new InvalidOperationException("Tool type not found");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable IDE0060 // Remove unused parameter
	static void ExecuteStub(object @this, object options) {
#pragma warning restore IDE0060 // Remove unused parameter
		throw new Exception("ExecuteStub");
	}

	static void WriteJmp(void* address, void* target) {
		byte[] jmpStub;
		if (sizeof(void*) == 4) {
			jmpStub = new byte[] {
				0xE9, 0x00, 0x00, 0x00, 0x00 // jmp rel
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

	static void Write(void* address, byte[] value) {
		VirtualProtect(address, (uint)value.Length, PAGE_EXECUTE_READWRITE, out uint oldProtection);
		Marshal.Copy(value, 0, (IntPtr)address, value.Length);
		VirtualProtect(address, (uint)value.Length, oldProtection, out _);
	}

	static bool IsN00bUser() {
		if (HasEnv("VisualStudioDir"))
			return false;
		if (HasEnv("SHELL"))
			return false;
		return HasEnv("windir") && !HasEnv("PROMPT");
	}

	static bool HasEnv(string name) {
		foreach (var key in Environment.GetEnvironmentVariables().Keys) {
			if (key is not string env)
				continue;
			if (string.Equals(env, name, StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}
}
