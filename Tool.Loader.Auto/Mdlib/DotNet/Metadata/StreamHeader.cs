using System;
using System.Diagnostics;
using System.Text;
using Mdlib.PE;
using static Mdlib.DotNet.Metadata.NativeMethods;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据流头
	/// </summary>
	[DebuggerDisplay("StmHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA} N:{DisplayName}]")]
	public sealed unsafe class StreamHeader : IRawData<STORAGESTREAM> {
		private readonly IPEImage _peImage;
		private readonly void* _rawData;
		private readonly uint _offset;
		private string _displayName;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public STORAGESTREAM* RawValue => (STORAGESTREAM*)_rawData;

		/// <summary />
		public RVA RVA => _peImage.ToRVA((FOA)_offset);

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => STORAGESTREAM.UnmanagedSize + NameLength;

		/// <summary />
		public uint Offset {
			get => RawValue->iOffset;
			set => RawValue->iOffset = value;
		}

		/// <summary />
		public uint Size {
			get => RawValue->iSize;
			set => RawValue->iSize = value;
		}

		/// <summary />
		public byte* Name => RawValue->rcName;

		/// <summary>
		/// 名称长度，包括0终止符
		/// </summary>
		public uint NameLength {
			get {
				for (uint i = 0; i < MAXSTREAMNAME; i++)
					if (RawValue->rcName[i] == 0)
						return (i & ~3u) + 4;
				return 32;
			}
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

		internal StreamHeader(IMetadata metadata, uint index) {
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			_peImage = metadata.PEImage;
			if (index == 0)
				_offset = (uint)metadata.StorageHeader.FOA + metadata.StorageHeader.Length;
			else
				_offset = (uint)metadata.StreamHeaders[index - 1].FOA + metadata.StreamHeaders[index - 1].Length;
			_rawData = (byte*)_peImage.RawData + _offset;
		}

		/// <summary>
		/// 刷新内部字符串缓存，这些缓存被Display*属性使用。若更改了字符串相关内容，请调用此方法。
		/// </summary>
		public void RefreshCache() {
			StringBuilder builder;

			builder = new StringBuilder(MAXSTREAMNAME);
			for (uint i = 0; i < MAXSTREAMNAME; i++) {
				if (RawValue->rcName[i] == 0)
					break;
				builder.Append((char)RawValue->rcName[i]);
			}
			_displayName = builder.ToString();
		}
	}
}
