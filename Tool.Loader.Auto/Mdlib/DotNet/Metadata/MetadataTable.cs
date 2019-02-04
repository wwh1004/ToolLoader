using System;
using System.Diagnostics;
using Mdlib.PE;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 元数据表
	/// </summary>
	[DebuggerDisplay("MdTbl:[P:{Utils.PointerToString(RawData)} FOA:{FOA} RS:{RowSize} RC:{RowCount} T:{Type}]")]
	[DebuggerTypeProxy(typeof(DebugView))]
	public sealed unsafe class MetadataTable : IRawData {
		private readonly TableStream _tableStream;
		private readonly void* _rawData;
		private readonly uint _offset;
		private readonly uint _length;
		private TableRow[] _rows;
		private readonly uint _rowSize;
		private readonly uint _rowCount;
		private readonly TableType _type;

		/// <summary />
		public IntPtr RawData => (IntPtr)_rawData;

		/// <summary />
		public RVA RVA => _tableStream.PEImage.ToRVA((FOA)_offset);

		/// <summary />
		public FOA FOA => (FOA)_offset;

		/// <summary>
		/// <see cref="Length"/> 返回整张表的大小，若要获取行数请使用 <see cref="RowCount"/>
		/// </summary>
		public uint Length => _length;

		/// <summary>
		/// 行
		/// </summary>
		public TableRow[] Rows {
			get {
				if (_rows == null) {
					_rows = new TableRow[_rowCount];
					for (uint i = 0; i < _rowCount; i++)
						_rows[i] = CreateRow(i);
				}
				return _rows;
			}
		}

		/// <summary>
		/// 行大小
		/// </summary>
		public uint RowSize => _rowSize;

		/// <summary>
		/// 行数
		/// </summary>
		public uint RowCount => _rowCount;

		/// <summary>
		/// 元数据表类型
		/// </summary>
		public TableType Type => _type;

		/// <summary />
		public bool IsBigString => _tableStream.IsBigString;

		/// <summary />
		public bool IsBigGuid => _tableStream.IsBigGuid;

		/// <summary />
		public bool IsBigBlob => _tableStream.IsBigBlob;

		internal MetadataTable(TableStream tableStream, ref uint lastTableOffset, uint rowCount, TableType type) {
			if (tableStream == null)
				throw new ArgumentNullException(nameof(tableStream));

			_tableStream = tableStream;
			_rawData = (byte*)_tableStream.RawData + lastTableOffset;
			_offset = lastTableOffset;
			_rowSize = CalculateRowSize(type, tableStream.IsBigString, tableStream.IsBigGuid, tableStream.IsBigBlob);
			_rowCount = rowCount;
			_length = _rowSize * rowCount;
			_type = type;
			lastTableOffset += _length;
		}

		private static uint CalculateRowSize(TableType type, bool isBigStrings, bool isBigGuid, bool isBigBlob) {
			switch (type) {
			case TableType.Module:
				// USHORT + STRING + GUID + GUID + GUID
				return 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigGuid) + ToOffsetSize(isBigGuid) + ToOffsetSize(isBigGuid);
			case TableType.TypeRef:
				// CODED_TOKEN + STRING + STRING
				return 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigStrings);
			case TableType.TypeDef:
				// ULONG + STRING + STRING + CODED_TOKEN + RID + RID
				return 4 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigStrings) + 2 + 2 + 2;
			case TableType.FieldPtr:
				// RID
				return 2;
			case TableType.Field:
				// USHORT + STRING + BLOB
				return 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob);
			case TableType.MethodPtr:
				// RID
				return 2;
			case TableType.Method:
				// ULONG + USHORT + USHORT + STRING + BLOB + RID
				return 4 + 2 + 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob) + 2;
			case TableType.ParamPtr:
				// RID
				return 2;
			case TableType.Param:
				// USHORT + USHORT + STRING
				return 2 + 2 + ToOffsetSize(isBigStrings);
			case TableType.InterfaceImpl:
				// RID + CODED_TOKEN
				return 2 + 2;
			case TableType.MemberRef:
				// CODED_TOKEN + STRING + BLOB
				return 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob);
			case TableType.Constant:
				// BYTE + BYTE + CODED_TOKEN + BLOB
				return 1 + 1 + 2 + ToOffsetSize(isBigBlob);
			case TableType.CustomAttribute:
				// CODED_TOKEN + CODED_TOKEN + BLOB
				return 2 + 2 + ToOffsetSize(isBigBlob);
			case TableType.FieldMarshal:
				// CODED_TOKEN + BLOB
				return 2 + ToOffsetSize(isBigBlob);
			case TableType.DeclSecurity:
				// SHORT + CODED_TOKEN + BLOB
				return 2 + 2 + ToOffsetSize(isBigBlob);
			case TableType.ClassLayout:
				// USHORT + ULONG + RID
				return 2 + 4 + 2;
			case TableType.FieldLayout:
				// ULONG + RID
				return 4 + 2;
			case TableType.StandAloneSig:
				// BLOB
				return ToOffsetSize(isBigBlob);
			case TableType.EventMap:
				// RID + RID
				return 2 + 2;
			case TableType.EventPtr:
				// RID
				return 2;
			case TableType.Event:
				// USHORT + STRING + CODED_TOKEN
				return 2 + ToOffsetSize(isBigStrings) + 2;
			case TableType.PropertyMap:
				// RID + RID
				return 2 + 2;
			case TableType.PropertyPtr:
				// RID
				return 2;
			case TableType.Property:
				// USHORT + STRING + BLOB
				return 2 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob);
			case TableType.MethodSemantics:
				// USHORT + RID + CODED_TOKEN
				return 2 + 2 + 2;
			case TableType.MethodImpl:
				// RID + CODED_TOKEN + CODED_TOKEN
				return 2 + 2 + 2;
			case TableType.ModuleRef:
				// STRING
				return ToOffsetSize(isBigStrings);
			case TableType.TypeSpec:
				// BLOB
				return ToOffsetSize(isBigBlob);
			case TableType.ImplMap:
				// USHORT + CODED_TOKEN + STRING + RID
				return 2 + 2 + ToOffsetSize(isBigStrings) + 2;
			case TableType.FieldRVA:
				// ULONG + RID
				return 4 + 2;
			case TableType.ENCLog:
				// ULONG + ULONG
				return 4 + 4;
			case TableType.ENCMap:
				// ULONG
				return 4;
			case TableType.Assembly:
				// ULONG + USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING + STRING
				return 4 + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(isBigBlob) + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigStrings);
			case TableType.AssemblyProcessor:
				// ULONG
				return 4;
			case TableType.AssemblyOS:
				// ULONG + ULONG + ULONG
				return 4 + 4 + 4;
			case TableType.AssemblyRef:
				// USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING + STRING + BLOB
				return 2 + 2 + 2 + 2 + 4 + ToOffsetSize(isBigBlob) + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob);
			case TableType.AssemblyRefProcessor:
				// ULONG + RID
				return 4 + 2;
			case TableType.AssemblyRefOS:
				// ULONG + ULONG + ULONG + RID
				return 4 + 4 + 4 + 2;
			case TableType.File:
				// ULONG + STRING + BLOB
				return 4 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigBlob);
			case TableType.ExportedType:
				// ULONG + ULONG + STRING + STRING + CODED_TOKEN
				return 4 + 4 + ToOffsetSize(isBigStrings) + ToOffsetSize(isBigStrings) + 2;
			case TableType.ManifestResource:
				// ULONG + ULONG + STRING + CODED_TOKEN
				return 4 + 4 + ToOffsetSize(isBigStrings) + 2;
			case TableType.NestedClass:
				// RID + RID
				return 2 + 2;
			case TableType.GenericParam:
				// USHORT + USHORT + CODED_TOKEN + STRING
				return 2 + 2 + 2 + ToOffsetSize(isBigStrings);
			case TableType.MethodSpec:
				// CODED_TOKEN + BLOB
				return 2 + ToOffsetSize(isBigBlob);
			case TableType.GenericParamConstraint:
				// RID + CODED_TOKEN
				return 2 + 2;
			default:
				throw new ArgumentOutOfRangeException(nameof(type));
			}
		}

		private static uint ToOffsetSize(bool flag) => flag ? 4u : 2;

		private TableRow CreateRow(uint index) {
			// Generated by Mdlib.Generator
			switch (_type) {
			case TableType.Module:
				return new ModuleRow(this, index);
			case TableType.TypeRef:
				return new TypeRefRow(this, index);
			case TableType.TypeDef:
				return new TypeDefRow(this, index);
			case TableType.FieldPtr:
				return new FieldPtrRow(this, index);
			case TableType.Field:
				return new FieldRow(this, index);
			case TableType.MethodPtr:
				return new MethodPtrRow(this, index);
			case TableType.Method:
				return new MethodRow(this, index);
			case TableType.ParamPtr:
				return new ParamPtrRow(this, index);
			case TableType.Param:
				return new ParamRow(this, index);
			case TableType.InterfaceImpl:
				return new InterfaceImplRow(this, index);
			case TableType.MemberRef:
				return new MemberRefRow(this, index);
			case TableType.Constant:
				return new ConstantRow(this, index);
			case TableType.CustomAttribute:
				return new CustomAttributeRow(this, index);
			case TableType.FieldMarshal:
				return new FieldMarshalRow(this, index);
			case TableType.DeclSecurity:
				return new DeclSecurityRow(this, index);
			case TableType.ClassLayout:
				return new ClassLayoutRow(this, index);
			case TableType.FieldLayout:
				return new FieldLayoutRow(this, index);
			case TableType.StandAloneSig:
				return new StandAloneSigRow(this, index);
			case TableType.EventMap:
				return new EventMapRow(this, index);
			case TableType.EventPtr:
				return new EventPtrRow(this, index);
			case TableType.Event:
				return new EventRow(this, index);
			case TableType.PropertyMap:
				return new PropertyMapRow(this, index);
			case TableType.PropertyPtr:
				return new PropertyPtrRow(this, index);
			case TableType.Property:
				return new PropertyRow(this, index);
			case TableType.MethodSemantics:
				return new MethodSemanticsRow(this, index);
			case TableType.MethodImpl:
				return new MethodImplRow(this, index);
			case TableType.ModuleRef:
				return new ModuleRefRow(this, index);
			case TableType.TypeSpec:
				return new TypeSpecRow(this, index);
			case TableType.ImplMap:
				return new ImplMapRow(this, index);
			case TableType.FieldRVA:
				return new FieldRVARow(this, index);
			case TableType.ENCLog:
				return new ENCLogRow(this, index);
			case TableType.ENCMap:
				return new ENCMapRow(this, index);
			case TableType.Assembly:
				return new AssemblyRow(this, index);
			case TableType.AssemblyProcessor:
				return new AssemblyProcessorRow(this, index);
			case TableType.AssemblyOS:
				return new AssemblyOSRow(this, index);
			case TableType.AssemblyRef:
				return new AssemblyRefRow(this, index);
			case TableType.AssemblyRefProcessor:
				return new AssemblyRefProcessorRow(this, index);
			case TableType.AssemblyRefOS:
				return new AssemblyRefOSRow(this, index);
			case TableType.File:
				return new FileRow(this, index);
			case TableType.ExportedType:
				return new ExportedTypeRow(this, index);
			case TableType.ManifestResource:
				return new ManifestResourceRow(this, index);
			case TableType.NestedClass:
				return new NestedClassRow(this, index);
			case TableType.GenericParam:
				return new GenericParamRow(this, index);
			case TableType.MethodSpec:
				return new MethodSpecRow(this, index);
			case TableType.GenericParamConstraint:
				return new GenericParamConstraintRow(this, index);
			default:
				throw new ArgumentOutOfRangeException(nameof(Type));
			}
		}

		private sealed class DebugView {
			private readonly TableRow[] _rows;

			public DebugView(MetadataTable table) {
				if (table == null)
					throw new ArgumentNullException(nameof(table));

				_rows = table.Rows;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public TableRow[] Rows => _rows;
		}
	}
}
