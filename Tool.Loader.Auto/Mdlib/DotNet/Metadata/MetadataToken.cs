using System;
using System.Diagnostics;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据标记
	/// </summary>
	[DebuggerDisplay("MDToken:{ToString()} ({Type})")]
	public struct MetadataToken : IEquatable<MetadataToken> {
		private readonly uint _value;

		/// <summary>
		/// 原始值
		/// </summary>
		public uint Value => _value;

		/// <summary>
		/// 元数据表类型
		/// </summary>
		public TableType Type => (TableType)((_value & 0xFF000000) >> 24);

		/// <summary>
		/// RID
		/// </summary>
		public uint RowId => _value & 0x00FFFFFF;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="value"></param>
		public MetadataToken(int value) => _value = (uint)value;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="value"></param>
		public MetadataToken(uint value) => _value = value;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="type"></param>
		/// <param name="rowId"></param>
		public MetadataToken(TableType type, int rowId) => _value = (uint)type << 24 | (uint)rowId;

		/// <summary>
		/// 构造器
		/// </summary>
		/// <param name="type"></param>
		/// <param name="rowId"></param>
		public MetadataToken(TableType type, uint rowId) => _value = (uint)type << 24 | rowId;

		/// <summary />
		public static explicit operator int(MetadataToken value) => (int)value._value;

		/// <summary />
		public static explicit operator uint(MetadataToken value) => value._value;

		/// <summary />
		public static explicit operator MetadataToken(int value) => new MetadataToken(value);

		/// <summary />
		public static explicit operator MetadataToken(uint value) => new MetadataToken(value);

		/// <summary />
		public static bool operator ==(MetadataToken left, MetadataToken right) => left._value == right._value;

		/// <summary />
		public static bool operator !=(MetadataToken left, MetadataToken right) => left._value != right._value;

		/// <summary />
		public bool Equals(MetadataToken other) => _value == other._value;

		/// <summary />
		public override bool Equals(object obj) => obj is MetadataToken ? _value == ((MetadataToken)obj)._value : false;

		/// <summary />
		public override int GetHashCode() => (int)_value;

		/// <summary />
		public override string ToString() => "0x" + _value.ToString("X8");
	}
}
