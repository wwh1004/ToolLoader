using System;
using System.Diagnostics;
using Mdlib.PE;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据流
	/// </summary>
	[DebuggerDisplay("MdStm:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA} L:{Length}]")]
	public abstract unsafe class MetadataStream : IRawData {
		/// <summary />
		protected readonly IPEImage _peImage;
		/// <summary />
		protected readonly void* _rawData;
		/// <summary />
		protected readonly uint _offset;
		/// <summary />
		protected readonly uint _length;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public RVA RVA => _peImage.ToRVA((FOA)_offset);

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => _length;

		internal MetadataStream(IMetadata metadata, int index) : this(metadata, metadata.StreamHeaders[index]) {
		}

		internal MetadataStream(IMetadata metadata, StreamHeader header) {
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			if (header == null)
				throw new ArgumentNullException(nameof(header));

			_peImage = metadata.PEImage;
			_offset = (uint)metadata.StorageSignature.FOA + header.Offset;
			_rawData = (byte*)_peImage.RawData + _offset;
			_length = header.Size;
		}
	}
}
