namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// #Strings堆
	/// </summary>
	public sealed class StringHeap : MetadataStream {
		internal StringHeap(IMetadata metadata, int index) : base(metadata, index) {
		}
	}

	/// <summary>
	/// #US堆
	/// </summary>
	public sealed class UserStringHeap : MetadataStream {
		internal UserStringHeap(IMetadata metadata, int index) : base(metadata, index) {
		}
	}

	/// <summary>
	/// #GUID堆
	/// </summary>
	public sealed class GuidHeap : MetadataStream {
		internal GuidHeap(IMetadata metadata, int index) : base(metadata, index) {
		}
	}

	/// <summary>
	/// #Blob堆
	/// </summary>
	public sealed class BlobHeap : MetadataStream {
		internal BlobHeap(IMetadata metadata, int index) : base(metadata, index) {
		}
	}

	/// <summary>
	/// 未知元数据堆
	/// </summary>
	public sealed class UnknownHeap : MetadataStream {
		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="metadata">元数据</param>
		/// <param name="index">堆的索引</param>
		public UnknownHeap(IMetadata metadata, int index) : base(metadata, index) {
		}

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="metadata">元数据</param>
		/// <param name="header">元数据流头</param>
		public UnknownHeap(IMetadata metadata, StreamHeader header) : base(metadata, header) {
		}
	}
}
