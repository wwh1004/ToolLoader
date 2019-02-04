using System;
using System.Diagnostics;
using static Mdlib.NativeMethods;

namespace Mdlib.PE {
	/// <summary>
	/// Dos头
	/// </summary>
	[DebuggerDisplay("DosHdr:[P:{Utils.PointerToString(RawData)} NTO:{NtHeaderOffset}]")]
	public sealed unsafe class DosHeader : IRawData<IMAGE_DOS_HEADER> {
		private readonly void* _rawData;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_DOS_HEADER* RawValue => (IMAGE_DOS_HEADER*)_rawData;

		/// <summary />
		public RVA RVA => 0;

		/// <summary />
		public FOA FOA => 0;

		/// <summary />
		public uint Length => IMAGE_DOS_HEADER.UnmanagedSize;

		/// <summary />
		public ushort MagicNumber {
			get => RawValue->e_magic;
			set => RawValue->e_magic = value;
		}

		/// <summary />
		public uint NtHeaderOffset {
			get => RawValue->e_lfanew;
			set => RawValue->e_lfanew = value;
		}

		internal DosHeader(IPEImage peImage) {
			if (peImage == null)
				throw new ArgumentNullException(nameof(peImage));

			_rawData = (byte*)peImage.RawData;
		}
	}
}
