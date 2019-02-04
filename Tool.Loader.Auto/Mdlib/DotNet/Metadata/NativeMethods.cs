#pragma warning disable CS1591
using System.Runtime.InteropServices;
using static Mdlib.NativeMethods;

namespace Mdlib.DotNet.Metadata {
	public static unsafe class NativeMethods {
		public const ushort MAXSTREAMNAME = 32;
		public const byte TBL_COUNT = 45;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_COR20_HEADER {
			public static readonly uint UnmanagedSize = (uint)sizeof(IMAGE_COR20_HEADER);

			public uint cb;
			public ushort MajorRuntimeVersion;
			public ushort MinorRuntimeVersion;
			public IMAGE_DATA_DIRECTORY MetaData;
			public uint Flags;
			public uint EntryPointTokenOrRVA;
			public IMAGE_DATA_DIRECTORY Resources;
			public IMAGE_DATA_DIRECTORY StrongNameSignature;
			public IMAGE_DATA_DIRECTORY CodeManagerTable;
			public IMAGE_DATA_DIRECTORY VTableFixups;
			public IMAGE_DATA_DIRECTORY ExportAddressTableJumps;
			public IMAGE_DATA_DIRECTORY ManagedNativeHeader;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct STORAGESIGNATURE {
			/// <summary>
			/// 大小不包括pVersion的长度
			/// </summary>
			public static readonly uint UnmanagedSize = (uint)sizeof(STORAGESIGNATURE) - 1;

			public uint lSignature;
			public ushort iMajorVer;
			public ushort iMinorVer;
			public uint iExtraData;
			public uint iVersionString;
			/// <summary>
			/// 由于C#语法问题不能写pVersion[0]，实际长度由 <see cref="iVersionString"/> 决定
			/// </summary>
			public fixed byte pVersion[1];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct STORAGEHEADER {
			public static readonly uint UnmanagedSize = (uint)sizeof(STORAGEHEADER);

			public byte fFlags;
			public byte pad;
			public ushort iStreams;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct STORAGESTREAM {
			/// <summary>
			/// 大小不包括rcName的长度
			/// </summary>
			public static readonly uint UnmanagedSize = (uint)sizeof(STORAGESTREAM) - 4;

			public uint iOffset;
			public uint iSize;
			/// <summary>
			/// 对齐到4字节边界，最大长度为 <see cref="MAXSTREAMNAME"/>
			/// </summary>
			public fixed byte rcName[4];
		}
	}
}
#pragma warning restore CS1591
