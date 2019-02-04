using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Mdlib.DotNet.Metadata;

namespace Mdlib.PE {
	/// <summary>
	/// PE映像布局方式
	/// </summary>
	public enum PEImageLayout {
		/// <summary>
		/// 文件
		/// </summary>
		File,

		/// <summary>
		/// 内存
		/// </summary>
		Memory
	}

	/// <summary>
	/// PE映像接口
	/// </summary>
	public interface IPEImage : IDisposable {
		/// <summary>
		/// 当前PE映像的原始数据
		/// </summary>
		IntPtr RawData { get; }

		/// <summary>
		/// 当前PE映像的长度
		/// </summary>
		uint Length { get; }

		/// <summary>
		/// 是否为64位PE头
		/// </summary>
		bool Is64Bit { get; }

		/// <summary>
		/// 是否为.NET程序集
		/// </summary>
		bool IsDotNetImage { get; }

		/// <summary>
		/// PE映像布局方式
		/// </summary>
		PEImageLayout Layout { get; }

		/// <summary>
		/// Dos头
		/// </summary>
		DosHeader DosHeader { get; }

		/// <summary>
		/// Nt头
		/// </summary>
		NtHeader NtHeader { get; }

		/// <summary>
		/// 文件头
		/// </summary>
		FileHeader FileHeader { get; }

		/// <summary>
		/// 可选头
		/// </summary>
		OptionalHeader OptionalHeader { get; }

		/// <summary>
		/// 节头
		/// </summary>
		SectionHeader[] SectionHeaders { get; }

		/// <summary>
		/// 元数据
		/// </summary>
		/// <exception cref="InvalidOperationException">非.NET程序集时引发</exception>
		IMetadata Metadata { get; }

		/// <summary>
		/// FOA => RVA
		/// </summary>
		/// <param name="foa">FOA</param>
		/// <returns></returns>
		RVA ToRVA(FOA foa);

		/// <summary>
		/// RVA => FOA
		/// </summary>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		FOA ToFOA(RVA rva);
	}

	/// <summary>
	/// PE映像工厂类
	/// </summary>
	public static unsafe class PEImageFactory {
		/// <summary>
		/// 创建 <see cref="IPEImage"/> 实例，使用文件布局
		/// </summary>
		/// <param name="filePath">PE映像文件路径</param>
		/// <returns></returns>
		public static IPEImage Create(string filePath) => Create(File.ReadAllBytes(filePath));

		/// <summary>
		/// 创建 <see cref="IPEImage"/> 实例，使用文件布局
		/// </summary>
		/// <param name="peImage">PE映像数据</param>
		/// <returns></returns>
		public static IPEImage Create(Stream peImage) => Create(ReadStreamAllBytes(peImage));

		/// <summary>
		/// 创建 <see cref="IPEImage"/> 实例，使用文件布局
		/// </summary>
		/// <param name="peImage">PE映像数据</param>
		/// <returns></returns>
		public static IPEImage Create(byte[] peImage) => new FilePEImage(peImage);

		/// <summary>
		/// 创建 <see cref="IPEImage"/> 实例，使用内存布局
		/// </summary>
		/// <param name="pPEImage">PE映像地址</param>
		/// <returns></returns>
		public static IPEImage Create(IntPtr pPEImage) => Create((void*)pPEImage);

		/// <summary>
		/// 创建 <see cref="IPEImage"/> 实例，使用内存布局
		/// </summary>
		/// <param name="pPEImage">PE映像地址</param>
		/// <returns></returns>
		public static IPEImage Create(void* pPEImage) => new MemoryPEImage(pPEImage);

		private static byte[] ReadStreamAllBytes(Stream stream) {
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			int length;

			try {
				length = (int)stream.Length;
			}
			catch {
				length = -1;
			}
			if (length == -1) {
				byte[] buffer;
				List<byte> byteList;
				int count;

				buffer = new byte[0x1000];
				byteList = new List<byte>();
				for (int i = 0; i < int.MaxValue; i++) {
					count = stream.Read(buffer, 0, buffer.Length);
					if (count == 0x1000)
						byteList.AddRange(buffer);
					else if (count == 0)
						return byteList.ToArray();
					else
						for (int j = 0; j < count; j++)
							byteList.Add(buffer[j]);
				}
				return byteList.ToArray();
			}
			else {
				byte[] buffer;

				buffer = new byte[length];
				stream.Read(buffer, 0, length);
				return buffer;
			}
		}
	}

