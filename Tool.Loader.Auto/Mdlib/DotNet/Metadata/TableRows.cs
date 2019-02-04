#pragma warning disable CS1591
using System;
using System.Diagnostics;
using Mdlib.PE;

namespace Mdlib.DotNet.Metadata {
	/// <summary>
	/// 表示元数据表中的一行
	/// </summary>
	[DebuggerDisplay("TblRow:[P:{Utils.PointerToString(RawData)} FOA:{FOA} MDToken:{MetadataToken}]")]
	public abstract unsafe class TableRow : IRawData {
		protected readonly MetadataTable _table;
		protected readonly uint _index;

		protected byte* RawDataUnsafe => (byte*)_table.RawData + _index * _table.RowSize;

		public IntPtr RawData => (IntPtr)RawDataUnsafe;

		public RVA RVA => _table.RVA + _index * _table.RowSize;

		public FOA FOA => _table.FOA + _index * _table.RowSize;

		public uint Length => _table.RowSize;

		public uint RowId => _index + 1;

		public MetadataToken MetadataToken => new MetadataToken(_table.Type, RowId);

		internal TableRow(MetadataTable table, uint index) {
			_table = table;
			_index = index;
		}

		protected static uint ToOffsetSize(bool flag) => flag ? 4u : 2;

		protected static byte ReadByte(void* address) => *(byte*)address;

		protected static short ReadInt16(void* address) => *(short*)address;

		protected static ushort ReadUInt16(void* address) => *(ushort*)address;

		protected static uint ReadUInt32(void* address) => *(uint*)address;

		protected static uint ReadHeapOffset(void* address, bool flag) => flag ? *(uint*)address : *(ushort*)address;
	}

	/// <summary>
	/// Module表中的一行
	/// </summary>
	public sealed unsafe class ModuleRow : TableRow {
		public ushort Generation => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + STRING

