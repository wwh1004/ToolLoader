using System;
using Mdlib.PE;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据
	/// </summary>
	public interface IMetadata {
		/// <summary>
		/// 当前元数据所属的PE映像
		/// </summary>
		IPEImage PEImage { get; }

		/// <summary>
		/// Cor20头
		/// </summary>
		Cor20Header Cor20Header { get; }

		/// <summary>
		/// 存储签名
		/// </summary>
		StorageSignature StorageSignature { get; }

		/// <summary>
		/// 存储头
		/// </summary>
		StorageHeader StorageHeader { get; }

		/// <summary>
		/// 流头
		/// </summary>
		StreamHeader[] StreamHeaders { get; }

		/// <summary>
		/// 元数据表流#~或#-
		/// </summary>
		TableStream TableStream { get; }

		/// <summary>
		/// Strings堆
		/// </summary>
		StringHeap StringHeap { get; }

		/// <summary>
		/// US堆
		/// </summary>
		UserStringHeap UserStringHeap { get; }

		/// <summary>
		/// GUID堆
		/// </summary>
		GuidHeap GuidHeap { get; }

		/// <summary>
		/// Blob堆
		/// </summary>
		BlobHeap BlobHeap { get; }
	}

	internal sealed class Metadata : IMetadata {
		private readonly IPEImage _peImage;
		private readonly Cor20Header _cor20Header;
		private readonly StorageSignature _storageSignature;
		private readonly StorageHeader _storageHeader;
		private readonly StreamHeader[] _streamHeaders;
		private readonly TableStream _tableStream;
		private readonly StringHeap _stringHeap;
		private readonly UserStringHeap _userStringHeap;
		private readonly GuidHeap _guidHeap;
		private readonly BlobHeap _blobHeap;

		public IPEImage PEImage => _peImage;

		public Cor20Header Cor20Header => _cor20Header;

		public StorageSignature StorageSignature => _storageSignature;

		public StorageHeader StorageHeader => _storageHeader;

		public StreamHeader[] StreamHeaders => _streamHeaders;

		public TableStream TableStream => _tableStream;

		public StringHeap StringHeap => _stringHeap;

		public UserStringHeap UserStringHeap => _userStringHeap;

		public GuidHeap GuidHeap => _guidHeap;

		public BlobHeap BlobHeap => _blobHeap;

		public Metadata(IPEImage peImage) {
			if (peImage == null)
				throw new ArgumentNullException(nameof(peImage));
			if (!peImage.IsDotNetImage)
				throw new InvalidOperationException();

			bool? isCompressed;

			_peImage = peImage;
			_cor20Header = new Cor20Header(this);
			_storageSignature = new StorageSignature(this);
			_storageHeader = new StorageHeader(this);
			_streamHeaders = new StreamHeader[_storageHeader.StreamsCount];
			for (int i = 0; i < _streamHeaders.Length; i++)
				_streamHeaders[i] = new StreamHeader(this, (uint)i);
			isCompressed = null;
			foreach (StreamHeader header in _streamHeaders) {
				string name;

				name = header.DisplayName;
				if (isCompressed == null)
					if (name == "#~")
						isCompressed = true;
					else if (name == "#-")
						isCompressed = false;
				if (name == "#Schema")
					isCompressed = false;
			}
			if (isCompressed == null)
				throw new BadImageFormatException("Metadata table (#~ / #-) not found");
			if (isCompressed.Value)
				for (int i = _streamHeaders.Length - 1; i >= 0; i--)
					switch (_streamHeaders[i].DisplayName) {
					case "#~":
						if (_tableStream == null)
							_tableStream = new TableStream(this, i, true);
						break;
					case "#Strings":
						if (_stringHeap == null)
							_stringHeap = new StringHeap(this, i);
						break;
					case "#US":
						if (_userStringHeap == null)
							_userStringHeap = new UserStringHeap(this, i);
						break;
					case "#GUID":
						if (_guidHeap == null)
							_guidHeap = new GuidHeap(this, i);
						break;
					case "#Blob":
						if (_blobHeap == null)
							_blobHeap = new BlobHeap(this, i);
						break;
					}
			else
				for (int i = 0; i < _streamHeaders.Length; i++)
					switch (_streamHeaders[i].DisplayName.ToUpperInvariant()) {
					case "#~":
					case "#-":
						if (_tableStream == null)
							_tableStream = new TableStream(this, i, false);
						break;
					case "#STRINGS":
						if (_stringHeap == null)
							_stringHeap = new StringHeap(this, i);
						break;
					case "#US":
						if (_userStringHeap == null)
							_userStringHeap = new UserStringHeap(this, i);
						break;
					case "#GUID":
						if (_guidHeap == null)
							_guidHeap = new GuidHeap(this, i);
						break;
					case "#BLOB":
						if (_blobHeap == null)
							_blobHeap = new BlobHeap(this, i);
						break;
					}
		}
	}
}
