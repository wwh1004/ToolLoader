using System;
using System.Diagnostics;
using System.Text;
using Mdlib.PE;
using static Mdlib.DotNet.Metadata.NativeMethods;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 存储签名
	/// </summary>
	[DebuggerDisplay("StgSig:[P:{Utils.PointerToString(RawData)} RVA:{RVA} FOA:{FOA} V:{DisplayVersionString}]")]
	public sealed unsafe class StorageSignature : IRawData<STORAGESIGNATURE> {
		private readonly IPEImage _peImage;
		private readonly void* _rawData;
		private readonly uint _offset;
		private string _displayVersionString;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public STORAGESIGNATURE* RawValue => (STORAGESIGNATURE*)_rawData;

		/// <summary />
		public RVA RVA => _peImage.ToRVA((FOA)_offset);

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary />
		public uint Length => STORAGESIGNATURE.UnmanagedSize + RawValue->iVersionString;

		/// <summary />
		public uint Signature {
			get => RawValue->lSignature;
			set => RawValue->lSignature = value;
		}

		/// <summary />
		public ushort MajorVersion {
			get => RawValue->iMajorVer;
			set => RawValue->iMajorVer = value;
		}

		/// <summary />
		public ushort MinorVersion {
			get => RawValue->iMinorVer;
			set => RawValue->iMinorVer = value;
		}

		/// <summary />
		public uint ExtraData {
			get => RawValue->iExtraData;
			set => RawValue->iExtraData = value;
		}

		/// <summary />
		public uint VersionStringLength {
			get => RawValue->iVersionString;
			set => RawValue->iVersionString = value;
		}

		/// <summary />
		public byte* VersionString => RawValue->pVersion;

		/// <summary />
		public string DisplayVersionString {
			get {
				if (_displayVersionString != null)
					return _displayVersionString;

				RefreshCache();
				return _displayVersionString;
			}
		}

		internal StorageSignature(IMetadata metadata) {
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));

			_peImage = metadata.PEImage;
			_offset = (uint)metadata.PEImage.ToFOA((RVA)metadata.Cor20Header.MetadataDirectory->Address);
			_rawData = (byte*)_peImage.RawData + _offset;
		}

		/// <summary>
		/// 刷新内部字符串缓存，这些缓存被Display*属性使用。若更改了字符串相关内容，请调用此方法。
		/// </summary>
		public void RefreshCache() {
			StringBuilder builder;

			builder = new StringBuilder((int)RawValue->iVersionString);
			for (uint i = 0; i < RawValue->iVersionString; i++) {
				if (RawValue->pVersion[i] == 0)
					break;
				builder.Append((char)RawValue->pVersion[i]);
			}
			_displayVersionString = builder.ToString();
		}
	}
}
