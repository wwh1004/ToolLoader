using System;

namespace Mdlib.PE {
	/// <summary>
	/// 原始数据泛型接口，提供访问结构化数据的能力
	/// </summary>
	public unsafe interface IRawData<T> : IRawData where T : unmanaged {
		/// <summary>
		/// 原始二进制数据对应的结构化数据
		/// </summary>
		T* RawValue { get; }
	}

	/// <summary>
	/// 原始数据接口
	/// </summary>
	public interface IRawData {
		/// <summary>
		/// 当前数据的原始数据
		/// </summary>
		IntPtr RawData { get; }

		/// <summary>
		/// 当前数据在PE映像中的RVA
		/// </summary>
		RVA RVA { get; }

		/// <summary>
		/// 当前数据在PE映像中的FOA
		/// </summary>
		FOA FOA { get; }

		/// <summary>
		/// 当前数据在PE映像中的长度
		/// </summary>
		uint Length { get; }
	}
}
