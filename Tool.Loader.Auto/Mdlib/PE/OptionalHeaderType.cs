namespace Mdlib.PE {
	/// <summary>
	/// IMAGE_OPTIONAL_HEADER.Magic
	/// </summary>
	public enum OptionalHeaderType : ushort {
		/// <summary>
		/// ROM
		/// </summary>
		ROM = 0x107,

		/// <summary>
		/// 32位PE头
		/// </summary>
		PE32 = 0x10B,

		/// <summary>
		/// 64位PE头
		/// </summary>
		PE64 = 0x20B
	}
}
