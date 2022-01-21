namespace System;

/// <summary>
/// Marks current type as child of <see cref="Parent"/>. <see cref="Parent"/> type must have instance property 'ChildOptions' with return type 'IDictionary&lt;Type, object&gt;'
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ChildOptionsAttribute : Attribute {
	/// <summary>
	/// Parent options type
	/// </summary>
	public Type Parent { get; }

	/// <summary>
	/// Option prefix (can be empty)
	/// </summary>
	public string Prefix { get; set; } = string.Empty;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="parent">Parent options type</param>
	/// <exception cref="ArgumentNullException"></exception>
	public ChildOptionsAttribute(Type parent) {
		Parent = parent ?? throw new ArgumentNullException(nameof(parent));
	}
}
