#pragma warning disable CS1591
using System.Runtime.InteropServices;

namespace Mdlib {
	public static unsafe class NativeMethods {
		public const ushort IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 16;
		public const ushort IMAGE_SIZEOF_SHORT_NAME = 8;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_DOS_HEADER {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_DOS_HEADER);

			public ushort e_magic;
			public ushort e_cblp;
			public ushort e_cp;
			public ushort e_crlc;
			public ushort e_cparhdr;
			public ushort e_minalloc;
			public ushort e_maxalloc;
			public ushort e_ss;
			public ushort e_sp;
			public ushort e_csum;
			public ushort e_ip;
			public ushort e_cs;
			public ushort e_lfarlc;
			public ushort e_ovno;
			public fixed ushort e_res[4];
			public ushort e_oemid;
			public ushort e_oeminfo;
			public fixed ushort e_res2[10];
			public uint e_lfanew;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_NT_HEADERS32 {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_NT_HEADERS32);

			public uint Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_NT_HEADERS64 {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_NT_HEADERS64);

			public uint Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_FILE_HEADER {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_FILE_HEADER);

			public ushort Machine;
			public ushort NumberOfSections;
			public uint TimeDateStamp;
			public uint PointerToSymbolTable;
			public uint NumberOfSymbols;
			public ushort SizeOfOptionalHeader;
			public ushort Characteristics;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_OPTIONAL_HEADER32 {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_OPTIONAL_HEADER32);

			public ushort Magic;
			public byte MajorLinkerVersion;
			public byte MinorLinkerVersion;
			public uint SizeOfCode;
			public uint SizeOfInitializedData;
			public uint SizeOfUninitializedData;
			public uint AddressOfEntryPoint;
			public uint BaseOfCode;
			public uint BaseOfData;
			public uint ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort MajorOperatingSystemVersion;
			public ushort MinorOperatingSystemVersion;
			public ushort MajorImageVersion;
			public ushort MinorImageVersion;
			public ushort MajorSubsystemVersion;
			public ushort MinorSubsystemVersion;
			public uint Win32VersionValue;
			public uint SizeOfImage;
			public uint SizeOfHeaders;
			public uint CheckSum;
			public ushort Subsystem;
			public ushort DllCharacteristics;
			public uint SizeOfStackReserve;
			public uint SizeOfStackCommit;
			public uint SizeOfHeapReserve;
			public uint SizeOfHeapCommit;
			public uint LoaderFlags;
			public uint NumberOfRvaAndSizes;
			public fixed ulong DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_OPTIONAL_HEADER64 {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_OPTIONAL_HEADER64);

			public ushort Magic;
			public byte MajorLinkerVersion;
			public byte MinorLinkerVersion;
			public uint SizeOfCode;
			public uint SizeOfInitializedData;
			public uint SizeOfUninitializedData;
			public uint AddressOfEntryPoint;
			public uint BaseOfCode;
			public ulong ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort MajorOperatingSystemVersion;
			public ushort MinorOperatingSystemVersion;
			public ushort MajorImageVersion;
			public ushort MinorImageVersion;
			public ushort MajorSubsystemVersion;
			public ushort MinorSubsystemVersion;
			public uint Win32VersionValue;
			public uint SizeOfImage;
			public uint SizeOfHeaders;
			public uint CheckSum;
			public ushort Subsystem;
			public ushort DllCharacteristics;
			public ulong SizeOfStackReserve;
			public ulong SizeOfStackCommit;
			public ulong SizeOfHeapReserve;
			public ulong SizeOfHeapCommit;
			public uint LoaderFlags;
			public uint NumberOfRvaAndSizes;
			public fixed ulong DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_DATA_DIRECTORY {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_DATA_DIRECTORY);

			public uint VirtualAddress;
			public uint Size;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_SECTION_HEADER {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_SECTION_HEADER);

			public fixed byte Name[IMAGE_SIZEOF_SHORT_NAME];
			public uint VirtualSize;
			public uint VirtualAddress;
			public uint SizeOfRawData;
			public uint PointerToRawData;
			public uint PointerToRelocations;
			public uint PointerToLinenumbers;
			public ushort NumberOfRelocations;
			public ushort NumberOfLinenumbers;
			public uint Characteristics;
		}
	}
}
#pragma warning restore CS1591
