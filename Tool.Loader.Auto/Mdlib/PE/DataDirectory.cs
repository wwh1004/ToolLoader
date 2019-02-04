using System.Runtime.InteropServices;

namespace Mdlib.PE {
	/// <summary />
	[StructLayout(LayoutKind.Sequential)]
	public struct DataDirectory {
		private uint _address;
		private uint _size;

		/// <summary />
		public uint Address {
			get => _address;
			set => _address = value;
		}

		/// <summary />
		public uint Size {
			get => _size;
			set => _size = value;
		}
	}
}
