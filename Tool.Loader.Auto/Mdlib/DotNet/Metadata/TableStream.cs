using Mdlib.PE;
using static Mdlib.DotNet.Metadata.NativeMethods;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据表流
	/// </summary>
	public sealed unsafe class TableStream : MetadataStream {
		private readonly bool _isCompressed;
		private readonly bool _isBigString;
		private readonly bool _isBigGuid;
		private readonly bool _isBigBlob;
		private readonly MetadataTable[] _tables;

		/// <summary>
		/// 元数据架构主版本
		/// </summary>
		public byte MajorVersion {
			get => *((byte*)_rawData + 4);
			set => *((byte*)_rawData + 4) = value;
		}

		/// <summary>
		/// 元数据架构次版本
		/// </summary>
		public byte MinorVersion {
			get => *((byte*)_rawData + 5);
			set => *((byte*)_rawData + 5) = value;
		}

		/// <summary>
		/// 堆二进制标志
		/// </summary>
		public HeapFlags Flags {
			get => *((HeapFlags*)_rawData + 6);
			set => *((HeapFlags*)_rawData + 6) = value;
		}

		/// <summary>
		/// 表示存在哪些表
		/// </summary>
		public ulong ValidMask {
			get => *(ulong*)((byte*)_rawData + 8);
			set => *(ulong*)((byte*)_rawData + 8) = value;
		}

		/// <summary>
		/// 表示哪些表被排序了
		/// </summary>
		public ulong SortedMask {
			get => *(ulong*)((byte*)_rawData + 16);
			set => *(ulong*)((byte*)_rawData + 16) = value;
		}

		/// <summary />
		public IPEImage PEImage => _peImage;

		/// <summary>
		/// 是否是压缩的元数据表
		/// </summary>
		public bool IsCompressed => _isCompressed;

		/// <summary />
		public bool IsBigString => _isBigString;

		/// <summary />
		public bool IsBigGuid => _isBigGuid;

		/// <summary />
		public bool IsBigBlob => _isBigBlob;

		/// <summary>
		/// 所有元数据表。不存在的表被设置为 <see langword="null"/>
		/// </summary>
		public MetadataTable[] Tables => _tables;

		internal TableStream(IMetadata metadata, int index, bool isCompressed) : base(metadata, index) {
			ulong validMask;
			uint[] rowCounts;
			uint tablesOffset;
			uint lastTableOffset;

			_isCompressed = isCompressed;
			_isBigString = (Flags & HeapFlags.BigString) != 0;
			_isBigGuid = (Flags & HeapFlags.BigGuid) != 0;
			_isBigBlob = (Flags & HeapFlags.BigBlob) != 0;
			_tables = new MetadataTable[TBL_COUNT];
			validMask = ValidMask;
			rowCounts = new uint[TBL_COUNT];
			tablesOffset = 24;
			// 到SortedMask为止有24个字节，再之后还有个长度为有效表长度的uint[]
			for (byte i = 0; i < TBL_COUNT; i++) {
				if ((validMask & 1ul << i) == 0)
					continue;
				rowCounts[i] = *(uint*)((byte*)_rawData + tablesOffset);
				tablesOffset += 4;
				// 算出所有表真正开始的偏移
			}
			lastTableOffset = _offset + tablesOffset;
			for (byte i = 0; i < TBL_COUNT; i++)
				if ((validMask & 1ul << i) != 0)
					_tables[i] = new MetadataTable(this, ref lastTableOffset, rowCounts[i], (TableType)i);
		}
	}
}
