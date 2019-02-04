using System;
using System.Diagnostics;
using Mdlib.PE;
using static Mdlib.DotNet.Metadata.NativeMethods;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// Cor20头
	/// </summary>
	[DebuggerDisplay("CorHdr:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA}]")]
	public unsafe sealed class Cor20Header : IRawData<IMAGE_COR20_HEADER> {
		private readonly IPEImage _peImage;
		private readonly void* _rawData;
		private readonly uint _offset;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public IMAGE_COR20_HEADER* RawValue => (IMAGE_COR20_HEADER*)_rawData;

		/// <summary />
		public RVA RVA => _peImage.ToRVA((FOA)_offset);

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => IMAGE_COR20_HEADER.UnmanagedSize;

		/// <summary />
		public uint Size {
			get => RawValue->cb;
			set => RawValue->cb = value;
		}

		/// <summary />
		public ushort MajorRuntimeVersion {
			get => RawValue->MajorRuntimeVersion;
			set => RawValue->MajorRuntimeVersion = value;
		}

		/// <summary />
		public ushort MinorRuntimeVersion {
			get => RawValue->MinorRuntimeVersion;
			set => RawValue->MinorRuntimeVersion = value;
		}

		/// <summary />
		public DataDirectory* MetadataDirectory => (DataDirectory*)&RawValue->MetaData;

		/// <summary />
		public ComImageFlags CorFlags {
			get => (ComImageFlags)RawValue->Flags;
			set => RawValue->Flags = (uint)value;
		}

		/// <summary>
		/// 托管入口点，若无托管入口点返回 <see langword="null"/>，设置本机入口点时将自动设置 <see cref="ComImageFlags.NativeEntryPoint"/> 位为 <see langword="true"/>
		/// </summary>
		public MetadataToken? EntryPointToken {
			get {
				if ((CorFlags & ComImageFlags.NativeEntryPoint) != 0)
					return null;

				return (MetadataToken)RawValue->EntryPointTokenOrRVA;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				CorFlags &= ~ComImageFlags.NativeEntryPoint;
				RawValue->EntryPointTokenOrRVA = (uint)value.Value;
			}
		}

		/// <summary>
		/// 本机入口点，若无本机入口点返回 <see langword="null"/>，设置本机入口点时将自动设置 <see cref="ComImageFlags.NativeEntryPoint"/> 位为 <see langword="true"/>
		/// </summary>
		public RVA? EntryPointRVA {
			get {
				if ((CorFlags & ComImageFlags.NativeEntryPoint) == 0)
					return null;

				return (RVA)RawValue->EntryPointTokenOrRVA;
			}
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				CorFlags |= ComImageFlags.NativeEntryPoint;
				RawValue->EntryPointTokenOrRVA = (uint)value.Value;
			}
		}

		/// <summary />
		public DataDirectory* ResourcesDirectory => (DataDirectory*)&RawValue->Resources;

		/// <summary />
		public DataDirectory* StrongNameSignatureDirectory => (DataDirectory*)&RawValue->StrongNameSignature;

		/// <summary />
		public DataDirectory* VTableFixupsDirectory => (DataDirectory*)&RawValue->VTableFixups;

		internal Cor20Header(IMetadata metadata) {
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			_peImage = metadata.PEImage;
			_offset = (uint)_peImage.ToFOA((RVA)_peImage.OptionalHeader.DotNetDirectory->Address);
			_rawData = (byte*)_peImage.RawData + _offset;
		}
	}
}
