//// dnlib\src\DotNet\MD\MDStreamFlags.cs modified

using System;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 堆二进制标志
	/// </summary>
	[Flags]
	public enum HeapFlags : byte {
		/// <summary>
		/// #Strings stream is big and requires 4 byte offsets
		/// </summary>
		BigString = 0x01,

		/// <summary>
		/// #GUID stream is big and requires 4 byte offsets
		/// </summary>
		BigGuid = 0x02,

		/// <summary>
		/// #Blob stream is big and requires 4 byte offsets
		/// </summary>
		BigBlob = 0x04,

		/// <summary />
		Padding = 0x08,

		/// <summary />
		DeltaOnly = 0x20,

		/// <summary>
		/// Extra data follows the row counts
		/// </summary>
		ExtraData = 0x40,

		/// <summary>
		/// Set if certain tables can contain deleted rows. The name column (if present) is set to "_Deleted"
		/// </summary>
		HasDelete = 0x80
	}
}
