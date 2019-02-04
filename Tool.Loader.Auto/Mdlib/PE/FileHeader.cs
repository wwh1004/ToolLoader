using System;
using System.Diagnostics;
using static Mdlib.NativeMethods;

namespace Mdlib.PE {
	/// <summary>
	/// 文件头
	/// </summary>
	[DebuggerDisplay("FileHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA} MT:{MachineType}]")]
	public sealed unsafe class FileHeader : IRawData<IMAGE_FILE_HEADER> {
		private readonly void* _rawData;
		private readonly uint _offset;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_FILE_HEADER* RawValue => (IMAGE_FILE_HEADER*)_rawData;

		/// <summary />
		public RVA RVA => (RVA)_offset;

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public MachineType MachineType {
			get => (MachineType)RawValue->Machine;
			set => RawValue->Machine = (ushort)value;
		}

		/// <summary />
		public ushort SectionsCount {
			get => RawValue->NumberOfSections;
			set => RawValue->NumberOfSections = value;
		}

		/// <summary />
		public uint TimeDateStamp {
			get => RawValue->TimeDateStamp;
			set => RawValue->TimeDateStamp = value;
		}

		/// <summary />
		public ushort OptionalHeaderSize {
			get => RawValue->SizeOfOptionalHeader;
			set => RawValue->SizeOfOptionalHeader = value;
		}

		/// <summary />
		public ushort CharacteristicFlags {
			get => RawValue->Characteristics;
			set => RawValue->Characteristics = value;
		}

		/// <summary />
		public uint Length => IMAGE_FILE_HEADER.UnmanagedSize;

		internal FileHeader(NtHeader ntHeader) {
			if (ntHeader == null)
				throw new ArgumentNullException(nameof(ntHeader));

			_rawData = (byte*)ntHeader.RawData + 4;
			_offset = (uint)ntHeader.FOA + 4;
		}
	}
}
