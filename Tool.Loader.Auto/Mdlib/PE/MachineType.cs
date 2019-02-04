// reference dnlib\src\PE\Machine.cs

namespace Mdlib.PE {
	/// <summary>
	/// IMAGE_FILE_HEADER.Machine enum
	/// </summary>
	public enum MachineType : ushort {
		/// <summary>
		/// Unknown machine
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// x86
		/// </summary>
		I386 = 0x014C,

		/// <summary>
		/// MIPS little-endian, 0x160 big-endian
		/// </summary>
		R3000 = 0x0162,

		/// <summary>
		/// MIPS little-endian
		/// </summary>
		R4000 = 0x0166,

		/// <summary>
		/// MIPS little-endian
		/// </summary>
		R10000 = 0x0168,

		/// <summary>
		/// MIPS little-endian WCE v2
		/// </summary>
		WCEMIPSV2 = 0x0169,

		/// <summary>
		/// Alpha_AXP
		/// </summary>
		ALPHA = 0x0184,

		/// <summary>
		/// SH3 little-endian
		/// </summary>
		SH3 = 0x01A2,

		/// <summary />
		SH3DSP = 0x01A3,

		/// <summary>
		/// SH3E little-endian
		/// </summary>
		SH3E = 0x01A4,

		/// <summary>
		/// SH4 little-endian
		/// </summary>
		SH4 = 0x01A6,

		/// <summary>
		/// SH5
		/// </summary>
		SH5 = 0x01A8,

		/// <summary>
		/// ARM Little-Endian
		/// </summary>
		ARM = 0x01C0,

		/// <summary>
		/// ARM Thumb/Thumb-2 Little-Endian
		/// </summary>
		THUMB = 0x01C2,

		/// <summary>
		/// ARM Thumb-2 Little-Endian
		/// </summary>
		ARMNT = 0x01C4,

		/// <summary />
		AM33 = 0x01D3,

		/// <summary>
		/// IBM PowerPC Little-Endian
		/// </summary>
		POWERPC = 0x01F0,

		/// <summary />
		POWERPCFP = 0x01F1,

		/// <summary>
		/// IA-64
		/// </summary>
		IA64 = 0x0200,

		/// <summary />
		MIPS16 = 0x0266,

		/// <summary />
		ALPHA64 = 0x0284,

		/// <summary />
		MIPSFPU = 0x0366,

		/// <summary />
		MIPSFPU16 = 0x0466,

		/// <summary>
		/// Infineon
		/// </summary>
		TRICORE = 0x0520,

		/// <summary />
		CEF = 0x0CEF,

		/// <summary>
		/// EFI Byte Code
		/// </summary>
		EBC = 0x0EBC,

		/// <summary>
		/// x64
		/// </summary>
		AMD64 = 0x8664,

		/// <summary>
		/// M32R little-endian
		/// </summary>
		M32R = 0x9041,

		/// <summary />
		ARM64 = 0xAA64,

		/// <summary />
		CEE = 0xC0EE,

		// Search for IMAGE_FILE_MACHINE_NATIVE and IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE here:
		//		https://github.com/dotnet/coreclr/blob/master/src/inc/pedecoder.h
		// Note that IMAGE_FILE_MACHINE_NATIVE_OS_OVERRIDE == 0 if it's Windows

		/// <summary />
		I386_Native_Apple = I386 ^ 0x4644,

		/// <summary />
		AMD64_Native_Apple = AMD64 ^ 0x4644,

		/// <summary />
		ARMNT_Native_Apple = ARMNT ^ 0x4644,

		/// <summary />
		ARM64_Native_Apple = ARM64 ^ 0x4644,

		/// <summary />
		I386_Native_FreeBSD = I386 ^ 0xADC4,

		/// <summary />
		AMD64_Native_FreeBSD = AMD64 ^ 0xADC4,

		/// <summary />
		ARMNT_Native_FreeBSD = ARMNT ^ 0xADC4,

		/// <summary />
		ARM64_Native_FreeBSD = ARM64 ^ 0xADC4,

		/// <summary />
		I386_Native_Linux = I386 ^ 0x7B79,

		/// <summary />
		AMD64_Native_Linux = AMD64 ^ 0x7B79,

		/// <summary />
		ARMNT_Native_Linux = ARMNT ^ 0x7B79,

		/// <summary />
		ARM64_Native_Linux = ARM64 ^ 0x7B79,

		/// <summary />
		I386_Native_NetBSD = I386 ^ 0x1993,

		/// <summary />
		AMD64_Native_NetBSD = AMD64 ^ 0x1993,

		/// <summary />
		ARMNT_Native_NetBSD = ARMNT ^ 0x1993,

		/// <summary />
		ARM64_Native_NetBSD = ARM64 ^ 0x1993
	}
}
