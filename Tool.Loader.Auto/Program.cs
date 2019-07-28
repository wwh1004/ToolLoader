using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tool.Loader.Auto {
	internal static class Program {
		private struct SectionHeader {
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

		private static void Main(string[] args) {
			if (args == null || args.Length == 0)
				return;

			bool lastIsF;
			string assemblyPath;
			string newLoaderName;
			ProcessStartInfo startInfo;

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
			if (assemblyPath is null) {
				Console.WriteLine("Not found -f argument.");
				newLoaderName = "Tool.Loader.CLR40.x86.exe";
			}
			else
				try {
					bool is64Bit;
					string version;

					using (FileStream stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read))
						GetDotNetInfo(stream, out is64Bit, out version);
					if (is64Bit)
						newLoaderName = version.StartsWith("v4") ? "Tool.Loader.CLR40.x64.exe" : "Tool.Loader.CLR20.x64.exe";
					else
						newLoaderName = version.StartsWith("v4") ? "Tool.Loader.CLR40.x86.exe" : "Tool.Loader.CLR20.x86.exe";
				}
				catch {
					Console.WriteLine("Error occured on getting target assembly target framework version.");
					newLoaderName = "Tool.Loader.CLR40.x86.exe";
				}
			Console.WriteLine("Using loader: " + newLoaderName);
			Console.WriteLine();
			startInfo = new ProcessStartInfo(newLoaderName, GetArgument(Environment.CommandLine)) {
				CreateNoWindow = false,
				UseShellExecute = false
			};
			using (Process process = new Process {
				StartInfo = startInfo
			}) {
				process.Start();
				process.WaitForExit();
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

		private static void GetDotNetInfo(Stream stream, out bool is64Bit, out string version) {
			BinaryReader reader;
			uint peOffset;
			SectionHeader[] sectionHeaders;
			uint rva;
			SectionHeader? sectionHeader;

			version = default;
#pragma warning disable IDE0067
			reader = new BinaryReader(stream);
#pragma warning restore IDE0067
			GetPEInfo(reader, out peOffset, out is64Bit);
			reader.BaseStream.Position = peOffset + (is64Bit ? 0xF8 : 0xE8);
			rva = reader.ReadUInt32();
			if (rva == 0)
				return;
			sectionHeaders = GetSectionHeaders(reader);
			sectionHeader = GetSectionHeader(rva, sectionHeaders);
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

		private static void GetPEInfo(BinaryReader reader, out uint peOffset, out bool is64Bit) {
			ushort machine;

			reader.BaseStream.Position = 0x3C;
			peOffset = reader.ReadUInt32();
			reader.BaseStream.Position = peOffset + 0x4;
			machine = reader.ReadUInt16();
			is64Bit = machine == 0x8664;
		}

		private static SectionHeader[] GetSectionHeaders(BinaryReader reader) {
			uint ntHeaderOffset;
			bool is64;
			ushort numberOfSections;
			SectionHeader[] sectionHeaders;

			GetPEInfo(reader, out ntHeaderOffset, out is64);
			numberOfSections = reader.ReadUInt16();
			reader.BaseStream.Position = ntHeaderOffset + (is64 ? 0x108 : 0xF8);
			sectionHeaders = new SectionHeader[numberOfSections];
			for (int i = 0; i < numberOfSections; i++) {
				reader.BaseStream.Position += 0x8;
				sectionHeaders[i] = new SectionHeader(reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
				reader.BaseStream.Position += 0x10;
			}
			return sectionHeaders;
		}

		private static SectionHeader? GetSectionHeader(uint rva, SectionHeader[] sectionHeaders) {
			foreach (SectionHeader sectionHeader in sectionHeaders)
				if (rva >= sectionHeader.VirtualAddress && rva < sectionHeader.VirtualAddress + Math.Max(sectionHeader.VirtualSize, sectionHeader.SizeOfRawData))
					return sectionHeader;
			return null;
		}
	}
}
