using System;
using System.Diagnostics;
using System.Text;
using static Mdlib.NativeMethods;

namespace Mdlib.PE {
	/// <summary>
	/// 节头
	/// </summary>
	[DebuggerDisplay("SectHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA} N:{DisplayName}]")]
	public sealed unsafe class SectionHeader : IRawData<IMAGE_SECTION_HEADER> {
		private readonly void* _rawData;
		private readonly uint _offset;
		private string _displayName;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_SECTION_HEADER* RawValue => (IMAGE_SECTION_HEADER*)_rawData;

		/// <summary />
		public RVA RVA => (RVA)_offset;

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => IMAGE_SECTION_HEADER.UnmanagedSize;

		/// <summary />
		public byte* Name => RawValue->Name;

		/// <summary />
		public uint VirtualSize {
			get => RawValue->VirtualSize;
			set => RawValue->VirtualSize = value;
		}

		/// <summary />
		public RVA VirtualAddress {
			get => (RVA)RawValue->VirtualAddress;
			set => RawValue->VirtualAddress = (uint)value;
		}

		/// <summary />
		public uint RawSize {
			get => RawValue->SizeOfRawData;
			set => RawValue->SizeOfRawData = value;
		}

		/// <summary />
		public FOA RawAddress {
			get => (FOA)RawValue->PointerToRawData;
			set => RawValue->PointerToRawData = (uint)value;
		}

		/// <summary />
		public uint Characteristics {
			get => RawValue->Characteristics;
			set => RawValue->Characteristics = value;
		}

		/// <summary />
		public string DisplayName {
			get {
				if (_displayName != null)
					return _displayName;

				RefreshCache();
				return _displayName;
			}
		}

		internal SectionHeader(IPEImage peImage, uint index) {
			if (peImage == null)
				throw new ArgumentNullException(nameof(peImage));

			_offset = (uint)peImage.FileHeader.FOA + IMAGE_FILE_HEADER.UnmanagedSize + peImage.FileHeader.OptionalHeaderSize + index * IMAGE_SECTION_HEADER.UnmanagedSize;
			_rawData = (byte*)peImage.RawData + _offset;
		}

		/// <summary>
		/// 刷新内部字符串缓存，这些缓存被Display*属性使用。若更改了字符串相关内容，请调用此方法。
		/// </summary>
		public void RefreshCache() {
			byte* pName;
			StringBuilder builder;

			pName = (byte*)_rawData;
			builder = new StringBuilder(IMAGE_SIZEOF_SHORT_NAME);
			for (ushort i = 0; i < IMAGE_SIZEOF_SHORT_NAME; i++) {
				if (pName[i] == 0)
					break;
				builder.Append((char)pName[i]);
			}
			_displayName = builder.ToString();
		}
	}
}