	[DebuggerDisplay("FilePEImage:[P:{Utils.PointerToString(RawData)} L:{Length}]")]
	internal sealed unsafe class FilePEImage : IPEImage {
		private readonly void* _rawData;
		private readonly uint _length;
		private readonly bool _isDotNetImage;
		private readonly DosHeader _dosHeader;
		private readonly NtHeader _ntHeader;
		private readonly SectionHeader[] _sectionHeaders;
		private IMetadata _metadata;
		private bool _isDisposed;

		public IntPtr RawData => (IntPtr)_rawData;

		public uint Length => _length;

		public bool Is64Bit => _ntHeader.Is64Bit;

		public bool IsDotNetImage => _isDotNetImage;

		public PEImageLayout Layout => PEImageLayout.File;

		public DosHeader DosHeader => _dosHeader;

		public NtHeader NtHeader => _ntHeader;

		public FileHeader FileHeader => _ntHeader.FileHeader;

		public OptionalHeader OptionalHeader => _ntHeader.OptionalHeader;

		public SectionHeader[] SectionHeaders => _sectionHeaders;

		public IMetadata Metadata {
			get {
				if (!_isDotNetImage)
					throw new InvalidOperationException();

				if (_metadata == null)
					_metadata = new Metadata(this);
				return _metadata;
			}
		}

		public FilePEImage(byte[] peImage) : this(PinByteArray(peImage), (uint)peImage.Length) {
		}

		internal FilePEImage(void* rawData, uint length) {
			_rawData = rawData;
			_length = length;
			_dosHeader = new DosHeader(this);
			_ntHeader = new NtHeader(this);
			_sectionHeaders = new SectionHeader[_ntHeader.FileHeader.SectionsCount];
			for (uint i = 0; i < _sectionHeaders.Length; i++)
				_sectionHeaders[i] = new SectionHeader(this, i);
			_isDotNetImage = _ntHeader.OptionalHeader.DotNetDirectory->Address != 0;
		}

		private static void* PinByteArray(byte[] array) {
			IntPtr pBuffer;

			pBuffer = Marshal.AllocHGlobal(array.Length);
			Marshal.Copy(array, 0, pBuffer, array.Length);
			return (void*)pBuffer;
		}

		public RVA ToRVA(FOA foa) {
			foreach (SectionHeader sectionHeader in _sectionHeaders)
				if (foa >= sectionHeader.RawAddress && foa < sectionHeader.RawAddress + sectionHeader.RawSize)
					return foa - sectionHeader.RawAddress + sectionHeader.VirtualAddress;
			return (RVA)foa;
		}

		public FOA ToFOA(RVA rva) {
			foreach (SectionHeader sectionHeader in _sectionHeaders)
				if (rva >= sectionHeader.VirtualAddress && rva < sectionHeader.VirtualAddress + Math.Max(sectionHeader.VirtualSize, sectionHeader.RawSize))
					return rva - sectionHeader.VirtualAddress + sectionHeader.RawAddress;
			return (FOA)rva;
		}

		public void Dispose() {
			if (_isDisposed)
				return;

			Marshal.FreeHGlobal((IntPtr)_rawData);
			_isDisposed = true;
		}
	}

	[DebuggerDisplay("MemoryPEImage:[P:{Utils.PointerToString(RawData)} L:{Length}]")]
	internal sealed unsafe class MemoryPEImage : IPEImage {
		private readonly void* _rawData;
		private readonly uint _length;
		private readonly bool _isDotNetImage;
		private readonly DosHeader _dosHeader;
		private readonly NtHeader _ntHeader;
		private readonly SectionHeader[] _sectionHeaders;
		private IMetadata _metadata;

		public IntPtr RawData => (IntPtr)_rawData;

		public uint Length => _length;

		public bool Is64Bit => _ntHeader.Is64Bit;

		public bool IsDotNetImage => _isDotNetImage;

		public PEImageLayout Layout => PEImageLayout.Memory;

		public DosHeader DosHeader => _dosHeader;

		public NtHeader NtHeader => _ntHeader;

		public FileHeader FileHeader => _ntHeader.FileHeader;

		public OptionalHeader OptionalHeader => _ntHeader.OptionalHeader;

		public SectionHeader[] SectionHeaders => _sectionHeaders;

		public IMetadata Metadata {
			get {
				if (!_isDotNetImage)
					throw new InvalidOperationException();

				if (_metadata == null)
					_metadata = new Metadata(this);
				return _metadata;
			}
		}

		internal MemoryPEImage(void* rawData) {
			_rawData = null;
			_length = 0;
			_isDotNetImage = false;
			_dosHeader = null;
			_ntHeader = null;
			_sectionHeaders = null;
			throw new NotImplementedException();
		}

		public RVA ToRVA(FOA foa) => (RVA)foa;

		public FOA ToFOA(RVA rva) => (FOA)rva;

		public void Dispose() {
			throw new NotImplementedException();
		}
	}
}
