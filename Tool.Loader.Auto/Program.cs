using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tool.Loader.Auto;

static class Program {
	struct SectionHeader {
		public uint VirtualSize;
		public uint VirtualAddress;
		public uint SizeOfRawData;
		public uint PointerToRawData;

		public SectionHeader(uint virtualSize, uint virtualAddress, uint sizeOfRawData, uint pointerToRawData) {
			VirtualSize = virtualSize;
			VirtualAddress = virtualAddress;
			SizeOfRawData = sizeOfRawData;
			PointerToRawData = pointerToRawData;
		}
	}

	static void Main(string[] args) {
		if (args is null || args.Length == 0)
			return;


		string? assemblyPath = GetAssemblyPath(args);
		string loaderName;
		if (assemblyPath is null) {
			Console.WriteLine("Assembly path not found.");
			loaderName = "Tool.Loader.CLR40.x64.exe";
		}
		else {
			try {
				bool is64BitTargetTool;
				string versionTargetTool;
				using (var stream = new FileStream(args[0], FileMode.Open, FileAccess.Read))
					GetDotNetInfo(stream, out is64BitTargetTool, out versionTargetTool);
				bool is64BitTargetAssembly;
				string versionTargetAssembly;
				using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
					GetDotNetInfo(stream, out is64BitTargetAssembly, out versionTargetAssembly);
				bool use64Bit = is64BitTargetTool || is64BitTargetAssembly;
				bool useClr4x = versionTargetTool.StartsWith("v4", StringComparison.Ordinal) || versionTargetAssembly.StartsWith("v4", StringComparison.Ordinal);
				loaderName = $"Tool.Loader.{(useClr4x ? "CLR40" : "CLR20")}.{(use64Bit ? "x64" : "x86")}.exe";
			}
			catch {
				Console.Error.WriteLine("Error occured on determining the exact loader.");
				loaderName = "Tool.Loader.CLR40.x64.exe";
			}
		}
		Console.WriteLine("Using loader: " + loaderName);
		Console.WriteLine();
		using var process = new Process {
			StartInfo = new ProcessStartInfo(loaderName, GetArgument(Environment.CommandLine)) {
				CreateNoWindow = false,
				UseShellExecute = false
			}
		};
		process.Start();
		process.WaitForExit();
	}

	static string? GetAssemblyPath(string[] args) {
		string? defaultOption = null;
		string? fOption = null;
		for (int i = 1; i < args.Length; i++) {
			var arg = args[i];
			if (MaybeOptionName(arg)) {
				// 选项
				if (i == args.Length - 1)
					break;
				// 如果当前是最后一个参数，退出循环，不需要判断了
				if (MaybeOptionName(args[i + 1]))
					continue;
				// 如果下一个字符串也是选项名，进入下一次循环
				if (string.Equals(arg, "-f", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "/f", StringComparison.OrdinalIgnoreCase))
					fOption = args[i + 1];
			}
			else {
				// 默认选项
				defaultOption = arg;
			}
		}
		try {
			if (defaultOption is not null && File.Exists(defaultOption))
				return defaultOption;
		}
		catch {
		}
		try {
			if (fOption is not null && File.Exists(fOption))
				return defaultOption;
		}
		catch {
		}
		return null;

		static bool MaybeOptionName(string arg) {
			return arg.StartsWith("-", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal);
		}
	}

	static string GetArgument(string commandLine) {
		commandLine = commandLine.Trim();
		bool hasQuote = commandLine[0] == '"';
		int startIndex = hasQuote ? (commandLine.IndexOf('"', 1) + 1) : commandLine.IndexOf(' ');
		return commandLine.Substring(startIndex).Trim();
	}

	static void GetDotNetInfo(Stream stream, out bool is64Bit, out string version) {
		version = string.Empty;
		using var reader = new BinaryReader(stream);
		GetPEInfo(reader, out uint peOffset, out is64Bit);
		reader.BaseStream.Position = peOffset + (is64Bit ? 0xF8 : 0xE8);
		uint rva = reader.ReadUInt32();
		if (rva == 0)
			return;
		var sectionHeaders = GetSectionHeaders(reader);
		var sectionHeader = GetSectionHeader(rva, sectionHeaders);
		if (sectionHeader is null)
			return;
		reader.BaseStream.Position = sectionHeader.Value.PointerToRawData + rva - sectionHeader.Value.VirtualAddress + 0x8;
		rva = reader.ReadUInt32();
		if (rva == 0)
			return;
		sectionHeader = GetSectionHeader(rva, sectionHeaders);
		if (sectionHeader is null)
			return;
		reader.BaseStream.Position = sectionHeader.Value.PointerToRawData + rva - sectionHeader.Value.VirtualAddress + 0xC;
		version = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32() - 2));
	}

	static void GetPEInfo(BinaryReader reader, out uint peOffset, out bool is64Bit) {
		reader.BaseStream.Position = 0x3C;
		peOffset = reader.ReadUInt32();
		reader.BaseStream.Position = peOffset + 0x4;
		ushort machine = reader.ReadUInt16();
		is64Bit = machine == 0x8664;
	}

	static SectionHeader[] GetSectionHeaders(BinaryReader reader) {
		GetPEInfo(reader, out uint ntHeaderOffset, out bool is64);
		ushort numberOfSections = reader.ReadUInt16();
		reader.BaseStream.Position = ntHeaderOffset + (is64 ? 0x108 : 0xF8);
		var sectionHeaders = new SectionHeader[numberOfSections];
		for (int i = 0; i < numberOfSections; i++) {
			reader.BaseStream.Position += 0x8;
			sectionHeaders[i] = new SectionHeader(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
			reader.BaseStream.Position += 0x10;
		}
		return sectionHeaders;
	}

	static SectionHeader? GetSectionHeader(uint rva, SectionHeader[] sectionHeaders) {
		foreach (var sectionHeader in sectionHeaders) {
			if (rva >= sectionHeader.VirtualAddress && rva < sectionHeader.VirtualAddress + Math.Max(sectionHeader.VirtualSize, sectionHeader.SizeOfRawData))
				return sectionHeader;
		}
		return null;
	}
}
