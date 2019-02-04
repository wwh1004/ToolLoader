using System;
using System.Diagnostics;
using static Mdlib.NativeMethods;

namespace Mdlib.PE {
	/// <summary>
	/// 可选头
	/// </summary>
	[DebuggerDisplay("OptHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA}]")]
	public sealed unsafe class OptionalHeader : IRawData {
		private readonly void* _rawData;
		private readonly uint _offset;
		private readonly bool _is64Bit;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_OPTIONAL_HEADER32* RawValue32 => _is64Bit ? throw new InvalidOperationException() : (IMAGE_OPTIONAL_HEADER32*)_rawData;

		/// <summary />
		public IMAGE_OPTIONAL_HEADER64* RawValue64 => _is64Bit ? (IMAGE_OPTIONAL_HEADER64*)_rawData : throw new InvalidOperationException();

		/// <summary />
		public RVA RVA => (RVA)_offset;

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => _is64Bit ? IMAGE_OPTIONAL_HEADER64.UnmanagedSize : IMAGE_OPTIONAL_HEADER32.UnmanagedSize;

		/// <summary />
		public bool Is64Bit => _is64Bit;

		/// <summary />
		public OptionalHeaderType Type {
			get => *(OptionalHeaderType*)_rawData;
			set => *(OptionalHeaderType*)_rawData = value;
		}

		/// <summary />
		public RVA EntryPointAddress {
			get {
				if (_is64Bit)
					return (RVA)RawValue64->AddressOfEntryPoint;
				else
					return (RVA)RawValue32->AddressOfEntryPoint;
			}
			set {
				if (_is64Bit)
					RawValue64->AddressOfEntryPoint = (uint)value;
				else
					RawValue32->AddressOfEntryPoint = (uint)value;
			}
		}

		/// <summary />
		public ulong ImageBase {
			get {
				if (_is64Bit)
					return RawValue64->ImageBase;
				else
					return RawValue32->ImageBase;
			}
			set {
				if (_is64Bit)
					RawValue64->ImageBase = value;
				else
					RawValue32->ImageBase = (uint)value;
			}
		}

		/// <summary />
		public uint SectionAlignment {
			get {
				if (_is64Bit)
					return RawValue64->SectionAlignment;
				else
					return RawValue32->SectionAlignment;
			}
			set {
				if (_is64Bit)
					RawValue64->SectionAlignment = value;
				else
					RawValue32->SectionAlignment = value;
			}
		}

		/// <summary />
		public uint FileAlignment {
			get {
				if (_is64Bit)
					return RawValue64->FileAlignment;
				else
					return RawValue32->FileAlignment;
			}
			set {
				if (_is64Bit)
					RawValue64->FileAlignment = value;
				else
					RawValue32->FileAlignment = value;
			}
		}

		/// <summary />
		public uint ImageSize {
			get {
				if (_is64Bit)
					return RawValue64->SizeOfImage;
				else
					return RawValue32->SizeOfImage;
			}
			set {
				if (_is64Bit)
					RawValue64->SizeOfImage = value;
				else
					RawValue32->SizeOfImage = value;
			}
		}

		/// <summary />
		public uint HeadersSize {
			get {
				if (_is64Bit)
					return RawValue64->SizeOfHeaders;
				else
					return RawValue32->SizeOfHeaders;
			}
			set {
				if (_is64Bit)
					RawValue64->SizeOfHeaders = value;
				else
					RawValue32->SizeOfHeaders = value;
			}
		}

		/// <summary />
		public SubsystemType SubsystemType {
			get {
				if (_is64Bit)
					return (SubsystemType)RawValue64->Subsystem;
				else
					return (SubsystemType)RawValue32->Subsystem;
			}
			set {
				if (_is64Bit)
					RawValue64->Subsystem = (ushort)value;
				else
					RawValue32->Subsystem = (ushort)value;
			}
		}

		/// <summary />
		public ushort DllCharacteristicFlags {
			get {
				if (_is64Bit)
					return RawValue64->DllCharacteristics;
				else
					return RawValue32->DllCharacteristics;
			}
			set {
				if (_is64Bit)
					RawValue64->DllCharacteristics = value;
				else
					RawValue32->DllCharacteristics = value;
			}
		}

		/// <summary />
		public uint DataDirectoryCount {
			get {
				if (_is64Bit)
					return RawValue64->NumberOfRvaAndSizes;
				else
					return RawValue32->NumberOfRvaAndSizes;
			}
			set {
				if (_is64Bit)
					RawValue64->NumberOfRvaAndSizes = value;
				else
					RawValue32->NumberOfRvaAndSizes = value;
			}
		}

		/// <summary />
		public DataDirectory* DataDirectories => _is64Bit ? (DataDirectory*)RawValue64->DataDirectory : (DataDirectory*)RawValue32->DataDirectory;

		/// <summary>
		/// 导出表目录
		/// </summary>
		public DataDirectory* ExportTableDirectory => DataDirectories;

		/// <summary>
		/// 导入表目录
		/// </summary>
		public DataDirectory* ImportTableDirectory => DataDirectories + 1;

		/// <summary>
		/// 资源表目录
		/// </summary>
		public DataDirectory* ResourceTableDirectory => DataDirectories + 2;

		/// <summary>
		/// 重定位表目录
		/// </summary>
		public DataDirectory* RelocationTableDirectory => DataDirectories + 5;

		/// <summary>
		/// 调试信息目录
		/// </summary>
		public DataDirectory* DebugInfoDirectory => DataDirectories + 6;

		/// <summary>
		/// TLS表目录
		/// </summary>
		public DataDirectory* TlsTableDirectory => DataDirectories + 11;

		/// <summary>
		/// 导入地址表目录
		/// </summary>
		public DataDirectory* ImportAddressTableDirectory => DataDirectories + 12;

		/// <summary>
		/// .NET目录
		/// </summary>
		public DataDirectory* DotNetDirectory => DataDirectories + 14;

		internal OptionalHeader(NtHeader ntHeader) {
			if (ntHeader == null)
				throw new ArgumentNullException(nameof(ntHeader));

			_rawData = (byte*)ntHeader.RawData + 4 + IMAGE_FILE_HEADER.UnmanagedSize;
			_offset = (uint)ntHeader.FOA + 4 + IMAGE_FILE_HEADER.UnmanagedSize;
			switch (*(OptionalHeaderType*)_rawData) {
			case OptionalHeaderType.PE32:
				_is64Bit = false;
				break;
			case OptionalHeaderType.PE64:
				_is64Bit = true;
				break;
			default:
				throw new BadImageFormatException("Invalid IMAGE_OPTIONAL_HEADER.Magic");
			}
		}
	}
}
