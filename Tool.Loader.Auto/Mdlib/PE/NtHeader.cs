using System;
using System.Diagnostics;
using static Mdlib.NativeMethods;

namespace Mdlib.PE {
	/// <summary>
	/// Nt头
	/// </summary>
	[DebuggerDisplay("NtHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA}]")]
	public sealed unsafe class NtHeader : IRawData {
		private readonly void* _rawData;
		private readonly uint _offset;
		private readonly FileHeader _fileHeader;
		private readonly OptionalHeader _optionalHeader;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_NT_HEADERS32* RawValue32 => Is64Bit ? throw new InvalidOperationException("It's PE32 format.") : (IMAGE_NT_HEADERS32*)_rawData;

		/// <summary />
		public IMAGE_NT_HEADERS64* RawValue64 => Is64Bit ? (IMAGE_NT_HEADERS64*)_rawData : throw new InvalidOperationException("It's PE32+ format.");

		/// <summary />
		public RVA RVA => (RVA)_offset;

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => 4 + IMAGE_FILE_HEADER.UnmanagedSize + _fileHeader.OptionalHeaderSize;

		/// <summary />
		public uint Signature {
			get => *(uint*)_rawData;
			set => *(uint*)_rawData = value;
		}

		/// <summary />
		public FileHeader FileHeader => _fileHeader;

		/// <summary />
		public OptionalHeader OptionalHeader => _optionalHeader;

		/// <summary />
		public bool Is64Bit => _optionalHeader.Is64Bit;

		internal NtHeader(IPEImage peImage) {
			if (peImage == null)
				throw new ArgumentNullException(nameof(peImage));

			_offset = peImage.DosHeader.NtHeaderOffset;
			_rawData = (byte*)peImage.RawData + _offset;
			_fileHeader = new FileHeader(this);
			_optionalHeader = new OptionalHeader(this);
		}
	}
}