		public uint Mvid => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigGuid), _table.IsBigGuid);
		// USHORT + STRING + GUID

		public uint EncId => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigGuid) + ToOffsetSize(_table.IsBigGuid), _table.IsBigGuid);
		// USHORT + STRING + GUID + GUID

		public uint EncBaseId => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigGuid) + ToOffsetSize(_table.IsBigGuid) + ToOffsetSize(_table.IsBigGuid), _table.IsBigGuid);
		// USHORT + STRING + GUID + GUID + GUID

		internal ModuleRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// TypeRef表中的一行
	/// </summary>
	public sealed unsafe class TypeRefRow : TableRow {
		public uint ResolutionScope => ReadUInt16(RawDataUnsafe + 2);
		// CODED_TOKEN

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// CODED_TOKEN + STRING

		public uint Namespace => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// CODED_TOKEN + STRING + STRING

		internal TypeRefRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// TypeDef表中的一行
	/// </summary>
	public sealed unsafe class TypeDefRow : TableRow {
		public uint Flags => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint Name => ReadHeapOffset(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + STRING

		public uint Namespace => ReadHeapOffset(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + STRING + STRING

		public uint Extends => ReadUInt16(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString) + 2);
		// ULONG + STRING + STRING + CODED_TOKEN

		public uint FieldList => ReadUInt16(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString) + 2 + 2);
		// ULONG + STRING + STRING + CODED_TOKEN + RID

		public uint MethodList => ReadUInt16(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString) + 2 + 2 + 2);
		// ULONG + STRING + STRING + CODED_TOKEN + RID + RID

		internal TypeDefRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// FieldPtr表中的一行
	/// </summary>
	public sealed unsafe class FieldPtrRow : TableRow {
		public uint Field => ReadUInt16(RawDataUnsafe + 2);
		// RID

		internal FieldPtrRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Field表中的一行
	/// </summary>
	public sealed unsafe class FieldRow : TableRow {
		public ushort Flags => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + STRING

		public uint Signature => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// USHORT + STRING + BLOB

		internal FieldRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// MethodPtr表中的一行
	/// </summary>
	public sealed unsafe class MethodPtrRow : TableRow {
		public uint Method => ReadUInt16(RawDataUnsafe + 2);
		// RID

		internal MethodPtrRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Method表中的一行
	/// </summary>
	public sealed unsafe class MethodRow : TableRow {
		public new uint RVA => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public ushort ImplFlags => ReadUInt16(RawDataUnsafe + 4 + 2);
		// ULONG + USHORT

		public ushort Flags => ReadUInt16(RawDataUnsafe + 4 + 2 + 2);
		// ULONG + USHORT + USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 4 + 2 + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + USHORT + USHORT + STRING

		public uint Signature => ReadHeapOffset(RawDataUnsafe + 4 + 2 + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// ULONG + USHORT + USHORT + STRING + BLOB

		public uint ParamList => ReadUInt16(RawDataUnsafe + 4 + 2 + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob) + 2);
		// ULONG + USHORT + USHORT + STRING + BLOB + RID

		internal MethodRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ParamPtr表中的一行
	/// </summary>
	public sealed unsafe class ParamPtrRow : TableRow {
		public uint Param => ReadUInt16(RawDataUnsafe + 2);
		// RID

		internal ParamPtrRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Param表中的一行
	/// </summary>
	public sealed unsafe class ParamRow : TableRow {
		public ushort Flags => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public ushort Sequence => ReadUInt16(RawDataUnsafe + 2 + 2);
		// USHORT + USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + USHORT + STRING

		internal ParamRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// InterfaceImpl表中的一行
	/// </summary>
	public sealed unsafe class InterfaceImplRow : TableRow {
		public uint Class => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint Interface => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + CODED_TOKEN

		internal InterfaceImplRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// MemberRef表中的一行
	/// </summary>
	public sealed unsafe class MemberRefRow : TableRow {
		public uint Class => ReadUInt16(RawDataUnsafe + 2);
		// CODED_TOKEN

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// CODED_TOKEN + STRING

		public uint Signature => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// CODED_TOKEN + STRING + BLOB

		internal MemberRefRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Constant表中的一行
	/// </summary>
	public sealed unsafe class ConstantRow : TableRow {
		public byte Type => ReadByte(RawDataUnsafe + 1);
		// BYTE

		public byte Padding => ReadByte(RawDataUnsafe + 1 + 1);
		// BYTE + BYTE

		public uint Parent => ReadUInt16(RawDataUnsafe + 1 + 1 + 2);
		// BYTE + BYTE + CODED_TOKEN

		public uint Value => ReadHeapOffset(RawDataUnsafe + 1 + 1 + 2 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// BYTE + BYTE + CODED_TOKEN + BLOB

		internal ConstantRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// CustomAttribute表中的一行
	/// </summary>
	public sealed unsafe class CustomAttributeRow : TableRow {
		public uint Parent => ReadUInt16(RawDataUnsafe + 2);
		// CODED_TOKEN

		public uint Type => ReadUInt16(RawDataUnsafe + 2 + 2);
		// CODED_TOKEN + CODED_TOKEN

		public uint Value => ReadHeapOffset(RawDataUnsafe + 2 + 2 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// CODED_TOKEN + CODED_TOKEN + BLOB

		internal CustomAttributeRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// FieldMarshal表中的一行
	/// </summary>
	public sealed unsafe class FieldMarshalRow : TableRow {
		public uint Parent => ReadUInt16(RawDataUnsafe + 2);
		// CODED_TOKEN

		public uint NativeType => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// CODED_TOKEN + BLOB

		internal FieldMarshalRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// DeclSecurity表中的一行
	/// </summary>
	public sealed unsafe class DeclSecurityRow : TableRow {
		public short Action => ReadInt16(RawDataUnsafe + 2);
		// SHORT

		public uint Parent => ReadUInt16(RawDataUnsafe + 2 + 2);
		// SHORT + CODED_TOKEN

		public uint PermissionSet => ReadHeapOffset(RawDataUnsafe + 2 + 2 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// SHORT + CODED_TOKEN + BLOB

		internal DeclSecurityRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ClassLayout表中的一行
	/// </summary>
	public sealed unsafe class ClassLayoutRow : TableRow {
		public ushort PackingSize => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint ClassSize => ReadUInt32(RawDataUnsafe + 2 + 4);
		// USHORT + ULONG

		public uint Parent => ReadUInt16(RawDataUnsafe + 2 + 4 + 2);
		// USHORT + ULONG + RID

		internal ClassLayoutRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// FieldLayout表中的一行
	/// </summary>
	public sealed unsafe class FieldLayoutRow : TableRow {
		public uint OffSet => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint Field => ReadUInt16(RawDataUnsafe + 4 + 2);
		// ULONG + RID

		internal FieldLayoutRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// StandAloneSig表中的一行
	/// </summary>
	public sealed unsafe class StandAloneSigRow : TableRow {
		public uint Signature => ReadHeapOffset(RawDataUnsafe + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// BLOB

		internal StandAloneSigRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// EventMap表中的一行
	/// </summary>
	public sealed unsafe class EventMapRow : TableRow {
		public uint Parent => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint EventList => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + RID

		internal EventMapRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// EventPtr表中的一行
	/// </summary>
	public sealed unsafe class EventPtrRow : TableRow {
		public uint Event => ReadUInt16(RawDataUnsafe + 2);
		// RID

		internal EventPtrRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Event表中的一行
	/// </summary>
	public sealed unsafe class EventRow : TableRow {
		public ushort EventFlags => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + STRING

		public uint EventType => ReadUInt16(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + 2);
		// USHORT + STRING + CODED_TOKEN

		internal EventRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// PropertyMap表中的一行
	/// </summary>
	public sealed unsafe class PropertyMapRow : TableRow {
		public uint Parent => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint PropertyList => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + RID

		internal PropertyMapRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// PropertyPtr表中的一行
	/// </summary>
	public sealed unsafe class PropertyPtrRow : TableRow {
		public uint Property => ReadUInt16(RawDataUnsafe + 2);
		// RID

		internal PropertyPtrRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Property表中的一行
	/// </summary>
	public sealed unsafe class PropertyRow : TableRow {
		public ushort PropFlags => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + STRING

		public uint Type => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// USHORT + STRING + BLOB

		internal PropertyRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// MethodSemantics表中的一行
	/// </summary>
	public sealed unsafe class MethodSemanticsRow : TableRow {
		public ushort Semantic => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint Method => ReadUInt16(RawDataUnsafe + 2 + 2);
		// USHORT + RID

		public uint Association => ReadUInt16(RawDataUnsafe + 2 + 2 + 2);
		// USHORT + RID + CODED_TOKEN

		internal MethodSemanticsRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// MethodImpl表中的一行
	/// </summary>
	public sealed unsafe class MethodImplRow : TableRow {
		public uint Class => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint MethodBody => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + CODED_TOKEN

		public uint MethodDeclaration => ReadUInt16(RawDataUnsafe + 2 + 2 + 2);
		// RID + CODED_TOKEN + CODED_TOKEN

		internal MethodImplRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ModuleRef表中的一行
	/// </summary>
	public sealed unsafe class ModuleRefRow : TableRow {
		public uint Name => ReadHeapOffset(RawDataUnsafe + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// STRING

		internal ModuleRefRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// TypeSpec表中的一行
	/// </summary>
	public sealed unsafe class TypeSpecRow : TableRow {
		public uint Signature => ReadHeapOffset(RawDataUnsafe + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// BLOB

		internal TypeSpecRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ImplMap表中的一行
	/// </summary>
	public sealed unsafe class ImplMapRow : TableRow {
		public ushort MappingFlags => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public uint MemberForwarded => ReadUInt16(RawDataUnsafe + 2 + 2);
		// USHORT + CODED_TOKEN

		public uint ImportName => ReadHeapOffset(RawDataUnsafe + 2 + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + CODED_TOKEN + STRING

		public uint ImportScope => ReadUInt16(RawDataUnsafe + 2 + 2 + ToOffsetSize(_table.IsBigString) + 2);
		// USHORT + CODED_TOKEN + STRING + RID

		internal ImplMapRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// FieldRVA表中的一行
	/// </summary>
	public sealed unsafe class FieldRVARow : TableRow {
		public new uint RVA => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint Field => ReadUInt16(RawDataUnsafe + 4 + 2);
		// ULONG + RID

		internal FieldRVARow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ENCLog表中的一行
	/// </summary>
	public sealed unsafe class ENCLogRow : TableRow {
		public uint Token => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint FuncCode => ReadUInt32(RawDataUnsafe + 4 + 4);
		// ULONG + ULONG

		internal ENCLogRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ENCMap表中的一行
	/// </summary>
	public sealed unsafe class ENCMapRow : TableRow {
		public uint Token => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		internal ENCMapRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// Assembly表中的一行
	/// </summary>
	public sealed unsafe class AssemblyRow : TableRow {
		public uint HashAlgId => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public ushort MajorVersion => ReadUInt16(RawDataUnsafe + 4 + 2);
		// ULONG + USHORT

		public ushort MinorVersion => ReadUInt16(RawDataUnsafe + 4 + 2 + 2);
		// ULONG + USHORT + USHORT

		public ushort BuildNumber => ReadUInt16(RawDataUnsafe + 4 + 2 + 2 + 2);
		// ULONG + USHORT + USHORT + USHORT

		public ushort RevisionNumber => ReadUInt16(RawDataUnsafe + 4 + 2 + 2 + 2 + 2);
		// ULONG + USHORT + USHORT + USHORT + USHORT

		public uint Flags => ReadUInt32(RawDataUnsafe + 4 + 2 + 2 + 2 + 2 + 4);
		// ULONG + USHORT + USHORT + USHORT + USHORT + ULONG

		public uint PublicKey => ReadHeapOffset(RawDataUnsafe + 4 + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// ULONG + USHORT + USHORT + USHORT + USHORT + ULONG + BLOB

		public uint Name => ReadHeapOffset(RawDataUnsafe + 4 + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING

		public uint Locale => ReadHeapOffset(RawDataUnsafe + 4 + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob) + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING + STRING

		internal AssemblyRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// AssemblyProcessor表中的一行
	/// </summary>
	public sealed unsafe class AssemblyProcessorRow : TableRow {
		public uint Processor => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		internal AssemblyProcessorRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// AssemblyOS表中的一行
	/// </summary>
	public sealed unsafe class AssemblyOSRow : TableRow {
		public uint OSPlatformId => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint OSMajorVersion => ReadUInt32(RawDataUnsafe + 4 + 4);
		// ULONG + ULONG

		public uint OSMinorVersion => ReadUInt32(RawDataUnsafe + 4 + 4 + 4);
		// ULONG + ULONG + ULONG

		internal AssemblyOSRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// AssemblyRef表中的一行
	/// </summary>
	public sealed unsafe class AssemblyRefRow : TableRow {
		public ushort MajorVersion => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public ushort MinorVersion => ReadUInt16(RawDataUnsafe + 2 + 2);
		// USHORT + USHORT

		public ushort BuildNumber => ReadUInt16(RawDataUnsafe + 2 + 2 + 2);
		// USHORT + USHORT + USHORT

		public ushort RevisionNumber => ReadUInt16(RawDataUnsafe + 2 + 2 + 2 + 2);
		// USHORT + USHORT + USHORT + USHORT

		public uint Flags => ReadUInt32(RawDataUnsafe + 2 + 2 + 2 + 2 + 4);
		// USHORT + USHORT + USHORT + USHORT + ULONG

		public uint PublicKeyOrToken => ReadHeapOffset(RawDataUnsafe + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// USHORT + USHORT + USHORT + USHORT + ULONG + BLOB

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING

		public uint Locale => ReadHeapOffset(RawDataUnsafe + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob) + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING + STRING

		public uint HashValue => ReadHeapOffset(RawDataUnsafe + 2 + 2 + 2 + 2 + 4 + ToOffsetSize(_table.IsBigBlob) + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// USHORT + USHORT + USHORT + USHORT + ULONG + BLOB + STRING + STRING + BLOB

		internal AssemblyRefRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// AssemblyRefProcessor表中的一行
	/// </summary>
	public sealed unsafe class AssemblyRefProcessorRow : TableRow {
		public uint Processor => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint AssemblyRef => ReadUInt16(RawDataUnsafe + 4 + 2);
		// ULONG + RID

		internal AssemblyRefProcessorRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// AssemblyRefOS表中的一行
	/// </summary>
	public sealed unsafe class AssemblyRefOSRow : TableRow {
		public uint OSPlatformId => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint OSMajorVersion => ReadUInt32(RawDataUnsafe + 4 + 4);
		// ULONG + ULONG

		public uint OSMinorVersion => ReadUInt32(RawDataUnsafe + 4 + 4 + 4);
		// ULONG + ULONG + ULONG

		public uint AssemblyRef => ReadUInt16(RawDataUnsafe + 4 + 4 + 4 + 2);
		// ULONG + ULONG + ULONG + RID

		internal AssemblyRefOSRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// File表中的一行
	/// </summary>
	public sealed unsafe class FileRow : TableRow {
		public uint Flags => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint Name => ReadHeapOffset(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + STRING

		public uint HashValue => ReadHeapOffset(RawDataUnsafe + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// ULONG + STRING + BLOB

		internal FileRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ExportedType表中的一行
	/// </summary>
	public sealed unsafe class ExportedTypeRow : TableRow {
		public uint Flags => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint TypeDefId => ReadUInt32(RawDataUnsafe + 4 + 4);
		// ULONG + ULONG

		public uint TypeName => ReadHeapOffset(RawDataUnsafe + 4 + 4 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + ULONG + STRING

		public uint TypeNamespace => ReadHeapOffset(RawDataUnsafe + 4 + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + ULONG + STRING + STRING

		public uint Implementation => ReadUInt16(RawDataUnsafe + 4 + 4 + ToOffsetSize(_table.IsBigString) + ToOffsetSize(_table.IsBigString) + 2);
		// ULONG + ULONG + STRING + STRING + CODED_TOKEN

		internal ExportedTypeRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// ManifestResource表中的一行
	/// </summary>
	public sealed unsafe class ManifestResourceRow : TableRow {
		public uint Offset => ReadUInt32(RawDataUnsafe + 4);
		// ULONG

		public uint Flags => ReadUInt32(RawDataUnsafe + 4 + 4);
		// ULONG + ULONG

		public uint Name => ReadHeapOffset(RawDataUnsafe + 4 + 4 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// ULONG + ULONG + STRING

		public uint Implementation => ReadUInt16(RawDataUnsafe + 4 + 4 + ToOffsetSize(_table.IsBigString) + 2);
		// ULONG + ULONG + STRING + CODED_TOKEN

		internal ManifestResourceRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// NestedClass表中的一行
	/// </summary>
	public sealed unsafe class NestedClassRow : TableRow {
		public uint NestedClass => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint EnclosingClass => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + RID

		internal NestedClassRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// GenericParam表中的一行
	/// </summary>
	public sealed unsafe class GenericParamRow : TableRow {
		public ushort Number => ReadUInt16(RawDataUnsafe + 2);
		// USHORT

		public ushort Flags => ReadUInt16(RawDataUnsafe + 2 + 2);
		// USHORT + USHORT

		public uint Owner => ReadUInt16(RawDataUnsafe + 2 + 2 + 2);
		// USHORT + USHORT + CODED_TOKEN

		public uint Name => ReadHeapOffset(RawDataUnsafe + 2 + 2 + 2 + ToOffsetSize(_table.IsBigString), _table.IsBigString);
		// USHORT + USHORT + CODED_TOKEN + STRING

		internal GenericParamRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// MethodSpec表中的一行
	/// </summary>
	public sealed unsafe class MethodSpecRow : TableRow {
		public uint Method => ReadUInt16(RawDataUnsafe + 2);
		// CODED_TOKEN

		public uint Instantiation => ReadHeapOffset(RawDataUnsafe + 2 + ToOffsetSize(_table.IsBigBlob), _table.IsBigBlob);
		// CODED_TOKEN + BLOB

		internal MethodSpecRow(MetadataTable table, uint index) : base(table, index) {
		}
	}

	/// <summary>
	/// GenericParamConstraint表中的一行
	/// </summary>
	public sealed unsafe class GenericParamConstraintRow : TableRow {
		public uint Owner => ReadUInt16(RawDataUnsafe + 2);
		// RID

		public uint Constraint => ReadUInt16(RawDataUnsafe + 2 + 2);
		// RID + CODED_TOKEN

		internal GenericParamConstraintRow(MetadataTable table, uint index) : base(table, index) {
		}
	}
}
#pragma warning restore CS1591
