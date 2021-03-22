using System;

namespace TestTool {
	internal enum ToolEnum {
		EnumA,
		EnumB
	}

	internal sealed class ToolOptions {
		[Option(Description = "A string value, this is default option")]
		public string DefaultOption { get; set; }

		[Option("-r", IsRequired = true, Description = "A string value")]
		public string RequiredOption { get; set; }

		[Option("-o", DefaultValue = ToolEnum.EnumB, Description = "An enum value")]
		public ToolEnum OptionalOption { get; set; }

		[Option("-o2", DefaultValue = new ToolEnum[] { ToolEnum.EnumA, ToolEnum.EnumB }, Description = "Some enum values")]
		public ToolEnum[] OptionalOption2 { get; set; }
	}
}
