namespace System;

/// <summary>
/// Represents a command line option. The property which <see cref="OptionAttribute"/> is applied to must be an instance property and one of the following types: <see cref="bool"/>, <see cref="char"/>, <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>, <see cref="DateTime"/>, <see cref="string"/>, <see cref="Enum"/> or array of them.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute : Attribute {
	/// <summary>
	/// Option name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Whether it is a default option
	/// </summary>
	public bool IsDefault => string.IsNullOrEmpty(Name);

	/// <summary>
	/// Whether it is a required option
	/// </summary>
	public bool IsRequired { get; set; }

	/// <summary>
	/// Array separator, which is used to split value when option type is array
	/// </summary>
	public char Separator { get; set; } = ',';

	/// <summary>
	/// By default, when <see cref="IsRequired"/> is <see langword="true"/>, <see cref="DefaultValue"/> must be <see langword="null"/>
	/// </summary>
	public object? DefaultValue { get; set; }

	/// <summary>
	/// Option description, which is used to describe this option
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Constructor
	/// </summary>
	public OptionAttribute() {
		Name = string.Empty;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="name">Option name</param>
	public OptionAttribute(string name) {
		Name = name;
	}
}
