using System;
using System.Collections.Generic;

namespace TestTool;

enum ToolEnum {
	EnumA,
	EnumB
}

sealed class ToolOptions {
	public IDictionary<Type, object> ChildOptions { get; } = new Dictionary<Type, object>();

	[Option(Description = "A string value, this is default option")]
	public string DefaultOption { get; set; }

	[Option("-r", IsRequired = true, Description = "A string value")]
	public string RequiredOption { get; set; }

	[Option("-o", DefaultValue = ToolEnum.EnumB, Description = "An enum value")]
	public ToolEnum OptionalOption { get; set; }

	[Option("-o2", DefaultValue = new ToolEnum[] { ToolEnum.EnumA, ToolEnum.EnumB }, Description = "Some enum values")]
	public ToolEnum[] OptionalOption2 { get; set; }

	public T ChildOption<T>() {
		return (T)ChildOptions[typeof(T)];
	}
}

[ChildOptions(typeof(ToolOptions))]
sealed class ChildToolOptions1 {
	[Option("-child1-o1", Description = "A child option", DefaultValue = "My default value")]
	public string ChildOption1 { get; set; }
}

[ChildOptions(typeof(ToolOptions), Prefix = "-child2")]
sealed class ChildToolOptions2 {
	[Option("-o2", Description = "Another child option")]
	public string ChildOption2 { get; set; }
}
